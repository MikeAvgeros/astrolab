# AstroLab

**AstroLab** is a .NET 10 / C# 14 REST API for working with astronomical data.

It can download observations from the **ESO** and **MAST**, accept your own FITS files, and then turn those datasets into something you can inspect, visualise and analyse programmatically.

The API currently supports:

- 🔭 Searching the ESO and MAST archives
- 📥 Downloading astronomical observations
- 📁 Uploading your own FITS files
- 🧩 Inspecting FITS HDUs and their scientific capabilities
- 🖼️ Rendering astronomical images
- 📊 Image statistics and histograms
- ⭐ Source detection and source characterisation
- 📐 Astrometry using FITS World Coordinate System (WCS) information
- 💡 Aperture and differential photometry
- 🌈 Spectral extraction and analysis
- ⏱️ Time-series and light-curve analysis
- 🪐 Transit and periodicity searches
- 🌡️ Stellar colour and temperature estimation
- 🌌 Galaxy morphology and surface-brightness measurements
- 📚 Cross-matching detected sources against astronomical catalogues
- 🔬 Image comparison, alignment and stacking

The project is intended to bridge the gap between **raw astronomical observations** and the kinds of measurements an astronomer would normally perform when analysing those observations.

The architecture is documented in [`spec.md`](spec.md) and [`CLAUDE.md`](CLAUDE.md). This README focuses on **using AstroLab and understanding what the API does**.

---

# The AstroLab workflow

AstroLab follows a simple staged-file workflow.

First, get a FITS dataset into AstroLab by either:

1. Searching ESO or MAST and downloading an observation.
2. Uploading your own FITS file.

Once a dataset has been staged, the API returns an opaque `fileId`.

That `fileId` can then be used with the analysis endpoints.

```text
                         ┌─────────────────┐
                         │   ESO / MAST    │
                         │    archives     │
                         └────────┬────────┘
                                  │
                                Search
                                  │
                                  ▼
                         ┌─────────────────┐
                         │     Download    │
                         └────────┬────────┘
                                  │
                                  │
     ┌────────────────┐           ▼
     │ Your FITS file │────►  ┌──────────┐
     └────────────────┘       │  Staged  │
                              │   FITS   │
                              │  fileId  │
                              └────┬─────┘
                                   │
             ┌─────────────────────┼──────────────────────┐
             │          │            │           │        │
             ▼          ▼            ▼           ▼        ▼
          Images   Astrometry   Photometry  Spectroscopy Time Series
             │          │            │           │        │
             └──────────┴────────────┴───────────┴────────┘
                                    │
                                    ▼
                           Scientific measurements
```

A FITS file is **not** assumed to represent one particular kind of data.

A single dataset can contain multiple HDUs and can provide several capabilities at once — for example, an image may contain both image data and WCS information, while another HDU in the same file may contain a time-series table.

Each analysis endpoint therefore checks whether the staged dataset provides the capability it requires.

---

# Getting started

## Build the API

```bash
dotnet build AstroLab.slnx
```

## Run locally

```bash
dotnet run --project src/AstroLab.Api
```

The API listens on:

```text
http://localhost:5279
```

The port is configured in:

```text
src/AstroLab.Api/Properties/launchSettings.json
```

When running in `Development`, Swagger UI is available at:

```text
http://localhost:5279/swagger
```

The OpenAPI document is available at:

```text
http://localhost:5279/openapi/v1.json
```

Swagger is the easiest way to explore the available endpoints and their request/response models.

---

# Running with Docker

Build the image:

```bash
docker build -t astrolab-api .
```

Run it:

```bash
docker run \
  -p 8080:8080 \
  -v astrolab-storage:/app/storage \
  astrolab-api
```

The storage volume is important because AstroLab stages downloaded and uploaded FITS files on local disk.

There is currently **no database**. Files are stored under:

```text
storage/
```

The location can be configured using:

```text
Storage:RootPath
```

Storage is gitignored and files are **not automatically deleted**. The staged datasets therefore remain available until they are cleaned up manually.

---

# Native dependency: CFITSIO

Most of AstroLab is implemented in managed C#.

This includes:

- FITS header parsing
- Image rendering
- Image statistics
- Photometry
- Astrometry
- Spectral extraction
- Most numerical and scientific calculations

The main native dependency is **CFITSIO**, the widely used C library for reading and writing FITS files.

AstroLab currently uses CFITSIO specifically for the binary/ASCII FITS table-reading path used by the time-series endpoints.

## Docker

Nothing needs to be installed manually.

The Docker build:

1. Downloads a pinned CFITSIO release.
2. Verifies its checksum.
3. Builds it in a dedicated build stage.
4. Installs `libcfitsio.so` into the runtime image.
5. Runs `ldconfig` so the .NET application can load it through P/Invoke.

## Running directly with `dotnet run`

CFITSIO is **not bundled with the application** when running directly on Windows, macOS or Linux.

A compatible native library must therefore be installed separately.

### Windows

Provide:

```text
cfitsio.dll
```

For example, using vcpkg:

```bash
vcpkg install cfitsio
```

The DLL can either be placed next to `AstroLab.Api.dll` in the build output or made available through `PATH`.

### Linux / macOS

Provide the appropriate shared library:

```text
libcfitsio.so
```

or:

```text
libcfitsio.dylib
```

It must be discoverable through the operating system's normal library mechanisms, such as `ldconfig`, `LD_LIBRARY_PATH` or the system package manager.

Without CFITSIO, the time-series endpoints that depend on FITS table reading will not work. The rest of the API remains usable.

---

# API overview

The API is organised around the main stages of astronomical analysis:

