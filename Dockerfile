# Stage 1: .NET Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy solution configuration and project files for restore caching
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

# Stage 2: CFITSIO Installation (matches the Ubuntu runtime base)
FROM ubuntu:24.04 AS cfitsio-build

ARG CFITSIO_VERSION=4.7.0
ARG CFITSIO_SHA256=ce573bbea8e75b429f8c3d3e86498741ba3dc9628a1530d2f65268397ad059e8

WORKDIR /usr/src/cfitsio

RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential curl ca-certificates zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL -o cfitsio.tar.gz "https://heasarc.gsfc.nasa.gov/FTP/software/fitsio/c/cfitsio-${CFITSIO_VERSION}.tar.gz" \
    && echo "${CFITSIO_SHA256}  cfitsio.tar.gz" | sha256sum -c - \
    && tar xzf cfitsio.tar.gz --strip-components=1 \
    && ./configure --prefix=/usr/local \
    && make -j"$(nproc)" \
    && make install

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=cfitsio-build /usr/local/lib/libcfitsio.so* /usr/lib/x86_64-linux-gnu/
RUN ldconfig

# Prepare storage directory
RUN mkdir -p /app/storage \
    && chown -R app:app /app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__RootPath=/app/storage

USER app

EXPOSE 8080

VOLUME ["/app/storage"]

ENTRYPOINT ["dotnet", "AstroLab.Api.dll"]
