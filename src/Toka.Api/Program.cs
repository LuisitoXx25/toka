using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;
using Toka.Api.Infrastructure;
using Toka.Api.Security;
using Toka.Application;
using Toka.Application.Abstractions;
using Toka.Infrastructure;
using Toka.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logs to stdout; the container platform collects them. Request bodies are never logged.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Toka.Api")
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<HttpCorrelationContext>();
builder.Services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<HttpCorrelationContext>());

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Malformed JSON or wrong types: return a Spanish message without echoing framework internals.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key.TrimStart('$', '.'), _ => new[] { "El valor no tiene un formato válido." });
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "La solicitud contiene datos inválidos.",
            Detail = "El cuerpo de la solicitud no tiene el formato esperado.",
            Extensions = { ["code"] = "malformed_request" },
        };
        Problems.Enrich(problem, context.HttpContext);
        return new BadRequestObjectResult(problem) { ContentTypes = { Problems.ContentType } };
    };
});

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => Problems.Enrich(ctx.ProblemDetails, ctx.HttpContext));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName,
        o => o.Keys = builder.Configuration.GetSection("Security:ApiKeys").Get<string[]>() ?? []);
// Secure by default: every endpoint requires the API key unless explicitly marked anonymous.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type", ApiKeyAuthenticationHandler.HeaderName, CorrelationIdMiddleware.HeaderName, "Idempotency-Key")
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Location")));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Toka Checkout API",
        Version = "v1",
        Description = "Registro de clientes, órdenes de compra, autorización de pago simulada, inventario y bitácora.",
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Toka.Api.xml"));
    options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyAuthenticationHandler.HeaderName,
        Description = "API key del cliente.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document)] = [],
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Behind the nginx proxy in Docker; lets rate limiting and logs see the real client IP.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Toka Checkout API");
}

app.UseCors();
// Before authentication so failed API-key guesses are rate limited too.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

app.Run();

public partial class Program;