| Area         | What it does                                                    |
| ------------ | --------------------------------------------------------------- |
| Archives     | Find and download real astronomical observations                |
| FITS         | Inspect the structure and metadata of a dataset                 |
| Images       | Render and analyse image data                                   |
| Astrometry   | Connect image pixels to positions on the sky                    |
| Photometry   | Measure the brightness of astronomical sources                  |
| Spectroscopy | Extract and analyse wavelength information                      |
| Time series  | Analyse brightness as a function of time                        |
| Measurements | Turn observations into higher-level astronomical measurements   |
| Catalogues   | Compare detected sources with published astronomical catalogues |

The sections below describe what each endpoint is for and when you would use it.

---

# 1. Getting astronomical data

## Search ESO or MAST

```http
GET /api/archives/search
```

Searches one of the supported astronomical archives.

### Query parameters

| Parameter             | Type   | Description                                 |
| --------------------- | ------ | ------------------------------------------- |
| `archive`             | enum   | `Eso` or `Mast`                             |
| `target`              | string | Target name                                 |
| `mission`             | string | Optional mission/collection filter          |
| `instrument`          | string | Optional instrument filter                  |
| `searchRadiusDegrees` | double | Optional cone-search radius                 |
| `maxResults`          | int    | Maximum number of results; defaults to `50` |

### MAST target names

MAST resolves target names through its own name-resolution service.

This means common names such as:

```text
M31
NGC 224
Andromeda Galaxy
```

can be used directly.

### ESO target names

ESO searches the archive's `target_name` field.

It is therefore useful to use ESO's naming convention. For example:

```text
M 31
```

may work better than:

```text
M31
```

depending on how the observation is recorded.

### Response

The response contains `ArchiveObservationDto` records describing the observations found.

Typical information includes:

- Dataset ID
- Target
- Instrument
- Observation date
- Right Ascension
- Declination
- Exposure time
- Wavelength range
- Proposal ID
- Proposal PI
- Data rights
- Archive provenance

The important field for the next step is:

```text
datasetId
```

---

## Download an observation

```http
POST /api/archives/download
```

Downloads a selected archive observation and stages it locally.

Example:

```json
{
  "archive": "Mast",
  "datasetId": "<datasetId from search>"
}
```

AstroLab asks the archive for the actual downloadable product rather than constructing a filename or download URL itself.

The downloaded FITS file is streamed directly to local storage.

Example response:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "archive": "Mast",
  "sizeBytes": 41984000
}
```

The returned `fileId` becomes the identifier for all subsequent analysis.

The response also provides a `Location` header pointing to the FITS header endpoint.

---

## Upload your own FITS file

```http
POST /api/fits/upload
```

AstroLab can also work with FITS files that did not come from ESO or MAST.

The file is sent as the raw request body:

```bash
curl -X POST \
  http://localhost:5279/api/fits/upload \
  --data-binary @m31.fits \
  -H "Content-Type: application/octet-stream"
```

The upload is streamed directly to disk rather than loading the entire FITS file into memory.

The response contains a `fileId` and file size:

```json
{
  "fileId": "20260906-...",
  "sizeBytes": 41984000
}
```

From this point onwards, an uploaded file behaves exactly like an archived observation.

---

# 2. Understanding a FITS dataset

## Inspect the FITS header

```http
GET /api/fits/{fileId}/header
```

This is usually the best first request after obtaining a `fileId`.

It parses the FITS structure and reports information about its HDUs and available scientific capabilities.

This helps answer questions such as:

- How many HDUs does this FITS file contain?
- Which HDU contains the image?
- Is there WCS information?
- Does the dataset contain a time-series table?
- Is the data suitable for spectroscopy?
- What metadata does the instrument provide?

Because AstroLab uses capabilities rather than a single mutually-exclusive FITS "type", a dataset can expose several capabilities simultaneously.

---

# 3. Image analysis

Image endpoints live under:

```text
/api/images/{fileId}
```

These endpoints operate on image HDUs and provide the building blocks for astronomical image analysis.

---

## Render an astronomical image

```http
GET /api/images/{fileId}/render
```

Converts an image HDU into a PNG suitable for viewing.

Astronomical detector data often has a much wider dynamic range than a normal display can show. AstroLab therefore supports different intensity stretches.

### Available stretches

- `Linear`
- `Logarithmic`
- `SquareRoot`
- `Asinh`

The API also supports colour maps such as:

- `Grayscale`
- `Viridis`
- `Hot`

Additional controls include:

- `blackPoint`
- `whitePoint`
- `lowerPercentile`
- `upperPercentile`
- `asinhSoftening`
- `maxDimension`

For example:

```bash
curl \
  "http://localhost:5279/api/images/{fileId}/render?stretch=Asinh&colorMap=Viridis" \
  -o preview.png
```

An `Asinh` stretch is particularly useful for astronomical images because it can simultaneously reveal faint structures while retaining detail in bright sources.

---

## Image statistics

```http
GET /api/images/{fileId}/statistics
```

Calculates descriptive statistics for the image.

The response includes measurements such as:

- Minimum
- Maximum
- Mean
- Median
- Standard deviation
- Valid pixel count
- Dead/invalid pixel count
- Background statistics
- Percentiles

These are useful for understanding the detector data before performing more advanced analysis.

---

## Histogram

```http
GET /api/images/{fileId}/histogram
```

Produces a histogram of pixel values.

The number of bins can be controlled using:

```text
binCount
```

For example:

```bash
curl \
  "http://localhost:5279/api/images/{fileId}/histogram?binCount=64"
