# PHASE 12: Multi-Stage Docker Build for ProjetoVarejo.Api
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files
COPY ["src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj", "src/ProjetoVarejo.Api/"]
COPY ["src/ProjetoVarejo.Domain/ProjetoVarejo.Domain.csproj", "src/ProjetoVarejo.Domain/"]
COPY ["src/ProjetoVarejo.Application/ProjetoVarejo.Application.csproj", "src/ProjetoVarejo.Application/"]
COPY ["src/ProjetoVarejo.Infrastructure/ProjetoVarejo.Infrastructure.csproj", "src/ProjetoVarejo.Infrastructure/"]
COPY ["src/ProjetoVarejo.Shared/ProjetoVarejo.Shared.csproj", "src/ProjetoVarejo.Shared/"]

# Restore dependencies
RUN dotnet restore "src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj"

# Copy all source code
COPY . .

# Build in Release mode
RUN dotnet build "src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish

RUN dotnet publish "src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install health check dependencies
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

# Expose port
EXPOSE 80

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check (Docker & Kubernetes)
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "ProjetoVarejo.Api.dll"]
