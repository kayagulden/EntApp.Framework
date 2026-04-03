# ═══════════════════════════════════════════════
#  EntApp.Framework — Multi-stage Dockerfile
# ═══════════════════════════════════════════════

# ── Build Stage ──────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution & project files (restore cache layer)
COPY EntApp.sln .
COPY Directory.Build.props .
COPY src/Shared/EntApp.Shared.Kernel/EntApp.Shared.Kernel.csproj src/Shared/EntApp.Shared.Kernel/
COPY src/Shared/EntApp.Shared.Contracts/EntApp.Shared.Contracts.csproj src/Shared/EntApp.Shared.Contracts/
COPY src/Shared/EntApp.Shared.Infrastructure/EntApp.Shared.Infrastructure.csproj src/Shared/EntApp.Shared.Infrastructure/
COPY src/Host/EntApp.WebAPI/EntApp.WebAPI.csproj src/Host/EntApp.WebAPI/

RUN dotnet restore src/Host/EntApp.WebAPI/EntApp.WebAPI.csproj

# Copy all source & build
COPY . .
RUN dotnet publish src/Host/EntApp.WebAPI/EntApp.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime Stage ────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "EntApp.WebAPI.dll"]