```

The histogram is intended to be consumed by a client for plotting or further analysis.

---

## Detect sources

```http
GET /api/images/{fileId}/sources
```

Searches the image for candidate astronomical sources.

Detection is based on the significance of pixels above the estimated background.

Parameters include:

- `thresholdSigma`
- `minimumArea`
- `maxSources`

When usable WCS information is available, detected pixel positions can also be converted into sky coordinates.

This endpoint can therefore turn an image from:

> "Here is a grid of detector values."

into:

> "Here are the candidate sources detected in the field, and here is where they are on the sky."

---

## Source characterisation

```http
GET /api/images/{fileId}/sources/characterization
```

Measures the shape of detected sources.

Measurements include:

- Semi-major axis
- Semi-minor axis
- Orientation
- Ellipticity-related properties

These measurements can help distinguish point-like objects from extended sources and provide information about how sources appear in the image.

---

## Source segmentation

```http
GET /api/images/{fileId}/segmentation
```

Returns the pixels belonging to detected sources based on threshold-based segmentation.

This provides more detailed information than simply returning a source centroid and is useful for downstream measurements.

---

## Background estimation

```http
GET /api/images/{fileId}/background
```

Builds a mesh-based background model.

The main configuration parameter is:

```text
meshSizePixels
```

The result provides an estimate of the local sky background and its RMS variation.

Background estimation is important because astronomical images contain not only source photons but also sky background, detector effects and noise.

---

## Render with detected sources

```http
GET /api/images/{fileId}/render/overlay
```

Produces a PNG containing the rendered image with detected sources marked on top.

This is useful as a quick visual sanity check of the source-detection algorithm.

---

# 4. Astrometry

Astrometry answers a fundamental question:

> **Where on the sky does this pixel correspond to?**

AstroLab uses the **World Coordinate System (WCS)** stored in FITS headers to establish this relationship.

## Inspect the WCS

```http
GET /api/images/{fileId}/astrometry/wcs
```

Returns the WCS solution, including information such as:

- Projection
- Reference pixel
- Reference sky coordinates
- Pixel scale
- Rotation

---

## Pixel → sky coordinates

```http
GET /api/images/{fileId}/astrometry/pixel-to-world
```

Converts:

```text
X, Y pixel position
```

into:

```text
Right Ascension, Declination
```

This is useful when you have identified an object in the image and want to determine its celestial coordinates.

---

## Sky coordinates → pixel

```http
GET /api/images/{fileId}/astrometry/world-to-pixel
```

Performs the inverse transformation.

Given:

```text
Right Ascension
Declination
```

it determines the corresponding image pixel.

This is useful for locating a known astronomical object in an image.

---

## Image footprint

```http
GET /api/images/{fileId}/astrometry/footprint
```

Calculates the region of the sky covered by the image.

The response contains the sky coordinates of the image corners.

This is particularly useful when comparing an observation against an external catalogue.

---

## Angular separation

```http
GET /api/images/{fileId}/astrometry/separation
```

Calculates the angular separation between two positions in the same image.

The positions are supplied in pixel coordinates and converted to sky coordinates using the image's WCS.

The result is returned in arcseconds.

---

# 5. Photometry

Photometry is the measurement of the brightness of astronomical objects.

AstroLab currently provides aperture-based photometry.

## Aperture photometry

```http
POST /api/images/{fileId}/photometry/aperture
```

Measures the flux inside a circular aperture and estimates the background using a surrounding annulus.

Example request:

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

The result includes values such as:

- Raw flux
- Aperture area
- Background per pixel
- Background-subtracted/net flux

The basic calculation is:

$$
F_{\mathrm{net}}
=
F_{\mathrm{total}}
-
A_{\mathrm{aperture}}
\times
I_{\mathrm{background}}
$$

This is one of the fundamental measurements in observational astronomy: determining how much light from a source actually belongs to the source rather than the surrounding sky.

---

## Photometry of detected sources

```http
GET /api/images/{fileId}/photometry/sources
```

Runs source detection followed by aperture photometry for the detected sources.

The result includes instrumental magnitude and uncertainty estimates.

Parameters include:

- `thresholdSigma`
- `minimumArea`
- `maxSources`
- `apertureRadius`
- `annulusInnerRadius`
- `annulusOuterRadius`
- `magnitudeZeroPoint`

This provides a convenient path from:

```text
Image → detected sources → brightness measurements
```

---

## Differential photometry

```http
POST /api/images/{fileId}/photometry/differential
```

Compares the brightness of two apertures in the same image:

- Target
- Comparison source

The result is a differential magnitude.

Differential photometry is particularly useful for variable-star and transit work because many changes affecting the whole image — such as atmospheric transparency — can partially cancel when comparing a target with a nearby reference source.

---

# 6. Comparing and combining images

## Compare two images

```http
POST /api/images/{fileId}/compare
```

Compares two staged images of the same dimensions.

The response includes pixel-difference statistics such as:

- Mean difference
- Standard deviation
- Maximum absolute difference

This can be useful for assessing whether two observations differ significantly.

---

## Align two images

```http
POST /api/images/{fileId}/align
```

Calculates a registration transform between an image and a reference image.

The transform can include:

- Translation
- Rotation
- Scale

This is useful before combining images that were taken at slightly different positions or orientations.

---

## Stack multiple images

```http
POST /api/images/{fileId}/stack
```

Combines multiple staged images into a new FITS dataset.

Supported methods include:

- Mean
- Median

Image stacking is a standard astronomical technique for improving signal-to-noise by combining multiple observations of the same field.

The result is a **new staged FITS file** with its own `fileId`.

---

# 7. Spectroscopy

Spectroscopy turns an image containing dispersed light into information about the wavelengths emitted or absorbed by an astronomical object.

Endpoints live under:

```text
/api/spectroscopy/{fileId}
```

---

## Extract a spectrum

```http
POST /api/spectroscopy/{fileId}/extract
```

Extracts a one-dimensional spectrum from a spectroscopic image using boxcar extraction.

The extraction can be performed along either:

```text
Horizontal
Vertical
```

The request can specify:

- Trace centre(s)
- Aperture half-width
- Optional wavelength-dispersion coefficients

The result is a 1D representation of:

```text
flux vs wavelength
```

when wavelength calibration is available.

---

## Wavelength calibration

```http
POST /api/spectroscopy/{fileId}/calibrate
```

Fits a polynomial dispersion relation from known pixel/wavelength pairs.

Input:

```text
pixel position → known wavelength
```

Output:

- Dispersion coefficients
- Residual RMS

This provides the mapping required to turn detector pixels into physically meaningful wavelengths.

---

## Detect spectral lines

```http
GET /api/spectroscopy/{fileId}/lines
```

Searches a spectrum for significant spectral features.

The optional:

```text
significanceThreshold
```

controls the detection threshold.

Spectral lines can correspond to atomic or molecular transitions and provide information about the physical properties and motion of astronomical sources.

---

## Estimate redshift

```http
POST /api/spectroscopy/{fileId}/redshift
```

Estimates redshift by comparing observed spectral-line wavelengths with their known rest-frame wavelengths.

The fundamental relation is:

$$
z =
\frac{\lambda_{\mathrm{obs}}-\lambda_{\mathrm{rest}}}
{\lambda_{\mathrm{rest}}}
$$

A positive redshift means the observed wavelength is longer than the rest wavelength.

For small velocities, redshift can be related approximately to radial velocity by:

$$
v \approx cz
$$

where `c` is the speed of light.

For sufficiently large redshifts, however, this simple classical approximation is not appropriate for interpreting cosmological distances or velocities.

---

## Compare spectra

```http
POST /api/spectroscopy/{fileId}/compare
```

Cross-correlates one staged spectrum against another.

This can be used to investigate whether two spectra contain similar features or whether one spectrum is shifted relative to another.

---

# 8. Time-series analysis

Time-series endpoints operate on FITS tables containing measurements taken at different times.

They live under:

```text
/api/timeseries/{fileId}
```

These endpoints require the native CFITSIO dependency.

---

## Build a light curve

```http
GET /api/timeseries/{fileId}/light-curve
```

Extracts a light curve from a time-series FITS table.

The resulting data represents:

```text
time → measured brightness/flux
```

This is the starting point for analysing objects whose brightness changes over time.

---

## Detrend a light curve

```http
POST /api/timeseries/{fileId}/detrend
```

Removes long-term trends from a light curve.

Available methods include:

- `linear`
- `median`

Detrending is important because changes in brightness may come from the instrument or observing conditions rather than the astronomical source itself.

---

## Compare light curves

```http
POST /api/timeseries/{fileId}/compare
```

Compares two staged light curves using:

- Pearson correlation
- Mean instrumental-magnitude offset

This can help determine whether two objects show similar variability.

---

## Search for periodicity

```http
GET /api/timeseries/{fileId}/period-search
```

Uses a **Lomb–Scargle periodogram** to search for periodic signals.

The user supplies:

```text
minPeriod
maxPeriod
```

Lomb–Scargle is particularly useful for astronomical observations because observations are often **unevenly sampled** rather than occurring at perfectly regular time intervals.

The result identifies the strongest candidate period within the requested range.

---

## Search for transits

```http
GET /api/timeseries/{fileId}/transit
```

Searches for periodic brightness dips consistent with transits.

The search can be constrained using:

- `minPeriod`
- `maxPeriod`
- `minTransitDepth`

The result includes quantities such as:

- Best-fit period
- Transit depth
- Transit duration
- Transit epoch

This provides a simple pipeline from:

```text
Time-series observations
        ↓
      Light curve
        ↓
     Detrending
        ↓
 Periodicity search
        ↓
 Transit candidate
