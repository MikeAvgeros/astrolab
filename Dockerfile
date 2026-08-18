# syntax=docker/dockerfile:1

# --- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached independently of
# source-code changes.
COPY AstroLab.slnx Directory.Build.props ./
COPY src/AstroLab.Api/AstroLab.Api.csproj src/AstroLab.Api/
COPY src/AstroLab.Core/AstroLab.Core.csproj src/AstroLab.Core/
COPY src/AstroLab.Infrastructure/AstroLab.Infrastructure.csproj src/AstroLab.Infrastructure/
COPY src/AstroLab.Tests/AstroLab.Tests.csproj src/AstroLab.Tests/
RUN dotnet restore src/AstroLab.Api/AstroLab.Api.csproj

COPY src/ src/
RUN dotnet publish src/AstroLab.Api/AstroLab.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Runtime stage -------------------------------------------------------------
# Note: the native cfitsio binary referenced in CLAUDE.md (native/win-x64/cfitsio.dll) is a
# Windows-only artifact and is never copied into this Linux image. That is safe today because
# NativeMethods (AstroLab.Infrastructure.Fits) is not yet called from anywhere in the FITS
# read/write path — see spec.md. Wiring up real cfitsio P/Invoke calls will require adding a
# native/linux-x64/*.so item group to Directory.Build.props and copying it into this stage.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1654 astrolab \
    && useradd --system --uid 1654 --gid astrolab --no-create-home astrolab \
    && mkdir -p /app/storage \
    && chown -R astrolab:astrolab /app

COPY --from=build --chown=astrolab:astrolab /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__RootPath=/app/storage

USER astrolab
EXPOSE 8080
VOLUME ["/app/storage"]

ENTRYPOINT ["dotnet", "AstroLab.Api.dll"]
