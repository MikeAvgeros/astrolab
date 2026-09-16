# AstroLab

AstroLab is a high-performance **.NET 10 / C# 14 RESTful API** for working with astronomical **FITS (Flexible Image Transport System)** data.

It can download observations from the **ESO** and **MAST**, accept your own FITS files, and then turn those datasets into something you can inspect, visualise and analyse programmatically.

> **Work in progress.** AstroLab is under active development. Endpoints, response shapes, and scientific algorithms may change, and some calculations have not yet been validated against reference implementations or real instrument data. Treat computed values as indicative rather than authoritative until they have been independently verified.

The API currently supports:

- 🔭 Searching the ESO and MAST archives
- 📥 Downloading astronomical observations
- 📁 Uploading your own FITS files
- 🧩 Inspecting FITS HDUs and their scientific capabilities
- 🗂️ Inspecting observation metadata and provenance
- ✅ Assessing FITS image data quality
- 🖼️ Rendering astronomical images
- 📊 Image statistics and histograms
- ⭐ Source detection and source characterisation
- 🧩 Source segmentation and background estimation
- 📐 WCS-based astrometry
- 🗺️ WCS-aware image visualisation
- 💡 Aperture, uncertainty, SNR and differential photometry
- 🔧 Aperture correction
- 🔬 Spectral extraction and wavelength calibration
- 📈 Spectral continuum fitting and subtraction
- ✨ Spectral-line detection and fitting
- 📏 Equivalent-width and spectral SNR measurements
- 🌈 Spectral redshift analysis and comparison
- ⏱️ Time-series and light-curve analysis
- 📉 Light-curve detrending and variability statistics
- 🔄 Lomb–Scargle period searches, periodograms and phase folding
- 🪐 Transit candidate searches
- 🌡️ Stellar colour and temperature estimation
- 🔬 Spectral classification and radial-velocity measurements
- 🌌 Galaxy morphology and surface-brightness measurements
- 📏 Physical-size measurements
- 📚 Astronomical catalogue queries and source cross-matching
- 🔬 Image comparison, alignment and stacking
- 🎨 RGB image composites
- ✂️ Image cutouts and scientific contour geometry

The project is intended to bridge the gap between **raw astronomical observations** and the kinds of measurements an astronomer would normally perform when analysing those observations.

The architecture is documented in [`spec.md`](spec.md) and [`CLAUDE.md`](CLAUDE.md). This README focuses on **using AstroLab and understanding what the API does**.

---

## Contents