```

It is intended as an analysis tool rather than a complete exoplanet validation pipeline.

---

# 9. Higher-level astronomical measurements

The `/api/measurements` endpoints build on the lower-level image and spectroscopy functionality to produce more directly interpretable astronomical quantities.

---

## Stellar colour

```http
POST /api/measurements/{fileId}/stellar-colour
```

Measures a colour index between two observations taken in different photometric bands.

The request provides:

- Comparison image
- Source position
- Aperture radius

Colour indices describe how an object's brightness changes between filters and provide information about its spectral energy distribution.

---

## Stellar temperature

```http
GET /api/measurements/stellar-temperature
```

Estimates effective temperature from a B−V colour index using the Ballesteros (2012) relation.

The result is expressed in Kelvin.

This is an estimate rather than a direct measurement of the star's physical temperature. Real stellar atmospheres, metallicity, reddening and photometric calibration can all affect the relationship between colour and temperature.

---

## Spectral classification

```http
GET /api/measurements/{fileId}/spectral-classification
```

Provides a coarse OBAFGKM spectral classification based on the density of detected absorption/emission features.

The familiar sequence:

```text
O → B → A → F → G → K → M
```

roughly corresponds to decreasing stellar temperature.

The classification implemented here is intentionally coarse and should be treated as an automated estimate rather than a professional spectroscopic classification pipeline.

---

## Radial velocity

```http
GET /api/measurements/{fileId}/radial-velocity
```

Calculates classical Doppler radial velocity from a rest-frame and observed wavelength.

The calculation is based on:

$$
v =
c
\frac{\lambda_{\mathrm{obs}}-\lambda_{\mathrm{rest}}}
{\lambda_{\mathrm{rest}}}
$$

The result describes motion along the line of sight.

As with the redshift endpoint, this classical approximation is most appropriate for relatively small velocities.

---

## Galaxy morphology

```http
GET /api/measurements/{fileId}/galaxy-morphology
```

Analyses the source nearest a supplied pixel position.

It calculates properties such as:

- Effective radius
- Ellipticity
- Concentration index

and uses those measurements to provide a coarse morphological classification:

```text
Elliptical
Spiral
Irregular
```

This should be regarded as a simplified image-based classification rather than a replacement for detailed galaxy morphology analysis.

---

## Surface brightness

```http
GET /api/measurements/{fileId}/surface-brightness
```

Calculates surface brightness in:

```text
mag / arcsec²
```

within an aperture.

The image's WCS is used to determine the angular size represented by the pixels.

This is important because total flux and surface brightness describe different physical properties: a large, faint galaxy and a small, bright source can have very different total brightnesses but overlapping surface-brightness characteristics.

---

## Physical size

```http
GET /api/measurements/physical-size
```

Converts an angular size and an assumed distance into a physical size.

Inputs:

- Angular size in arcseconds
- Distance in parsecs

Output:

- Physical size in AU

Conceptually, this is the astronomical equivalent of converting:

```text
"How large does it look?"
```

into:

```text
"How large is it actually?"
```

The result naturally depends on the assumed distance.

---

# 10. Astronomical catalogues

AstroLab can query public astronomical catalogues through **VizieR's IVOA TAP service**.

No API key or user account is required.

Catalogue identifiers use VizieR table names, for example:

```text
I/355/gaiadr3
```

for Gaia DR3, or:

```text
II/246/out
```

for 2MASS.

---

## Query a catalogue

```http
GET /api/catalogues/query
```

Performs a cone search around a sky position.

Parameters:

- `catalogueId`
- `rightAscension`
- `declination`
- `radiusArcsec`
- Optional `maxResults`

This is useful when you know the sky position of an object and want to find matching catalogue entries.

---

## Cross-match an image against catalogues

```http
POST /api/catalogues/cross-match
```

This endpoint connects several parts of AstroLab together.

The workflow is:

```text
Image
  ↓
