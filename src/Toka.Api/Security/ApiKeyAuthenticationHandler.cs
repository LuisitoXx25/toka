using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Toka.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace Toka.Api.Security;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>Valid keys, loaded from configuration (env var <c>Security__ApiKeys__0</c>...). Never committed to the repo.</summary>
    public string[] Keys { get; set; } = [];
}

/// <summary>
/// Authenticates server-to-server callers with the <c>X-Api-Key</c> header. The web frontend never sees the key:
/// its nginx proxy adds it when forwarding to the API.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Options.Keys.Any(key => FixedTimeEquals(key, provided.ToString())))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "api-client")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Problems.WriteAsync(Context, StatusCodes.Status401Unauthorized, "No autenticado.",
            $"Falta el encabezado {HeaderName} o no es válido.", "unauthorized");

    // Constant-time comparison so response timing doesn't reveal how much of the key matched.
    private static bool FixedTimeEquals(string expected, string provided) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));
}
