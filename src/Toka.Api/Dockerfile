# Build context: repository root (needs Toka.sln and Directory.Build.props).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Project files first so the restore layer is cached until dependencies change.
COPY Directory.Build.props Toka.sln ./
COPY src/Toka.Domain/Toka.Domain.csproj src/Toka.Domain/
COPY src/Toka.Application/Toka.Application.csproj src/Toka.Application/
COPY src/Toka.Infrastructure/Toka.Infrastructure.csproj src/Toka.Infrastructure/
COPY src/Toka.Api/Toka.Api.csproj src/Toka.Api/
RUN dotnet restore src/Toka.Api/Toka.Api.csproj
COPY src/ src/
RUN dotnet publish src/Toka.Api/Toka.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Diagnostics IPC needs a writable /tmp socket; disabled so the container can run with a read-only filesystem.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
# Non-root user provided by the base image.
USER $APP_UID
ENTRYPOINT ["dotnet", "Toka.Api.dll"]