Detect sources
  ↓
WCS pixel → RA/Dec
  ↓
Query catalogue
  ↓
Match nearby catalogue objects
```

The request specifies:

- `fileId`
- One or more `catalogueIds`
- Matching radius in arcseconds

This allows detected sources in an image to be associated with objects already recorded in major astronomical catalogues.

---

# Error handling

Expected failures are returned as standard `ProblemDetails` responses rather than raw exceptions.

Examples include:

- Invalid FITS data
- Missing scientific capability
- No archive results
- Invalid request parameters
- Missing files
- Unsupported analysis operations

Appropriate HTTP status codes are used, such as:

```text
400 Bad Request
404 Not Found
422 Unprocessable Entity
501 Not Implemented
```

Raw exception messages and stack traces are not returned to API consumers.

`501 Not Implemented` is returned by roadmap endpoints that have been scaffolded with a real route and request contract but do not yet have a scientific implementation behind them — see [Not implemented: roadmap endpoints](#not-implemented-roadmap-endpoints) below.

---

# Worked example: M31 from archive to photometry

The following example demonstrates the complete AstroLab workflow using M31 (the Andromeda Galaxy).

## 1. Find an observation

```bash
curl \
  "http://localhost:5279/api/archives/search?archive=Mast&target=M31&instrument=WFC3&maxResults=5"
```

A result contains information such as:

```json
{
  "datasetId": "hst_12345_01_wfc3_uvis_f814w",
  "target": "M31",
  "instrument": "WFC3/UVIS",
  "observationDate": "2011-08-...",
  "rightAscension": 10.6847,
  "declination": 41.269,
  "exposureTimeSeconds": 1200
}
```

---

## 2. Download it

```bash
curl -X POST \
  http://localhost:5279/api/archives/download \
  -H "Content-Type: application/json" \
  -d '{
    "archive": "Mast",
    "datasetId": "hst_12345_01_wfc3_uvis_f814w"
  }'
```

Response:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "archive": "Mast",
  "sizeBytes": 41984000
}
```

Keep the `fileId`. It identifies the staged dataset for the rest of the workflow.

---

## 3. Inspect the dataset

```bash
curl \
  "http://localhost:5279/api/fits/20260906-142233-a1b2c3-fits/header"
```

This tells you what the FITS file contains and which capabilities are available.

---

## 4. Render an image

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/render?stretch=Asinh&colorMap=Viridis" \
  -o m31-preview.png
```

---

## 5. Analyse the image

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/statistics"

curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/histogram?binCount=64"
```

---

## 6. Detect sources

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/sources?thresholdSigma=5&minimumArea=5"
```

---

## 7. Use WCS to locate an object

First inspect the WCS:

```bash
curl \
  "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/astrometry/wcs"
```

Then convert M31's approximate coordinates:

```text
RA  = 10.6847°
Dec = 41.269°
```

into image coordinates:

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

## 8. Measure the source

Run aperture photometry around the resulting position:

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

The response contains measurements such as:

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "rawFlux": 184230.5,
  "apertureArea": 314.16,
  "backgroundPerPixel": 12.4,
  "netFlux": 180334.6
}
```

At this point the API has taken you all the way from:

```text
Astronomical archive
      ↓
Observation
      ↓
FITS file
      ↓
Image
      ↓
Sky coordinates
      ↓
Detected/selected source
      ↓
Photometric measurement
```

---

# The Science Behind the AstroLab API

AstroLab is built around a simple idea:

> **Astronomical observations are measurements of light, and useful astronomy comes from turning those measurements into physical quantities.**

A FITS file provides the raw ingredients: detector measurements plus the metadata required to understand what those measurements mean.

AstroLab implements several of the standard techniques used to turn those ingredients into astronomical information.

