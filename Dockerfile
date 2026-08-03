# Multi-stage Dockerfile for GharCraft Backend (.NET 10 API)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for optimal layer caching
COPY backend/GharCraft.slnx ./backend/
COPY backend/src/GharCraft.Domain/GharCraft.Domain.csproj ./backend/src/GharCraft.Domain/
COPY backend/src/GharCraft.Application/GharCraft.Application.csproj ./backend/src/GharCraft.Application/
COPY backend/src/GharCraft.Infrastructure/GharCraft.Infrastructure.csproj ./backend/src/GharCraft.Infrastructure/
COPY backend/src/GharCraft.Api/GharCraft.Api.csproj ./backend/src/GharCraft.Api/
COPY backend/tests/GharCraft.UnitTests/GharCraft.UnitTests.csproj ./backend/tests/GharCraft.UnitTests/
COPY backend/tests/GharCraft.IntegrationTests/GharCraft.IntegrationTests.csproj ./backend/tests/GharCraft.IntegrationTests/

# Restore dependencies
RUN dotnet restore backend/GharCraft.slnx

# Copy all source
COPY backend/ ./backend/

# Publish release build (only the API project, not tests)
RUN dotnet publish backend/src/GharCraft.Api/GharCraft.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install wget for health check (aspnet base image has wget but not curl)
RUN apt-get update && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Railway injects PORT at runtime; default to 8080
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Staging
ENV ASPNETCORE_URLS=http://+:${PORT}

EXPOSE 8080

# Use wget instead of curl (available in aspnet base image)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "GharCraft.Api.dll"]