- [Features](#features)
- [How AstroLab Works](#how-astrolab-works)
- [Running the API](#running-the-api)
- [CFITSIO](#cfitsio)
- [Getting FITS Data](#getting-fits-data)
  - [Search an Archive](#1-search-an-archive)
  - [Download an Observation](#2-download-an-observation)
  - [Upload Your Own FITS File](#3-upload-your-own-fits-file)
- [Working with a FITS Dataset](#working-with-a-fits-dataset)
  - [FITS Inspection](#fits-inspection)
  - [Image Analysis](#image-analysis)
  - [Astrometry](#astrometry)
  - [Photometry](#photometry)
  - [Spectroscopy](#spectroscopy)
  - [Time-Series Analysis](#time-series-analysis)
  - [Measurements](#measurements)
  - [Catalogues](#catalogues)
- [Astronomical Concepts](#astronomical-concepts)
  - [FITS](#fits)
  - [Astrometry and WCS](#astrometry-and-wcs)
  - [Image Analysis](#image-analysis-1)
  - [Photometry](#photometry-1)
  - [Spectroscopy and Redshift](#spectroscopy-and-redshift)
  - [Time-Series Astronomy](#time-series-astronomy)
- [End-to-End Example](#end-to-end-example)
- [Architecture](#architecture)
- [Storage](#storage)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Testing](#testing)
- [Performance and Design](#performance-and-design)

---

## Features

### FITS

AstroLab understands FITS files at the HDU level and can inspect:

- Primary HDUs
- Image HDUs
- Binary tables
- ASCII tables
- Header keywords and values
- Image dimensions
- Data types
- WCS metadata
- Scientific capabilities provided by individual HDUs
- Data-quality statistics (invalid/saturated pixels, dynamic range, usable-pixel fraction)
- Observation metadata and provenance (target, instrument, exposure, calibration/archive identifiers)

A FITS dataset is treated as a collection of capabilities rather than as one mutually exclusive data type. A single file can therefore contain image data, WCS information, spectral data, and time-series tables simultaneously.

### Image Analysis

AstroLab provides tools for working with astronomical images:

- FITS-to-PNG rendering
- Linear, logarithmic, square-root, and asinh stretches
- Multiple colour maps
- Percentile-based black/white points
- Image statistics
- Pixel histograms
- Background estimation
- Source detection
- Source segmentation
- Source shape characterization
- Source-overlay rendering
- Image comparison
- Image alignment
- Image stacking
- Image cutouts (pixel-region or WCS sky-region)
- Scientific contour geometry
- RGB image composites
- WCS coordinate-grid overlay rendering

### Astrometry

Images containing a valid FITS WCS can be connected to celestial coordinates:

- Inspect WCS parameters
- Convert pixel coordinates to RA/Dec
- Convert RA/Dec to pixel coordinates
- Calculate an image's sky footprint
- Calculate angular separation between image positions
- Report pixel scale and orientation
- Validate a WCS solution (invertibility, axis orthogonality, round-trip consistency)
- Convert batches of pixel/world coordinates in a single request

### Photometry

AstroLab can measure the brightness of astronomical sources using:

- Aperture photometry
- Background subtraction
- Instrumental magnitudes
- Photometric uncertainty
- Signal-to-noise ratio
- Aperture correction
- Differential photometry
- Multi-source photometry

### Spectroscopy

For spectroscopic FITS data AstroLab supports:

- 1D spectral extraction
- Wavelength calibration
- Continuum fitting and subtraction
- Spectral-line detection and Gaussian profile fitting
- Equivalent-width measurement
- Spectral signal-to-noise ratio
- Redshift estimation
- Spectral comparison

### Time Series

For FITS tables containing observational time-series data:

- Light-curve extraction
- Detrending
- Phase folding
- Light-curve comparison
- Lomb-Scargle period searches
- Variability statistics
- Transit detection

### Measurements

Higher-level measurements combine lower-level astronomical results to derive quantities such as:

- Stellar colour
- Effective temperature
- Spectral classification
- Radial velocity
- Galaxy morphology
- Surface brightness
- Physical size

### Catalogues

AstroLab integrates with public astronomical catalogues through VizieR and supports:

- Catalogue cone searches
- Multiple-catalogue cross-matching
- Matching detected image sources to catalogue coordinates

---

# How AstroLab Works

Every analysis begins with a **staged FITS file**.

There are three ways to obtain one:

```text
                         ┌─────────────────────┐
                         │     ESO / MAST      │
                         │       Archive       │
                         └──────────┬──────────┘
                                    │
                                  Search
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │      Download       │
                         └──────────┬──────────┘
                                    │
                                    ▼
┌─────────────────┐        ┌─────────────────────┐
│  Your own FITS  │───────►│   Local Storage     │
│      file       │ Upload └──────────┬──────────┘
└─────────────────┘                   │
                                      │ fileId
                                      ▼
                           ┌─────────────────────┐
                           │     FITS Dataset    │
                           └──────────┬──────────┘
                                      │
             ┌────────────────────────┼────────────────────────┐
             │                        │                        │
             ▼                        ▼                        ▼
        Image Analysis           Spectroscopy           Time Series
             │                        │                        │
             ▼                        ▼                        ▼
        Astrometry               Measurements          Measurements
        Photometry
             │
             ▼
         Catalogues
```

The returned `fileId` identifies the staged dataset. Once a file has been staged, it can be analysed repeatedly without downloading or uploading it again.

The analysis endpoints are read-only unless an operation explicitly creates a new dataset, such as image stacking.

---

# Running the API

## Requirements

For local development:

- .NET 10 SDK
- C# 14
- Docker Desktop, if using the containerised setup
- CFITSIO for the native FITS table/time-series functionality when running outside Docker

Build the solution with:

```bash
dotnet build AstroLab.slnx
```

Run the API with:

```bash
dotnet run --project src/AstroLab.Api
```

By default the development API listens on:

```text
http://localhost:5279
```

Swagger UI is available at:

```text
http://localhost:5279/swagger
```

The OpenAPI document is available at:

```text
http://localhost:5279/openapi/v1.json
```

Swagger is the easiest way to inspect the available request and response models interactively.

---

## Docker

The recommended way to run AstroLab in Docker is via Docker Compose, which builds the image and bind mounts the repository's `storage/` directory into the container:

```bash
docker compose up -d
```

The API is then available at:

```text
http://localhost:8080
```

Equivalently, without Compose:

```bash
docker build -t astrolab-api .

docker run \
  -p 8080:8080 \
  -v ./storage:/app/storage \
  astrolab-api
```

The host's `./storage` directory is bind mounted to the container's `/app/storage`, which is the container-side value of `Storage:RootPath` (set via the `Storage__RootPath` environment variable in the image). Staged FITS files therefore live on the host filesystem and survive `docker compose down` / container deletion and recreation — see [Storage](#storage) below for the full persistence model.

The container runs as the non-root `app` user built into the .NET runtime image. On Docker Desktop (Windows/macOS) the bind-mounted `storage/` directory is writable by that user automatically. On native Linux hosts, ensure the host `storage/` directory is writable by the container's `app` user (uid/gid `1654` in the .NET 10 runtime image), for example:

```bash
mkdir -p storage
chown 1654:1654 storage
```

---

# CFITSIO

Most AstroLab functionality is implemented in managed C#.

The time-series FITS-table reader additionally uses the native **CFITSIO** library through P/Invoke.

CFITSIO is used for reading FITS binary and ASCII tables used by the time-series endpoints.

## Docker

Nothing additional is required.

The Docker build:

1. Downloads a pinned CFITSIO release.
2. Verifies its checksum.
3. Compiles CFITSIO in a dedicated build stage.
4. Copies the resulting shared library into the runtime image.
5. Registers the library with the dynamic linker.

## Running with `dotnet run`

CFITSIO is not bundled with the .NET application when running directly on the host.

### Windows

A `cfitsio.dll` must be available to the application, for example by installing/building CFITSIO with vcpkg:

```bash
vcpkg install cfitsio
```

The DLL can be placed next to the application binaries or somewhere on `PATH`.

### Linux / macOS

A compatible:

```text
libcfitsio.so
```

or:

```text
libcfitsio.dylib
```

must be installed and discoverable by the operating system's dynamic linker.

Without CFITSIO, FITS image processing and the other managed functionality continue to work, but the native-backed time-series FITS-table functionality cannot operate.

---

# Getting FITS Data

## 1. Search an Archive

```http
GET /api/archives/search
```

Supported archives:

- `Mast`
- `Eso`

Example:

```bash
curl "http://localhost:5279/api/archives/search?archive=Mast&target=M31&instrument=WFC3&maxResults=5"
```

### Parameters

| Parameter             | Type   | Description                 |
| --------------------- | ------ | --------------------------- |
| `archive`             | enum   | `Mast` or `Eso`             |
| `target`              | string | Astronomical target         |
| `mission`             | string | Optional mission/collection |
| `instrument`          | string | Optional instrument         |
| `searchRadiusDegrees` | double | Optional cone-search radius |
| `maxResults`          | int    | Maximum number of results   |

MAST resolves target names through its own name-resolution service, so names such as:

```text
M31
NGC 224
Andromeda Galaxy
```

can be used directly.

ESO target matching uses the archive's target-name field. Using the archive's naming convention can therefore improve results.

The response contains observations with information such as:

- Dataset ID
- Target
- Instrument
- Observation date
- RA/Dec
- Exposure time
- Wavelength information
- Proposal information
- Principal investigator
- Data rights
- Archive provenance

---

## 2. Download an Observation

Once an observation has been selected:

```http
POST /api/archives/download
```

Example:

```bash
curl -X POST http://localhost:5279/api/archives/download \
  -H "Content-Type: application/json" \
  -d '{
    "archive": "Mast",
    "datasetId": "hst_12345_01_wfc3_uvis_f814w"
  }'
```

The archive client uses the archive's product/DataLink information to discover the downloadable product rather than constructing a download URL from assumptions about the filename.

The result contains a `fileId`:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "archive": "Mast",
  "sizeBytes": 41984000
}
```

That `fileId` is used by subsequent analysis requests.

---

## 3. Upload Your Own FITS File

Local observations can be uploaded directly:

```http
POST /api/fits/upload
```

The FITS bytes are streamed directly to storage.

There is no requirement to wrap the file in a multipart form and the entire FITS dataset does not need to be buffered in application memory.

Example:

```bash
curl -X POST http://localhost:5279/api/fits/upload \
  --data-binary @m31.fits \
  -H "Content-Type: application/octet-stream"
```

Response:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "sizeBytes": 41984000
}
```

This is particularly useful for FITS files produced by personal telescopes, observatory instruments, or other astronomy software.

---

# Working with a FITS Dataset

Once a FITS file has a `fileId`, the same dataset can be passed to the analysis endpoints.

## FITS Inspection

```http
GET /api/fits/{fileId}/header
```

This endpoint parses the FITS structure and returns information about the HDUs and their available scientific capabilities.

It can be used to determine whether a dataset contains:

- Image data
- WCS information
- Spectral data
- Time-series tables
- Other FITS table data

The dataset is not forced into a single mutually exclusive category. Capabilities are determined from the actual HDUs present in the file.

---

## Data Quality

```http
GET /api/fits/{fileId}/quality
```

Reports cross-cutting data-quality statistics for a staged dataset's image data, distinguishing pixels that are not present, not measurable, or measured-as-zero.

The response includes:

- NaN and infinite pixel counts
- Invalid and valid pixel counts
- Minimum, maximum, mean, and standard deviation
- Estimated background and noise
- Dynamic range
- Saturation threshold (from the header when available) and saturated pixel count/fraction
- Usable-pixel fraction
- Quality flags

---

## Observation Metadata

```http
GET /api/fits/{fileId}/observation
```

Reports observation metadata and provenance for a staged FITS file, keeping values read directly from the FITS header distinct from values AstroLab derives from the WCS.

The response includes:

- Header-sourced fields such as target, observation date, telescope, instrument, observer, exposure time, filter, RA/Dec, detector size, gain, and checksum/archive identifiers
- Derived fields: dataset kind classification, WCS presence, projection, pixel scale, and rotation

---

# Image Analysis

All image endpoints use:

```text
/api/images/{fileId}/...
```

## Render an Image

```http
GET /api/images/{fileId}/render
```

Produces a browser-consumable PNG representation of an image HDU.

Supported controls include:

- `stretch`
  - `Linear`
  - `Logarithmic`
  - `SquareRoot`
  - `Asinh`
- `colorMap`
  - `Grayscale`
  - `Viridis`
  - `Hot`
- `blackPoint`
- `whitePoint`
- `lowerPercentile`
- `upperPercentile`
- `asinhSoftening`
- `maxDimension`

Example:

```bash
curl "http://localhost:5279/api/images/FILE_ID/render?stretch=Asinh&colorMap=Viridis" \
  -o image.png
```

Rendering is a visualisation operation. It does not alter the underlying scientific pixel data.

---

## Image Statistics

```http
GET /api/images/{fileId}/statistics
```

Returns statistics such as:

- Minimum
- Maximum
- Mean
- Median
- Standard deviation
- Valid pixel count
- Invalid/dead pixel count
- Background statistics
- Percentiles

These values are useful for understanding the dynamic range and quality of an astronomical image before performing measurements.

---

## Histogram

```http
GET /api/images/{fileId}/histogram?binCount=64
```

Returns a pixel-value histogram suitable for plotting or further analysis by a client application.

---

## Source Detection

```http
GET /api/images/{fileId}/sources
```

Detects candidate astronomical sources above a configurable background threshold.

Parameters include:

- `thresholdSigma`
- `minimumArea`
- `maxSources`

When a valid WCS is available, detected sources can also be associated with celestial RA/Dec coordinates.

---

## Source Segmentation

```http
GET /api/images/{fileId}/segmentation
```

Produces pixel-level segments associated with detected sources.

This provides the connection between source detection and subsequent measurements of individual objects.

---

## Source Characterization

```http
GET /api/images/{fileId}/sources/characterization
```

Measures properties such as:

- Semi-major axis
- Semi-minor axis
- Orientation
- Ellipticity

These measurements can help distinguish point-like and extended sources and provide useful information for subsequent astronomical analysis.

---

## Background Estimation

```http
GET /api/images/{fileId}/background
```

Calculates a mesh-based background model.

The mesh size can be controlled using:

```text
meshSizePixels
```

The result includes background and RMS information useful for source detection and photometric measurements.

---

## Source Overlay

```http
GET /api/images/{fileId}/render/overlay
```

Renders an image while overlaying detected sources.

This provides a convenient visual check of whether the source-detection parameters are identifying the expected astronomical objects.

---

## Cutout

```http
GET /api/images/{fileId}/cutout
```

Extracts a rectangular region from a staged image and renders it as a PNG.

The region can be specified either in pixel space:

```text
x, y, width, height
```

or as a WCS-based sky region:

```text
rightAscension, declination, radiusArcseconds
```

Example:

```bash
curl "http://localhost:5279/api/images/FILE_ID/cutout?x=400&y=380&width=256&height=256" -o cutout.png
```

---

## Contours

```http
GET /api/images/{fileId}/contours
```

Generates scientific contour geometry (marching-squares polylines) from an image's pixel data, at configurable or automatically calculated levels.

Parameters include:

- `levels` — explicit contour levels
- `levelCount` — number of automatically calculated levels, when `levels` is omitted

The result reports, for each level, the traced polylines as pixel-coordinate points, suitable for client-side plotting or overlay rendering.

---

## RGB Composite

```http
POST /api/images/composite
```

Combines up to three separate staged images into an RGB colour composite, one per channel, each independently percentile-scaled.

Example:

```json
{
  "redFileId": "frame-r",
  "greenFileId": "frame-g",
  "blueFileId": "frame-b"
}
```

The three channel images must share the same pixel dimensions.

---

## WCS Grid Overlay

```http
GET /api/images/{fileId}/render/wcs-grid
```

Renders a staged image to PNG with a WCS right-ascension/declination coordinate grid overlaid.

The `linesPerAxis` parameter controls grid density. Pixel scale, orientation, and mirroring are reported in response headers.

---

## Image Comparison

```http
POST /api/images/compare
```

Compares two staged images of the same dimensions.

Example:

```json
{
  "fileId": "frame-001",
  "comparisonFileId": "frame-002"
}
```

The result includes difference statistics such as:

- Mean difference
- Standard deviation
- Maximum absolute difference

---

## Image Alignment

```http
POST /api/images/align
```

Calculates a registration transform between an image and a reference image.

Example:

```json
{
  "fileId": "frame-001",
  "referenceFileId": "frame-002"
}
```

The transform can include:

- Translation
- Rotation
- Scale

This is useful when multiple observations of the same field have been captured at different positions or orientations.

---

## Image Stacking

```http
POST /api/images/stack
```

Combines multiple staged images into a new FITS dataset.

Supported combination methods include:

- Mean
- Median

Example request:

```json
{
  "fileIds": ["frame-001", "frame-002", "frame-003"],
  "method": "Median"
}
```

The result is a new staged FITS file that can itself be passed to the analysis endpoints.

---

# Astrometry

Astrometry connects an image's pixel coordinate system to the celestial coordinate system.

Routes are available under:

```text
/api/images/{fileId}/astrometry/...
```

## WCS

```http
GET /api/images/{fileId}/astrometry/wcs
```

Returns information from the image's FITS WCS solution, including:

- Projection
- Reference pixel
- Reference celestial coordinates
- Pixel scale
- Rotation

---

## Pixel to World

```http
GET /api/images/{fileId}/astrometry/pixel-to-world
```

Converts:

```text
pixel X/Y
```

into:

```text
Right Ascension / Declination
```

Example:

```bash
curl "http://localhost:5279/api/images/FILE_ID/astrometry/pixel-to-world?pixelX=512&pixelY=498"
```

---

## World to Pixel

```http
GET /api/images/{fileId}/astrometry/world-to-pixel
```

Performs the inverse transformation.

Example:

```bash
curl "http://localhost:5279/api/images/FILE_ID/astrometry/world-to-pixel?rightAscension=10.6847&declination=41.269"
```

Example result:

```json
{
  "fileId": "FILE_ID",
  "pixelX": 512.4,
  "pixelY": 498.1
}
```

---

## Image Footprint

```http
GET /api/images/{fileId}/astrometry/footprint
```

Calculates the celestial footprint of the image from its WCS and pixel dimensions.

The result describes the region of sky covered by the image.

---

## Angular Separation

```http
GET /api/images/{fileId}/astrometry/separation
```

Calculates the angular separation between two pixel positions using the image's WCS.

The result is returned in arcseconds.

---

## Pixel Scale

```http
GET /api/images/{fileId}/astrometry/pixel-scale
```

Reports the angular pixel scale (arcsec/pixel and degrees/pixel, per axis) derived from the image's WCS.

---

## Orientation

```http
GET /api/images/{fileId}/astrometry/orientation
```

Reports the image's position angle relative to celestial north, and whether it is mirrored, derived from the image's WCS.

---

## Batch Coordinate Conversion

```http
POST /api/images/{fileId}/astrometry/pixel-to-world
POST /api/images/{fileId}/astrometry/world-to-pixel
```

Converts multiple pixel or world coordinates in a single request.

Example request for the batch pixel-to-world endpoint:

```json
{
  "points": [
    { "pixelX": 512.4, "pixelY": 498.1 },
    { "pixelX": 600.0, "pixelY": 420.0 }
  ]
}
```

---

## WCS Validation

```http
GET /api/images/{fileId}/astrometry/validate
```

Validates the image's WCS solution: invertibility, axis orthogonality, pixel-scale symmetry, and pixel-to-world-to-pixel round-trip consistency.

---

# Photometry

Photometry measures the brightness of astronomical objects.

Routes are available under:

```text
/api/images/{fileId}/photometry/...
```

## Aperture Photometry

```http
POST /api/images/{fileId}/photometry/aperture
```

Example:

```json
{
  "centerX": 512.4,
  "centerY": 498.1,
  "apertureRadius": 10,
  "annulusInnerRadius": 14,
  "annulusOuterRadius": 20,
  "backgroundMethod": "Median"
}
```

The aperture contains the target source.

The surrounding annulus estimates the local sky background.

The basic background-subtracted flux is:

\[
F*{\mathrm{net}}
=
F*{\mathrm{total}}

- A*{\mathrm{aperture}} I*{\mathrm{background}}
  \]

where:

- \(F\_{\mathrm{total}}\) is the total measured flux inside the aperture
- \(A\_{\mathrm{aperture}}\) is the aperture area
- \(I\_{\mathrm{background}}\) is the estimated background intensity per pixel

The response includes values such as:

```json
{
  "fileId": "FILE_ID",
  "rawFlux": 184230.5,
  "apertureArea": 314.16,
  "backgroundPerPixel": 12.4,
  "netFlux": 180334.6
}
```

---

## Photometry of Detected Sources

```http
GET /api/images/{fileId}/photometry/sources
```

Detects sources and performs aperture photometry for each one.

The result can include:

- Source position
- Flux
- Instrumental magnitude
- Uncertainty
- Background estimate

Parameters include:

```text
thresholdSigma
minimumArea
maxSources
apertureRadius
annulusInnerRadius
annulusOuterRadius
magnitudeZeroPoint
```

---

## Differential Photometry

```http
POST /api/images/{fileId}/photometry/differential
```

Measures the magnitude difference between a target and comparison source in the same image.

Example:

```json
{
  "targetCenterX": 512,
  "targetCenterY": 498,
  "comparisonCenterX": 650,
  "comparisonCenterY": 470,
  "apertureRadius": 8,
  "annulusInnerRadius": 12,
  "annulusOuterRadius": 18
}
```

Differential photometry is particularly useful for monitoring relative brightness changes because the comparison source provides a reference against common observational variations.

---

## Photometric Uncertainty

```http
POST /api/images/{fileId}/photometry/uncertainty
```

Estimates the propagated flux uncertainty of an aperture measurement, accounting for source shot noise, sky-background noise, and read noise, using the CCD equation when a detector gain is available (from the request, or the FITS `GAIN` header).

---

## Signal-to-Noise Ratio

```http
POST /api/images/{fileId}/photometry/snr
```

Measures aperture flux and its propagated uncertainty, then reports the resulting signal-to-noise ratio for the same aperture/annulus parameters used by aperture photometry.

---

## Aperture Correction

```http
POST /api/images/{fileId}/photometry/aperture-correction
```

Applies a multiplicative aperture correction to a measured flux, propagating uncertainty where supplied.

Example:

```json
{
  "measuredFlux": 180334.6,
  "correctionFactor": 1.05,
  "measuredFluxUncertainty": 420.1
}
```

---

# Spectroscopy

Spectroscopic routes are available under:

```text
/api/spectroscopy/{fileId}/...
```

## Spectral Extraction

```http
POST /api/spectroscopy/{fileId}/extract
```

Extracts a one-dimensional spectrum from a spectroscopic image using boxcar extraction.

The extraction can operate along either the horizontal or vertical image axis and supports trace positions and an extraction aperture.

---

## Wavelength Calibration

```http
POST /api/spectroscopy/{fileId}/calibrate
```

Fits a polynomial wavelength-dispersion relationship using known pixel/wavelength pairs.

The response includes:

- Dispersion coefficients
- Residual RMS

This converts detector pixel positions into physical wavelengths.

---

## Continuum Fitting

```http
POST /api/spectroscopy/{fileId}/continuum
```

Fits a polynomial continuum model to a one-dimensional spectrum, with optional wavelength exclusion ranges and sigma-clipping, without mutating the original spectrum.

Example:

```json
{
  "polynomialDegree": 3,
  "excludedRanges": [{ "minWavelength": 6550, "maxWavelength": 6580 }],
  "sigmaClipThreshold": 3.0,
  "sigmaClipIterations": 2
}
```

The response includes the wavelength grid, original flux, fitted continuum, and polynomial coefficients.

---

## Continuum Subtraction

```http
POST /api/spectroscopy/{fileId}/continuum/subtract
```

Fits and subtracts a continuum model from a one-dimensional spectrum, using the same parameters as continuum fitting.

The response includes the wavelength grid, original flux, and continuum-subtracted flux.

---

## Spectral-Line Detection

```http
GET /api/spectroscopy/{fileId}/lines
```

Detects significant spectral features, including potential absorption and emission lines.

A significance threshold can be supplied to control detection sensitivity.

---

## Spectral-Line Fitting

```http
POST /api/spectroscopy/{fileId}/lines/fit
```

Fits a Gaussian profile to a spectral line over a supplied wavelength region, optionally seeded with initial centre, amplitude, and FWHM estimates.

The response includes the fitted baseline, amplitude, centre, and FWHM (each with uncertainty), integrated flux, and reduced chi-square.

---

## Equivalent Width

```http
POST /api/spectroscopy/{fileId}/equivalent-width
```

Calculates the equivalent width of a spectral feature over a supplied wavelength interval.

Example:

```json
{
  "minWavelength": 6550,
  "maxWavelength": 6580
}
```

---

## Redshift

```http
POST /api/spectroscopy/{fileId}/redshift
```

Estimates redshift from observed and rest-frame spectral-line wavelengths.

The fundamental relation is:

\[
z =
\frac{\lambda*{\mathrm{obs}}-\lambda*{\mathrm{rest}}}
{\lambda\_{\mathrm{rest}}}
\]

where:

- \(\lambda\_{\mathrm{obs}}\) is the observed wavelength
- \(\lambda\_{\mathrm{rest}}\) is the laboratory/rest wavelength
- \(z\) is the redshift

---

## Spectral Signal-to-Noise Ratio

```http
GET /api/spectroscopy/{fileId}/snr
```

Reports overall and per-sample spectral signal-to-noise ratio for a staged spectrum.

---

## Spectral Comparison

```http
POST /api/spectroscopy/{fileId}/compare
```

Cross-correlates one spectrum against another to identify similarity between their spectral structures.

---

# Time-Series Analysis

Time-series routes are available under:

```text
/api/timeseries/{fileId}/...
```

They operate on FITS tables containing observational measurements at multiple times.

## Light Curve

```http
GET /api/timeseries/{fileId}/light-curve
```

Extracts a time-versus-flux light curve from a FITS table.

The native CFITSIO library is used to read the FITS table.

---

## Detrending

```http
POST /api/timeseries/{fileId}/detrend
```

Removes long-term trends from a light curve.

Supported methods include:

- `linear`
- `median`

Detrending is useful when instrumental or observational trends are much larger than the variation being investigated.

---

## Phase Folding

```http
POST /api/timeseries/{fileId}/phase-fold
```

Folds a light curve around a supplied period and reference epoch, preserving the original time values alongside the computed phase.

Example:

```json
{
  "period": 3.14,
  "referenceEpoch": 2459000.5
}
```

---

## Light-Curve Comparison

```http
POST /api/timeseries/{fileId}/compare
```

Compares two light curves using measures such as:

- Pearson correlation
- Mean instrumental-magnitude offset

---

## Period Search

```http
GET /api/timeseries/{fileId}/period-search
```

Searches for periodic signals using a Lomb-Scargle periodogram.

Parameters include:

```text
minPeriod
maxPeriod
```

This is useful for detecting periodic behaviour such as:

- Variable stars
- Stellar rotation
- Binary systems
- Repeating observational signals

---

## Variability Statistics

```http
GET /api/timeseries/{fileId}/variability
```

Calculates variability statistics for a staged light curve: mean, median, standard deviation, amplitude, RMS, and median absolute deviation.

---

## Transit Detection

```http
GET /api/timeseries/{fileId}/transit
```

Searches for periodic brightness decreases consistent with transiting objects.

The result can include:

- Best period
- Transit depth
- Transit duration
- Transit epoch

Parameters include:

```text
minPeriod
maxPeriod
minTransitDepth
```

A transit appears as a temporary decrease in observed brightness when a body passes across the stellar disk.

---

# Measurements

Higher-level measurements combine information from the lower-level analysis capabilities.

Routes are available under:

```text
/api/measurements/...
```

## Stellar Colour

```http
POST /api/measurements/{fileId}/stellar-colour
```

Calculates a colour index from photometry in two different bands.

The comparison image is supplied through:

```text
comparisonFileId
```

along with the target position and aperture.

---

## Stellar Temperature

```http
GET /api/measurements/stellar-temperature
```

Estimates stellar effective temperature from a B−V colour index.

The implementation uses the Ballesteros relation.

The result is expressed in Kelvin.

---

## Spectral Classification

```http
GET /api/measurements/{fileId}/spectral-classification
```

Provides a coarse stellar spectral classification using spectral absorption/emission-line characteristics.

The classification follows the familiar:

```text
O B A F G K M
```

sequence.

---

## Radial Velocity

```http
GET /api/measurements/{fileId}/radial-velocity
```

Calculates line-of-sight velocity from the observed shift of a spectral line.

The classical Doppler relationship is used from the supplied rest and observed wavelengths.

---

## Galaxy Morphology

```http
GET /api/measurements/{fileId}/galaxy-morphology
```

Measures properties of a detected extended source, including:

- Effective radius
- Ellipticity
- Concentration

A concentration-based classification provides a coarse:

- Elliptical
- Spiral
- Irregular

classification.

---

## Surface Brightness

```http
GET /api/measurements/{fileId}/surface-brightness
```

Calculates surface brightness in:

```text
mag / arcsec²
```

The image WCS supplies the pixel scale required to convert an image aperture into an angular area on the sky.

---

## Physical Size

```http
GET /api/measurements/physical-size
```

Converts an angular size and assumed distance into a physical size.

Parameters:

```text
angularSizeArcsec
distanceParsecs
```

The result is expressed in astronomical units.

---

# Catalogues

AstroLab integrates with public VizieR catalogues through the IVOA TAP service.

No API key or account is required.

Examples of catalogue identifiers include:

```text
I/355/gaiadr3
II/246/out
```

representing catalogues such as Gaia DR3 and 2MASS.

## Catalogue Query

```http
GET /api/catalogues/query
```

Performs a cone search around a celestial position.

Parameters:

```text
catalogueId
rightAscension
declination
radiusArcsec
maxResults
```

---

## Image Catalogue Cross-Match

```http
POST /api/catalogues/cross-match
```

The workflow is:

```text
FITS image
    │
    ▼
Source detection
    │
    ▼
Pixel positions
    │
    ▼
WCS transformation
    │
    ▼
RA / Dec
    │
    ▼
VizieR catalogue
    │
    ▼
Matched catalogue sources
```

The request supplies:

```json
{
  "fileId": "FILE_ID",
  "catalogueIds": ["I/355/gaiadr3"],
  "radiusArcsec": 2
}
```

This allows detected astronomical sources to be associated with external catalogue information.

---

# Astronomical Concepts

AstroLab is built around several fundamental observational-astronomy concepts.

## FITS

FITS is the standard format used throughout astronomy for exchanging scientific observational data.

A FITS file can contain:

- Scientific measurements
- Image pixels
- Tables
- Instrument metadata
- Observation metadata
- Celestial-coordinate information

The file is divided into **Header/Data Units (HDUs)**.

The header contains keyword/value pairs describing the associated data.

For example, a header can contain information about:

```text
EXPTIME
DATE-OBS
FILTER
INSTRUME
RA
DEC
CTYPE
CRPIX
CRVAL
CDELT
CD
```

AstroLab preserves the distinction between the FITS data itself and the metadata describing how that data should be interpreted.

---

# Astrometry and WCS

A telescope records an image using detector coordinates:

```text
X, Y
```

Astronomers ultimately want celestial coordinates:

```text
Right Ascension, Declination
```

The **World Coordinate System (WCS)** stored in a FITS header defines the transformation between these coordinate systems.

Conceptually:

```text
Detector
  Pixel coordinates
       │
       │ WCS transformation
       ▼
Celestial sphere
  RA / Dec
```

This allows AstroLab to answer questions such as:

> Which part of the sky does this image contain?

or:

> Where in the image is the object at RA 10.6847°, Dec 41.269°?

The sky coordinate system is especially important when combining AstroLab's source detection with external catalogues.

---

# Image Analysis

Astronomical images contain far more than just visible stars.

A typical image may contain:

- Point sources
- Extended galaxies
- Nebulae
- Background sky
- Detector noise
- Bad pixels
- Cosmic-ray contamination
- Saturated pixels

AstroLab therefore separates several stages of image analysis.

```text
FITS pixels
     │
     ▼
Background estimation
     │
     ▼
Source detection
     │
     ├────► Segmentation
     │
     └────► Characterization
                 │
                 ▼
             Photometry
```

The image renderer is deliberately separate from the scientific analysis. Changing the colour map or image stretch changes how the data is displayed, not the underlying scientific measurements.

---

# Photometry

Photometry measures the brightness of astronomical sources.

For aperture photometry, AstroLab defines:

```text
       Annulus
    ┌─────────────┐
    │             │
    │   Aperture  │
    │      ●      │
    │             │
    └─────────────┘
```

The aperture measures the target.

The surrounding annulus estimates the local sky background.

The background contribution is then removed from the total aperture flux.

The resulting quantity is **net instrumental flux**.

Instrumental magnitudes can then be calculated from flux ratios.

Absolute calibrated magnitudes require additional calibration information such as a photometric zero point and, depending on the observation, instrument/filter/atmospheric calibration.

---

# Spectroscopy and Redshift

A spectrograph spreads incoming light by wavelength.

Instead of a 2D image of an object, the analysis ultimately produces a one-dimensional spectrum:

```text
Flux
 │
 │       /\          /\
 │      /  \        /  \
 │_____/____\______/____\____ Wavelength
```

Spectral lines provide information about the physical source.

If a known spectral line is observed at a different wavelength from its laboratory value, the shift can be expressed as:

\[
z =
\frac{\lambda*{\mathrm{obs}}-\lambda*{\mathrm{rest}}}
{\lambda\_{\mathrm{rest}}}
\]

This redshift can then be related to the motion of the emitting/absorbing object and, for sufficiently distant astronomical objects, to cosmic expansion.

---

# Time-Series Astronomy

A single image provides information about an object at one point in time.

A sequence of observations provides a **light curve**:

```text
Brightness
   │
   │ ────────╲      ╱────────
   │          ╲____╱
   │
   └────────────────────────── Time
```

Repeated observations can reveal changes that are invisible in a single image.

Examples include:

- Variable stars
- Eclipsing binaries
- Stellar rotation
- Transiting exoplanets
- Periodic stellar activity

AstroLab can extract, detrend, compare, and search these time-series measurements for periodic behaviour.

The Lomb-Scargle method is particularly useful for astronomical observations because observations are not always evenly spaced in time.

---

# End-to-End Example

The following example demonstrates a complete workflow using an M31 observation from MAST.

## 1. Search MAST

```bash
curl "http://localhost:5279/api/archives/search?archive=Mast&target=M31&instrument=WFC3&maxResults=5"
```

A result may contain:

```json
{
  "observations": [
    {
      "datasetId": "hst_12345_01_wfc3_uvis_f814w",
      "target": "M31",
      "instrument": "WFC3/UVIS",
      "observationDate": "2011-08-...",
      "source": "Mast",
      "rightAscension": 10.6847,
      "declination": 41.269,
      "exposureTimeSeconds": 1200
    }
  ]
}
```

---

## 2. Download the Observation

```bash
curl -X POST http://localhost:5279/api/archives/download \
  -H "Content-Type: application/json" \
  -d '{
    "archive": "Mast",
    "datasetId": "hst_12345_01_wfc3_uvis_f814w"
  }'
```

Suppose this returns:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "archive": "Mast",
  "sizeBytes": 41984000
}
```

Save the `fileId`.

---

## 3. Inspect the FITS Dataset

```bash
curl "http://localhost:5279/api/fits/20260906-142233-a1b2c3-fits/header"
```

This identifies the HDUs and available capabilities.

---

## 4. Render the Image

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/render?stretch=Asinh&colorMap=Viridis" \
  -o m31.png
```

The asinh stretch is useful for astronomical images because it can display faint structure while retaining brighter features.

---

## 5. Inspect Image Statistics

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/statistics"
```

And inspect the histogram:

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/histogram?binCount=64"
```

---

## 6. Detect Sources

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/sources?thresholdSigma=5&minimumArea=5"
```

AstroLab identifies candidate sources in the image.

---

## 7. Use the WCS

Retrieve the WCS:

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/astrometry/wcs"
```

Then convert the approximate centre of M31:

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/astrometry/world-to-pixel?rightAscension=10.6847&declination=41.269"
```

For example:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "pixelX": 512.4,
  "pixelY": 498.1
}
```

---

## 8. Perform Aperture Photometry

Use the calculated pixel position:

```bash
curl -X POST \
  http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/photometry/aperture \
  -H "Content-Type: application/json" \
  -d '{
    "centerX": 512.4,
    "centerY": 498.1,
    "apertureRadius": 10,
    "annulusInnerRadius": 14,
    "annulusOuterRadius": 20,
    "backgroundMethod": "Median"
  }'
```

The resulting measurement gives the background-subtracted flux for the selected region.

The complete workflow is therefore:

```text
MAST
 │
 │ search
 ▼
Observation
 │
 │ download
 ▼
Staged FITS
 │
 ├──► FITS metadata
 │
 ├──► Image rendering
 │
 ├──► Image statistics
 │
 ├──► Source detection
 │
 ├──► WCS
 │      │
 │      ▼
 │    RA/Dec
 │
 └──► Photometry
        │
        ▼
      Flux
```

The same workflow can begin with an ESO observation or a FITS file uploaded from a user's own telescope.

---

# Architecture

AstroLab uses **Functional Core, Imperative Shell (FCIS)** together with **Vertical Slice Architecture** in the API.

The solution consists of four projects:

```text
AstroLab.slnx
│
├── src/
│   ├── AstroLab.Core/
│   ├── AstroLab.Infrastructure/
│   ├── AstroLab.Api/
│   └── AstroLab.Tests/
```

Dependency direction is one-way:

```text
AstroLab.Api
      │
      ▼
AstroLab.Infrastructure
      │
      ▼
AstroLab.Core

AstroLab.Tests
      │
      └──────► Core
      └──────► Infrastructure
      └──────► Api
```

## AstroLab.Core

The Core contains the scientific domain logic.

It is designed to be:

- Pure
- Deterministic
- Allocation-conscious
- Independent of ASP.NET Core
- Independent of infrastructure
- Free from I/O
- Free from native interop

Scientific algorithms operate on data rather than performing file or network operations themselves.

The Core contains areas such as:

```text
Fits/
Astrometry/
Imaging/
Photometry/
Spectroscopy/
TimeSeries/
Measurements/
Result/
```

---

## AstroLab.Infrastructure

Infrastructure owns external effects and platform-specific operations.

It contains functionality such as:

- FITS file access
- Native CFITSIO interop
- Unmanaged memory
- Local file storage
- ESO HTTP clients
- MAST HTTP clients
- VizieR integration
- FITS image rendering
- PNG encoding
- Streaming

---

## AstroLab.Api

The API project is the HTTP boundary.

Endpoints are organised as vertical slices around user-facing features.

Each endpoint follows the **REPR** model:

```text
Request
   │
   ▼
Endpoint
   │
   ▼
Processing
   │
   ▼
Response
```

The API is responsible for:

- HTTP request binding
- Validation
- Calling application/domain functionality
- Mapping results to HTTP responses
- Producing API DTOs
- ProblemDetails responses

---

# Storage

AstroLab does not currently require a database.

FITS datasets are staged on local disk:

```text
storage/
```

The location is configurable through:

```text
Storage:RootPath
```

Each staged dataset is referenced by a `fileId`.

The storage model is deliberately simple:

```text
Archive / Upload
       │
       ▼
Local FITS file
       │
       ▼
     fileId
       │
       ├──► Header
       ├──► Image
       ├──► Astrometry
       ├──► Photometry
       ├──► Spectroscopy
       ├──► Time Series
       └──► Measurements
```

Files are not automatically deleted. Storage therefore needs to be managed by the host environment.

## Persistent Storage in Docker

AstroLab always stages FITS files as ordinary files under `Storage:RootPath`, whether running directly on a developer machine or inside Docker. There is no database or object-storage layer, and the application contains no Docker-specific storage logic — persistence is entirely a matter of how the container is run.

- **Locally (`dotnet run`)**, the configured storage directory (`./storage` by default) is used directly on the host filesystem.
- **In Docker**, `Storage:RootPath` is set to `/app/storage` inside the container, and the recommended deployment bind mounts the host's `storage/` directory into it:

```text
Host filesystem
└── storage/
    └── observation.fits
            │
            │ Docker bind mount
            ▼
Container
└── /app/storage/
    └── observation.fits
            │
            ▼
       AstroLab API
```

Because the host directory *is* the container's storage directory (not a copy of it), FITS files survive `docker compose down` and container recreation without any migration or import step — the container is disposable, the `storage/` directory is not.

A Docker-managed named volume is deliberately **not** used for this purpose: a bind mount keeps staged FITS files directly visible and backup-able on the host, at a normal filesystem path, rather than hidden inside Docker's storage driver.

Two lifecycles to keep distinct:

- **Container lifecycle** — `docker compose up` / `down`, image rebuilds, container recreation. Ephemeral by design.
- **Host storage lifecycle** — the `storage/` directory. Deleting it **does** delete the persisted FITS data, independently of the container. Back up `storage/` like any other important data directory if its contents matter.

FITS files are intentionally not stored in PostgreSQL, another database, or S3/object storage — see [`spec.md` §5.4](spec.md#54-persistent-storage-and-deployment).

---

# Configuration

Infrastructure settings are configured through the application's normal .NET configuration system.

Important configuration areas include:

```text
Storage
Archives
```

Storage controls where staged FITS datasets are written.

Archive configuration contains the ESO and MAST service settings used by their respective clients.

The repository's configuration files provide the concrete defaults used during development.

---

# Error Handling

AstroLab uses `ProblemDetails` for expected API failures.

Examples include:

- Invalid request parameters
- Invalid FITS files
- Missing FITS capabilities
- Missing files
- Unsupported operations
- Archive search failures
- No matching archive observations
- Invalid scientific input

Responses use appropriate HTTP status codes such as:

```text
400 Bad Request
404 Not Found
422 Unprocessable Entity
```

Expected failures do not expose raw exception messages or stack traces to API consumers.

---

# Testing

The test project is:

```text
src/AstroLab.Tests
```

Run the complete suite with:

```bash
dotnet test src/AstroLab.Tests
```

Run a specific test class:

```bash
dotnet test src/AstroLab.Tests \
  --filter "FullyQualifiedName~ApertureEngineTests"
```

Run a specific test:

```bash
dotnet test src/AstroLab.Tests \
  --filter "DisplayName~<test name>"
```

API integration tests use:

```text
Microsoft.AspNetCore.Mvc.Testing
```

to run the API host in-process.

Tests cover areas including:

- FITS parsing
- FITS headers
- Image processing
- Photometry
- Astrometry
- Spectroscopy
- Time-series processing
- Native buffer ownership
- Native resource disposal
- Archive clients
- Streaming
- Rendering
- API request validation
- HTTP status codes
- DTO mapping
- Result-to-HTTP mapping
- Error handling
- Cancellation
- End-to-end FITS workflows

Native-library-dependent tests can detect the absence of CFITSIO and skip the native portion rather than making unrelated tests fail.

---

# Performance and Design

AstroLab is designed to process potentially very large astronomical datasets without unnecessarily loading entire files into managed memory.

Several design decisions support this.

## Functional Core

Scientific calculations are isolated from I/O and infrastructure.

This makes the mathematical operations:

- Deterministic
- Easier to test
- Easier to benchmark
- Independent of the HTTP layer

## Spans

Hot-path calculations use constructs such as:

```csharp
ReadOnlySpan<float>
ReadOnlySpan<byte>
```

where appropriate.

This allows algorithms to operate over existing memory without creating unnecessary arrays or other managed allocations.

## Unmanaged Memory

Native FITS data can involve large buffers.

AstroLab therefore uses explicit unmanaged-memory ownership where required, with deterministic disposal of native resources.

## Streaming

Large FITS files are streamed to storage rather than being buffered in their entirety in application memory.

This is particularly important for astronomical datasets that can reach hundreds of megabytes or more.

## Native Interop

CFITSIO is isolated behind the infrastructure boundary.

The scientific Core does not depend directly on the native library.

## Separation of Visualisation and Science

Rendering an astronomical image and analysing an astronomical image are different operations.

For example:

```text
FITS pixels
     │
     ├──────────────► Scientific analysis
     │                  │
     │                  ├── Statistics
     │                  ├── Source detection
     │                  ├── Photometry
     │                  └── Astrometry
     │
     └──────────────► Visualisation
                        │
                        ├── Stretch
                        ├── Colour map
                        └── PNG
```

A visual stretch or colour map therefore does not modify the underlying scientific data.

---

# Commands

The main development commands are:

```bash
# Build
dotnet build AstroLab.slnx

# Run tests
dotnet test src/AstroLab.Tests

# Run the API
dotnet run --project src/AstroLab.Api

# Build and run in Docker (bind mounts ./storage:/app/storage)
docker compose up -d
```

---

# Project Documentation

The repository contains three complementary sources of documentation:

### `README.md`

Practical documentation for understanding and using the running AstroLab API.

### `spec.md`

The authoritative engineering and architectural specification. It defines the project's requirements, coding standards, architectural constraints, implementation patterns, and testing requirements.

### `CLAUDE.md`

Development guidance for AI coding agents working within the repository. It complements `spec.md` rather than duplicating it.

---

## AstroLab at a Glance

```text
                    ┌──────────────────────┐
                    │      ESO / MAST      │
                    │       Archives       │
                    └──────────┬───────────┘
                               │
                               ▼
┌─────────────────┐     ┌───────────────┐
│  Telescope FITS │────►│ Staged FITS   │
│   / Local File  │     │    Dataset    │
└─────────────────┘     └───────┬───────┘
                                │
             ┌──────────────────┼──────────────────┐
             │                  │                  │
             ▼                  ▼                  ▼
          Images          Spectroscopy        Time Series
             │                  │                  │
       ┌─────┼─────┐            │            ┌─────┼─────┐
       │     │     │            │            │     │     │
       ▼     ▼     ▼            ▼            ▼     ▼     ▼
     WCS  Sources Photo      Lines        Detrend Period Transit
       │     │     │            │            │
       └─────┼─────┘            │            │
             │                  │            │
             └──────────┬───────┴────────────┘
                        ▼
                  Measurements
                        │
          ┌─────────────┼─────────────┐
          ▼             ▼             ▼
      Stars         Galaxies      Physical
      Colour        Morphology    Properties
                        │
                        ▼
                   Catalogues
                        │
                        ▼
                  VizieR / Gaia
```

AstroLab brings the complete path from **astronomical FITS data to usable scientific measurements** into a single .NET API.