---

## FITS: the foundation

**FITS — Flexible Image Transport System** — is the standard format used throughout astronomy for exchanging observational data.

A FITS file can contain:

- Images
- Tables
- Multiple HDUs
- Instrument metadata
- Observation times
- Exposure information
- Detector information
- Celestial coordinate information
- Wavelength calibration information

The distinction between the **data** and its **metadata** is particularly important.

For example, a pixel value by itself might simply be:

```text
184230.5
```

The FITS metadata can tell us:

- Which detector produced it
- When the observation was taken
- Which filter was being used
- Where the telescope was pointing
- How pixels map onto the sky
- What the exposure time was

AstroLab uses both the numerical data and this metadata throughout the analysis pipeline.

---

# From pixels to the sky: Astrometry

A telescope camera records an image as a rectangular array:

```text
       X →
   ┌───────────────┐
 Y │               │
 ↓ │     source    │
   │       ●       │
   │               │
   └───────────────┘
```

The pixel coordinates are not themselves celestial coordinates.

The FITS **World Coordinate System (WCS)** describes how those pixels map onto positions on the celestial sphere.

AstroLab uses that WCS information to perform transformations between:

```text
Pixel coordinates
      ↕
RA / Dec
```

This allows the API to answer questions such as:

- Where on the sky is this source?
- Where should a known object appear in this image?
- What area of the sky does this image cover?
- How far apart are two detected objects?

WCS therefore provides the bridge between **image processing** and **astronomy**.

---

# Measuring light: Photometry

Photometry is the measurement of an object's brightness.

A simple aperture-photometry measurement looks like:

```text
             background annulus
          ┌─────────────────────┐
          │                     │
          │      ┌───────┐      │
          │      │ source│      │
          │      │   ●   │      │
          │      └───────┘      │
          │                     │
          └─────────────────────┘
                aperture
```

The aperture contains the target's light.

The surrounding annulus estimates the local sky background.

If:

- `F_total` is the total flux inside the aperture,
- `A_aperture` is the aperture area,
- `I_background` is the estimated background intensity per pixel,

then the background-subtracted flux is:

$$
F_{\mathrm{net}}
=
F_{\mathrm{total}}
-
A_{\mathrm{aperture}}
I_{\mathrm{background}}
$$

This distinction is important because a detector measures both the astronomical source and everything contributing to the local background.

Once a flux has been measured, it can be converted into an instrumental magnitude:

$$
m = -2.5\log_{10}(F) + ZP
$$

where `ZP` is the photometric zero point.

The quality of the resulting physical magnitude depends on calibration, so instrumental photometry should not automatically be interpreted as calibrated apparent magnitude.

---

# Differential photometry

Instead of measuring an object in isolation, astronomers can compare it with a nearby reference object.

For example:

```text
Target star       Comparison star
    ●                   ●
    │                   │
    └──── brightness ───┘
             ↓
      relative change
```

If both objects are observed through approximately the same atmosphere and detector conditions, common variations can partially cancel.

This makes differential photometry particularly useful for:

- Variable stars
- Transit searches
- Monitoring changing brightness

A transit is a good example: the absolute brightness of the target may be affected by observing conditions, but its brightness relative to nearby comparison stars can reveal a small periodic dip.

---

# Understanding images: background, noise and sources

Astronomical images are not simply collections of bright dots.

They contain a combination of:

- Astronomical sources
- Sky background
- Detector noise
- Instrumental effects
- Cosmic-ray or other anomalous pixels
- Potentially very faint sources

AstroLab therefore separates several operations.

### Background estimation

Estimates the local sky level and RMS variation.

### Source detection

Looks for regions significantly above the estimated background.

### Segmentation

Determines which pixels belong to detected sources.

### Characterisation

Measures properties such as source size, ellipticity and orientation.

These steps provide the foundation for higher-level analysis such as photometry and galaxy morphology.

---

# Image stretching and visualisation

Raw astronomical pixel values often have a very different distribution from ordinary photographs.

A few very bright pixels can dominate the numerical range while faint astronomical structures remain almost invisible.

AstroLab therefore supports several display transformations.

### Linear

Directly maps pixel values to display intensity.

### Logarithmic

Compresses large dynamic ranges and makes faint structures easier to see.

### Square-root

Provides a compromise between linear and logarithmic scaling.

### Asinh

The inverse hyperbolic sine transformation is particularly useful for astronomical images because it behaves approximately linearly around zero while compressing large positive values.

This makes it possible to display both:

```text
very bright sources
        +
faint extended structure
```

in the same image.

The rendered image is therefore a **visual representation of the data**, not a replacement for the underlying numerical measurements.

---

# Spectroscopy: reading the fingerprint of light

Spectroscopy spreads light out by wavelength.

Instead of simply asking:

> "How bright is this object?"

spectroscopy asks:

> "How does its brightness change with wavelength?"

The result is a spectrum:

```text
Flux
 ↑
 │       /\                 /\
 │      /  \       /\      /  \
 │─────/────\─────/──\────/────\──→ wavelength
 │          ↑         ↑
 │       spectral   spectral
 │         line       line
```

Spectral lines are particularly valuable because atoms and molecules absorb or emit light at characteristic wavelengths.

This means a spectrum can reveal information about:

- Chemical composition
- Temperature
- Physical conditions
- Motion
- Stellar classification

AstroLab extracts a 1D spectrum from a spectroscopic image and can then identify significant spectral features.

---

# Wavelength calibration

The detector initially measures pixels, not wavelengths.

For example:

```text
pixel 100 → ?
pixel 200 → ?
pixel 300 → ?
```

Known spectral lines provide reference points:

```text
pixel position ↔ known wavelength
```

