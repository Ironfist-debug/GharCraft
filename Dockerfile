# Multi-stage Dockerfile for GharCraft Backend (.NET 10 API)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching
COPY backend/GharCraft.slnx ./backend/
COPY backend/src/GharCraft.Domain/GharCraft.Domain.csproj ./backend/src/GharCraft.Domain/
COPY backend/src/GharCraft.Application/GharCraft.Application.csproj ./backend/src/GharCraft.Application/
COPY backend/src/GharCraft.Infrastructure/GharCraft.Infrastructure.csproj ./backend/src/GharCraft.Infrastructure/
COPY backend/src/GharCraft.Api/GharCraft.Api.csproj ./backend/src/GharCraft.Api/
COPY backend/tests/GharCraft.UnitTests/GharCraft.UnitTests.csproj ./backend/tests/GharCraft.UnitTests/
COPY backend/tests/GharCraft.IntegrationTests/GharCraft.IntegrationTests.csproj ./backend/tests/GharCraft.IntegrationTests/

# Restore dependencies
RUN dotnet restore backend/GharCraft.slnx

# Copy the rest of the source code
COPY backend/ ./backend/

# Build and publish release binaries
WORKDIR /src/backend/src/GharCraft.Api
RUN dotnet publish GharCraft.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Dynamic PORT support (Railway/Render default)
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Staging
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:${PORT}/healthz || exit 1

ENTRYPOINT ["dotnet", "GharCraft.Api.dll"]
