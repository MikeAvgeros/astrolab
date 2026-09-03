FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution configs and project files for restore caching
COPY AstroLab.slnx Directory.Build.props ./
COPY src/AstroLab.Api/AstroLab.Api.csproj src/AstroLab.Api/
COPY src/AstroLab.Core/AstroLab.Core.csproj src/AstroLab.Core/
COPY src/AstroLab.Infrastructure/AstroLab.Infrastructure.csproj src/AstroLab.Infrastructure/
RUN dotnet restore src/AstroLab.Api/AstroLab.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/AstroLab.Api/AstroLab.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install native C++ dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends libcfitsio-dev \
    && rm -rf /var/lib/apt/lists/*

# Prepare storage directory and set ownership to built-in non-root 'app' user
RUN mkdir -p /app/storage && chown -R app:app /app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__RootPath=/app/storage

USER app
EXPOSE 8080
VOLUME ["/app/storage"]

ENTRYPOINT ["dotnet", "AstroLab.Api.dll"]