AstroLab fits a polynomial dispersion relationship to these points.

The calibrated spectrum can then be represented as:

```text
flux ↔ wavelength
```

rather than:

```text
flux ↔ detector pixel
```

The residual RMS returned by the calibration endpoint provides an indication of how well the fitted dispersion relation matches the supplied calibration points.

---

# Redshift and radial velocity

If an astronomical object is moving relative to the observer, its spectral lines shift.

For a rest wavelength:

$$
\lambda_{\mathrm{rest}}
$$

and an observed wavelength:

$$
\lambda_{\mathrm{obs}}
$$

the redshift is:

$$
z =
\frac{\lambda_{\mathrm{obs}}-\lambda_{\mathrm{rest}}}
{\lambda_{\mathrm{rest}}}
$$

A positive value means the wavelength has shifted towards the red end of the spectrum.

For relatively small velocities, radial velocity can be approximated using:

$$
v \approx cz
$$

or directly from the wavelength shift:

$$
v =
c
\frac{\lambda_{\mathrm{obs}}-\lambda_{\mathrm{rest}}}
{\lambda_{\mathrm{rest}}}
$$

This is the principle behind measuring the line-of-sight motion of stars and galaxies.

For large cosmological redshifts, however, `z` should not simply be interpreted as a classical velocity using `v = cz`.

---

# Time-series astronomy

Some astronomical observations are not primarily about spatial structure.

Instead, the important quantity is:

```text
brightness → time
```

This produces a **light curve**.

For example:

```text
Brightness
   ↑
   │  ● ● ● ●
   │ ●       ● ●
   │            ●
   │             ● ●
   │
   └──────────────────→ time
                 transit
```

A light curve can reveal:

- Variable stars
- Stellar rotation
- Pulsations
- Eclipsing binaries
- Exoplanet transits
- Other periodic or transient behaviour

---

# Detrending

Real observations contain trends that are not necessarily intrinsic to the astronomical object.

Examples include:

- Changing atmospheric conditions
- Instrumental drift
- Long-term baseline changes
- Systematic effects

Detrending attempts to remove these slow variations while retaining the shorter-scale signal of interest.

AstroLab currently provides linear and median-based detrending methods.

The purpose is not to manufacture a cleaner-looking graph, but to make subsequent searches for periodic or transient signals less sensitive to known long-term trends.

---

# Finding periodic signals

A periodic astronomical signal may not be sampled at perfectly regular intervals.

This is common in real observations because:

- Observations occur only at night
- Weather interrupts observations
- Telescope scheduling creates gaps
- Individual observations may have different timings

AstroLab therefore uses a **Lomb–Scargle periodogram** for periodicity searches.

Conceptually, the periodogram asks:

> "At which periods does a repeating signal best explain the observed brightness variations?"

The result can be used to identify candidate periods for variable stars or other periodic phenomena.

---

# Detecting exoplanet transits

A planetary transit occurs when a planet passes between its host star and the observer.

The observed brightness changes approximately like:

```text
Brightness
   │
   │───────────┐     ┌───────────
   │           └─────┘
   │
   └─────────────────────────────→ time
                 transit
```

The depth of the transit is related approximately to the ratio of the planet and star radii:

$$
\frac{\Delta F}{F}
\approx
\left(\frac{R_p}{R_*}\right)^2
$$

where:

- `R_p` is the planet radius
- `R_*` is the stellar radius

AstroLab's transit endpoint searches for periodic brightness dips and reports candidate properties such as period, depth, duration and epoch.

This is a **candidate-detection tool**, not a complete exoplanet confirmation pipeline. Confirming a candidate normally requires additional observations and astrophysical validation.

---

# Stellar colour and temperature

Stars emit approximately thermal spectra, but their observed colours depend on the shape of that spectrum and the filters through which they are observed.

A colour index compares brightness in two bands.

For example:

$$
B-V = m_B - m_V
$$

where `B` and `V` are standard photometric bands.

Hotter stars tend to be bluer, while cooler stars tend to be redder.

AstroLab uses a colour-temperature relationship to estimate effective temperature from B−V.

The result is an estimate of the star's **effective temperature** — the temperature of a blackbody that would emit the same total energy per unit surface area.

In real observations, effects such as:

- Interstellar reddening
- Metallicity
- Photometric calibration
- Stellar atmosphere physics

can cause the simple colour-temperature relationship to deviate from the true stellar temperature.

---

# Stellar spectral classification

The OBAFGKM sequence is one of the best-known classification systems in astronomy:

```text
O → B → A → F → G → K → M
```

The sequence primarily tracks stellar temperature.

Very roughly:

```text
O  hottest
B
A
F
G
K
M  coolest
```

Different spectral types exhibit characteristic absorption-line patterns because different atomic transitions become prominent at different temperatures.

AstroLab uses detected spectral features to produce a coarse automated OBAFGKM classification.

This is intended as an educational and analytical approximation rather than a replacement for a detailed professional spectral classification.

---

# Galaxy morphology

Galaxies can have very different structures.

Common broad categories include:

```text
Elliptical     Spiral        Irregular
   ◉           ◎            ✦  ✧
```

AstroLab uses image measurements such as:

- Effective radius
- Ellipticity
- Concentration

to produce a coarse morphology classification.

These quantities capture different aspects of the distribution of light.

For example, ellipticity describes how elongated a source appears, while concentration describes how centrally concentrated its light is.

The resulting classification is intentionally simplified. Real galaxy morphology is influenced by factors such as inclination, resolution, wavelength, dust and interactions with neighbouring galaxies.

---

# Surface brightness

Total brightness does not tell the whole story for an extended astronomical object.

Two objects can have the same total flux but distribute that flux over very different areas.

Surface brightness describes brightness per unit angular area.

AstroLab reports surface brightness in:

$$
\mathrm{mag/arcsec^2}
$$

The WCS is important here because detector pixels represent an angular area on the sky.

This lets AstroLab convert:

```text
pixels
   ↓
angular area
   ↓
brightness per arcsec²
```

Surface brightness is especially useful when studying extended objects such as galaxies and nebulae.

---

# Catalogue cross-matching

Astronomy rarely analyses an observation in isolation.

Once AstroLab has detected a source and converted its pixel position into RA/Dec, that position can be compared with external catalogues.

For example:

```text
                    AstroLab image
                         │
                    Source detection
                         │
                         ▼
                       RA/Dec
                         │
                         ▼
                ┌─────────────────┐
                │VizieR catalogue │
                └────────┬────────┘
                         │
                         ▼
                  Catalogue match
```

This makes it possible to associate an observed source with existing catalogue information.

For example, a detected star could potentially be matched with a Gaia catalogue entry containing additional astrometric or photometric information.

The cross-match radius determines how close a catalogue source must be to the detected sky position to be considered a match.

---

# Image stacking

Astronomical imaging often involves taking multiple exposures of the same field.

A single exposure may contain substantial noise, while genuine astronomical signal is repeated across exposures.

Combining the images can therefore improve the signal-to-noise ratio.

AstroLab supports mean and median stacking.

Conceptually:

```text
Exposure 1 ─┐
Exposure 2 ─┤
Exposure 3 ─┼──► stack ──► improved image
Exposure 4 ─┤
Exposure 5 ─┘
```

Median stacking can also help reject isolated anomalous pixels or transient artefacts, while mean stacking preserves the average signal.

Before stacking, images may need to be aligned so that the same astronomical source occupies the same position in every frame.

---

# What AstroLab is trying to provide

AstroLab is not intended to be a replacement for the complete ecosystem of professional astronomy software.

Instead, it provides a coherent API around a set of fundamental observational-astronomy workflows:

```text
             Astronomical observation
                       │
                       ▼
                    FITS
                       │
             ┌─────────┴─────────┐
             │                   │
          Image              Time series
             │                   │
       ┌─────┼─────┐          Light curve
       │     │     │             │
      WCS  Sources Photometry  Periodicity
       │     │                   │
       │     └──► Measurements  Transits
       │
       └──► Sky coordinates
               │
               ▼
           Catalogues

             Spectroscopic data
                       │
                       ▼
                 1D spectrum
                       │
              ┌────────┼────────┐
              │        │        │
          Lines    Redshift  Classification
              │
              ▼
        Physical information
```

The aim is to make these workflows available through a single REST API while keeping the underlying scientific calculations explicit and composable.

In other words, AstroLab takes you from:

> **"I have an astronomical FITS file."**

to:

> **"I can inspect it, see what it contains, locate objects on the sky, measure their light, analyse their spectra or variability, and compare them with astronomical catalogues."**

---

# Not implemented: roadmap endpoints

The endpoints below have been scaffolded with a real route and, where the request has a body, a real validated request contract. They are wired into the API today, but each one currently returns:

```text
HTTP 501 Not Implemented
```

with a stable error code identifying which capability is pending. No scientific calculation, Infrastructure access, or Core algorithm has been implemented for them yet — they exist so the eventual routes and request shapes are already stable. See `spec.md` §9 for the authoritative list and required FITS capability per endpoint.

## Astrometry

- `GET /api/images/{fileId}/astrometry/pixel-scale` — angular pixel scale (and per-axis scales) derived from the WCS.
- `GET /api/images/{fileId}/astrometry/orientation` — image orientation (position angle) relative to celestial north.
- `POST /api/images/{fileId}/astrometry/pixel-to-world` — converts multiple pixel positions to RA/Dec in one request.
- `POST /api/images/{fileId}/astrometry/world-to-pixel` — converts multiple RA/Dec coordinates to pixel positions in one request.

## Photometry

- `POST /api/images/{fileId}/photometry/aperture-correction` — corrects an aperture flux measurement using a supplied correction factor, propagating uncertainty where possible.

## Spectroscopy

- `POST /api/spectroscopy/{fileId}/continuum` — fits a polynomial continuum model to a spectrum.
- `POST /api/spectroscopy/{fileId}/continuum/subtract` — subtracts a fitted continuum from a spectrum.
- `POST /api/spectroscopy/{fileId}/lines/fit` — fits a Gaussian profile to a spectral line.
- `POST /api/spectroscopy/{fileId}/equivalent-width` — calculates equivalent width over a wavelength interval.
- `GET /api/spectroscopy/{fileId}/snr` — reports overall and per-sample spectral signal-to-noise ratio.

## Time series

- `POST /api/timeseries/{fileId}/phase-fold` — folds a light curve around a supplied period and reference epoch.
- `GET /api/timeseries/{fileId}/variability` — variability statistics (mean, median, standard deviation, amplitude, RMS, MAD).

The existing period-search endpoint (`GET /api/timeseries/{fileId}/period-search`) already works today; expanding its response to expose the full periodogram is future work on that existing endpoint, not a new roadmap stub.

## Image visualisation

- `GET /api/images/{fileId}/cutout` — extracts a rectangular pixel region, or a WCS-based sky region, from a staged image.
- `GET /api/images/{fileId}/contours` — generates scientific contour geometry from an image's pixel data.
- `POST /api/images/composite` — combines separate red/green/blue staged images into an RGB composite.

## Data quality

- `GET /api/fits/{fileId}/quality` — cross-cutting data-quality statistics (invalid pixel counts, saturation, dynamic range, usable-pixel fraction) for a staged FITS dataset.
