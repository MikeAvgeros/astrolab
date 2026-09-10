# AstroLab

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and
visualising FITS (Flexible Image Transport System) astronomical datasets from the **ESO** and
**MAST** archives, or from your own uploaded files.

The architecture is documented in full in [`spec.md`](spec.md) and [`CLAUDE.md`](CLAUDE.md). This
README is a practical, task-oriented guide to using the running API.

There is currently no database — every dataset is streamed onto local disk under `storage/`
(gitignored, path configurable via `Storage:RootPath`) and referred to afterwards by a `fileId`.
Nothing here is deleted automatically; staged files are yours to clean up.

## Running the API

```bash
dotnet build AstroLab.slnx
dotnet run --project src/AstroLab.Api
```

By default the API listens on `http://localhost:5279` (see
`src/AstroLab.Api/Properties/launchSettings.json`). In `Development`, an interactive Swagger UI is
served at `/swagger` (backed by the OpenAPI document at `/openapi/v1.json`) — the fastest way to
explore every request/response shape.

Or via Docker:

```bash
docker build -t astrolab-api .
docker run -p 8080:8080 -v astrolab-storage:/app/storage astrolab-api
```

All examples below assume the API is running at `http://localhost:5279` and use `curl`; swap in
Postman/Swagger UI/httpie as you prefer.

### Native dependency: CFITSIO

Most of the API — FITS header parsing, image rendering/statistics/photometry, astrometry,
spectral extraction — is pure managed C# and needs nothing extra. The one exception is the
binary/ASCII table-reading path backing `GET /api/timeseries/{fileId}/light-curve`, which calls
into the real [CFITSIO](https://heasarc.gsfc.nasa.gov/fitsio/) C library via P/Invoke.

- **Docker**: nothing to do. The image downloads, checksum-verifies, and compiles a pinned CFITSIO
  release from source in its own build stage, and the runtime stage ends up with `libcfitsio.so`
  already installed and `ldconfig`'d — see the `cfitsio-build` stage in the `Dockerfile`.
- **`dotnet run` (e.g. developing directly on Windows/macOS/Linux, not in Docker)**: CFITSIO is
  _not_ bundled with the app. You need to supply a compiled CFITSIO shared library yourself and
  make it loadable by the .NET runtime:
  - **Windows**: obtain or build `cfitsio.dll` (e.g. via [vcpkg](https://vcpkg.io) —
    `vcpkg install cfitsio` — or by compiling the same pinned source the Dockerfile uses with
    MSVC/CMake) and place it next to `AstroLab.Api.dll` in the build output
    (`src/AstroLab.Api/bin/Debug/net10.0/`), or anywhere on `PATH`.
  - **Linux/macOS**: install/build `libcfitsio.so`/`libcfitsio.dylib` and make sure it's
    discoverable (`ldconfig`, `LD_LIBRARY_PATH`, or your OS's package manager equivalent).
  - Without it, `/light-curve` fails and the corresponding native-backed test suite
    (`CfitsIoTimeSeriesReaderTests`, `FitsFileHandleTests`, `TimeSeriesWorkflowTests`) dynamically
    skips rather than fails — everything else in this README works regardless.

## The core workflow

Every analysis endpoint operates on a **staged file**, identified by an opaque `fileId`. You get a
`fileId` one of three ways, then run any number of read-only analysis calls against it:

```text
 ┌───────────────┐      ┌───────────────┐     ┌────────────────┐
 │Search ESO/MAST├────► │ Download to   │────►│                │
 └───────────────┘      │ local storage │     │   fileId       │
                        └───────────────┘     │                │──► Inspect / Render / Measure / Extract
 ┌───────────────┐                            │                │
 │Upload your own├─────────────────────────── │                │
 │FITS file      │                            └────────────────┘
 └───────────────┘
```

A FITS file is not treated as one single "kind" of data — a dataset can carry image, spectral,
time-series, and WCS capabilities simultaneously across its HDUs. Each analysis endpoint checks for
the capability it needs and returns a `Result`-mapped error (see [Error handling](#error-handling))
if that capability isn't present.

---

## 1. Getting a FITS file onto disk

### Option A — Search an archive

`GET /api/archives/search`

| Query parameter       | Type   | Notes                              |
| --------------------- | ------ | ---------------------------------- |
| `archive`             | enum   | `Eso` or `Mast` (required)         |
| `target`              | string | Target name (required)             |
| `mission`             | string | Optional mission/collection filter |
| `instrument`          | string | Optional instrument filter         |
| `searchRadiusDegrees` | double | Optional cone-search radius        |
| `maxResults`          | int    | Default `50`                       |

- **MAST**: `target` is resolved through MAST's own name-resolution service, so common names
  (`M31`, `NGC 224`, `Andromeda Galaxy`) work directly.
- **ESO**: `target` is matched as a substring against the archive's `target_name` field, so it
  helps to use the archive's own naming convention (e.g. `M 31` with the space, as ESO records it).

The response (`ObservationSearchResponse`) is a list of `ArchiveObservationDto`, each carrying a
`datasetId` you can hand to the download endpoint, plus `target`, `instrument`, `observationDate`,
`rightAscension`/`declination`, `exposureTimeSeconds`, wavelength range, and provenance fields
(`proposalId`, `proposalPi`, `dataRights`) where the archive supplies them.

### Option B — Download a known dataset

`POST /api/archives/download`

```json
{ "archive": "Mast", "datasetId": "<datasetId from search>" }
```

The API discovers the real downloadable product for that observation through the archive's own
product/DataLink API (it never guesses a download URL from a filename), streams it to local
storage, and returns:

```json
{ "fileId": "20260906-...-fits", "archive": "Mast", "sizeBytes": 41984000 }
```

The `Location` response header points at `/api/fits/{fileId}/header` so you can immediately inspect
what you just downloaded.

### Option C — Upload your own FITS file

`POST /api/fits/upload` — send the raw FITS bytes as the request body (streamed directly to
storage; no multipart form wrapper, no in-memory buffering of the whole file):

```bash
curl -X POST http://localhost:5279/api/fits/upload \
  --data-binary @m31.fits \
  -H "Content-Type: application/octet-stream"
```

Response is the same shape as a download: `{ "fileId": "...", "sizeBytes": ... }`.

---

## 2. Inspecting and analysing a staged file

Everything below takes the `fileId` from step 1.

### FITS structure

| Method & route                  | Description                                                                 |
| ------------------------------- | --------------------------------------------------------------------------- |
| `GET /api/fits/{fileId}/header` | Parses every HDU, classifies its scientific capabilities, returns metadata. |

### Image analysis (`/api/images/{fileId}/...`)

| Method & route                   | Description                                                                                                                                                                                                                                            |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `GET /render`                    | Renders the first image HDU to a PNG. Query: `stretch` (`Linear`\|`Logarithmic`\|`SquareRoot`\|`Asinh`), `colorMap` (`Grayscale`\|`Viridis`\|`Hot`), `blackPoint`/`whitePoint`, `lowerPercentile`/`upperPercentile`, `asinhSoftening`, `maxDimension`. |
| `GET /statistics`                | Min/max/mean/median/stddev, valid/dead pixel counts, sky background sigma, percentiles.                                                                                                                                                                |
| `GET /histogram`                 | Pixel-value histogram (`binCount` query param) for client-side plotting.                                                                                                                                                                               |
| `GET /sources`                   | Detects candidate point sources (`thresholdSigma`, `minimumArea`, `maxSources`); reports RA/Dec per source when the file carries a usable WCS.                                                                                                         |
| `POST /photometry/aperture`      | Background-subtracted aperture photometry at a pixel position. Body: `centerX`, `centerY`, `apertureRadius`, `annulusInnerRadius`, `annulusOuterRadius`, `backgroundMethod`.                                                                           |
| `GET /photometry/sources`        | Aperture photometry (instrumental magnitude + uncertainty) for every source the detector finds. Query: `thresholdSigma`, `minimumArea`, `maxSources`, `apertureRadius`, `annulusInnerRadius`, `annulusOuterRadius`, `magnitudeZeroPoint`.              |
| `POST /photometry/differential`  | Differential magnitude between a target and comparison aperture in the same image. Body: `targetCenterX`/`Y`, `comparisonCenterX`/`Y`, `apertureRadius`, `annulusInnerRadius`, `annulusOuterRadius`.                                                   |
| `GET /astrometry/wcs`            | Reports the WCS solution (projection, reference pixel/coordinates, pixel scale, rotation).                                                                                                                                                             |
| `GET /astrometry/pixel-to-world` | Converts `pixelX`/`pixelY` to RA/Dec via the WCS.                                                                                                                                                                                                      |
| `GET /astrometry/world-to-pixel` | Converts `rightAscension`/`declination` to a pixel position via the WCS.                                                                                                                                                                               |
| `GET /astrometry/footprint`      | Reports the sky footprint (corner RA/Dec) of the image, derived from its WCS and pixel dimensions.                                                                                                                                                     |
| `GET /astrometry/separation`     | Angular separation (arcsec) between two pixel positions in the same image, via its WCS. Query: `firstPixelX`/`Y`, `secondPixelX`/`Y`.                                                                                                                  |
| `GET /background`                | Mesh-based background model: median background and RMS. Query: `meshSizePixels`.                                                                                                                                                                       |
| `GET /segmentation`              | Per-source pixel segments from threshold-based detection. Query: `thresholdSigma`, `minimumArea`.                                                                                                                                                      |
| `GET /sources/characterization`  | Shape/ellipticity measurements (semi-major/minor axis, orientation) per detected source. Query: `thresholdSigma`, `minimumArea`, `maxSources`.                                                                                                         |
| `GET /render/overlay`            | Renders the image to PNG with detected sources overlaid as markers. Query: `thresholdSigma`, `minimumArea`, `maxSources`.                                                                                                                              |
| `POST /compare`                  | Pixel-difference statistics (mean/stddev/max absolute difference) between two staged images of the same dimensions. Body: `fileId`, `comparisonFileId`.                                                                                                |
| `POST /align`                    | Registration transform (offset, rotation, scale) aligning one staged image to a reference image. Body: `fileId`, `referenceFileId`.                                                                                                                    |
| `POST /stack`                    | Combines multiple staged images (mean/median) into a new staged FITS file. Body: `fileIds`, `method`.                                                                                                                                                  |

### Spectroscopy (`/api/spectroscopy/{fileId}/...`)

| Method & route    | Description                                                                                                                                                                                                                                                       |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `POST /extract`   | Extracts a 1D boxcar flux spectrum from an image HDU classified as spectral data, optionally wavelength-calibrated from dispersion coefficients. Body: `axis` (`Horizontal`\|`Vertical`), `traceCenters`, `apertureHalfWidth`, optional `dispersionCoefficients`. |
| `POST /calibrate` | Fits a polynomial wavelength-dispersion solution (least squares) from known pixel/wavelength pairs. Body: `pixelPositions`, `knownWavelengths`. Returns `dispersionCoefficients` and `residualRms`.                                                               |
| `GET /lines`      | Detects spectral lines in a 1D spectrum collapsed across the full spatial extent of a spectroscopic frame. Query: optional `significanceThreshold`.                                                                                                               |
| `POST /redshift`  | Estimates redshift from paired observed/rest-frame spectral line wavelengths. Body: `observedWavelengths`, `restWavelengths`.                                                                                                                                     |
| `POST /compare`   | Cross-correlates this spectrum against another staged spectrum. Body: `comparisonFileId`.                                                                                                                                                                         |

### Time series (`/api/timeseries/{fileId}/...`)

| Method & route       | Description                                                                                                                                                                               |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET /light-curve`   | Extracts flux-vs-time from a staged time-series FITS table. Requires a native CFITSIO library — see [Native dependency: CFITSIO](#native-dependency-cfitsio).                             |
| `POST /detrend`      | Removes a long-term trend from a light curve. Body: `method` (`linear`\|`median`). Requires a native CFITSIO library.                                                                     |
| `POST /compare`      | Compares two staged light curves via Pearson correlation and mean instrumental-magnitude offset. Body: `comparisonFileId`. Requires a native CFITSIO library.                             |
| `GET /period-search` | Finds the best-fit periodicity (Lomb-Scargle) in a light curve. Query: `minPeriod`, `maxPeriod`. Requires a native CFITSIO library.                                                       |
| `GET /transit`       | Searches for periodic transit (brightness-dip) signals: best period, transit depth/duration/epoch. Query: `minPeriod`, `maxPeriod`, `minTransitDepth`. Requires a native CFITSIO library. |

### Catalogues (`/api/catalogues/...`)

Roadmap only at present (see below).

---

## Roadmap endpoints (HTTP 501)

The following slices are scaffolded with a final request/response contract but no scientific
implementation yet — they always return HTTP 501 rather than a fake or partial result. Do not build
against their response bodies expecting real numbers yet:

- `GET/POST /api/measurements/*` — galaxy morphology, physical size, radial velocity, spectral
  classification, stellar colour, stellar temperature, surface brightness
- `GET /api/catalogues/query`; `POST /api/catalogues/cross-match`

---

## Error handling

Expected failures (invalid FITS data, a missing capability, an archive returning no results, bad
request parameters, etc.) come back as a `ProblemDetails` JSON body with an appropriate HTTP status
code (`400`, `404`, `422`, etc.) — never a raw exception message or stack trace. Roadmap endpoints
use the same shape with status `501`.

---

## Worked example: M31 (Andromeda Galaxy) end to end via MAST

This walks through searching MAST, downloading a real observation, and running the currently
implemented analyses against it.

**1. Search MAST for M31**

```bash
curl "http://localhost:5279/api/archives/search?archive=Mast&target=M31&instrument=WFC3&maxResults=5"
```

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
      "exposureTimeSeconds": 1200,
      "...": "..."
    }
  ]
}
```

**2. Download the chosen observation**

```bash
curl -X POST http://localhost:5279/api/archives/download \
  -H "Content-Type: application/json" \
  -d '{"archive":"Mast","datasetId":"hst_12345_01_wfc3_uvis_f814w"}'
```

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "archive": "Mast",
  "sizeBytes": 41984000
}
```

Save `fileId` — every following call uses it.

**3. Inspect what you got**

```bash
curl "http://localhost:5279/api/fits/20260906-142233-a1b2c3-fits/header"
```

Confirms the HDU layout and which capabilities (image, WCS, ...) the file actually has.

**4. Render a preview PNG**

```bash
curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/render?stretch=Asinh&colorMap=Viridis" \
  -o m31-preview.png
```

**5. Get pixel statistics and a histogram**

```bash
curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/statistics"
curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/histogram?binCount=64"
```

**6. Detect sources in the field (star clusters, HII regions, foreground stars)**

```bash
curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/sources?thresholdSigma=5&minimumArea=5"
```

**7. Look up the WCS, then convert M31's core coordinates (RA 10.6847°, Dec 41.269°) to a pixel**

```bash
curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/astrometry/wcs"

curl "http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/astrometry/world-to-pixel?rightAscension=10.6847&declination=41.269"
```

```json
{ "fileId": "20260906-142233-a1b2c3-fits", "pixelX": 512.4, "pixelY": 498.1 }
```

**8. Run aperture photometry centred on the galactic core**

```bash
curl -X POST http://localhost:5279/api/images/20260906-142233-a1b2c3-fits/photometry/aperture \
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

```json
{
  "fileId": "20260906-142233-a1b2c3-fits",
  "rawFlux": 184230.5,
  "apertureArea": 314.16,
  "backgroundPerPixel": 12.4,
  "netFlux": 180334.6
}
```

That's the full path from "find a galaxy in an archive" to "photometric measurement on a staged
FITS file," using only currently-implemented endpoints. The same flow works with `archive=Eso` and
an ESO-style target string (e.g. `M 31`), or by skipping steps 1–2 entirely and uploading your own
FITS file at step 3's `fileId`.

# The Science Behind the AstroLab API

AstroLab automates core observational astronomy techniques on **FITS** (Flexible Image Transport System) files—the universal file standard that stores raw pixel grids alongside metadata detailing exposure time, instrument parameters, and celestial pointing.

## Astrometry (World Coordinate System - WCS)

Cameras capture images on 2D pixel grids ($X, Y$). AstroLab uses WCS mathematical matrices stored in FITS headers to project flat pixel coordinates into universal sky coordinates: Right Ascension (RA, longitude equivalent) and Declination (Dec, latitude equivalent).

## Aperture Photometry

To measure the brightness of a star, the API places a central circular mask (aperture) over the target and an outer ring (annulus) around it. It sums the total light in the center, calculates average background sky noise in the ring, and subtracts the background to calculate net flux ($F_{	ext{net}}$):

$$F_{	ext{net}} = F_{	ext{total}} - (A_{	ext{aperture}} \times I_{	ext{background}})$$

## Spectroscopy & Redshift

Prism-like optics split light into individual wavelengths. AstroLab extracts 1D light profiles, detects absorption/emission lines, and calculates cosmological redshift ($z$) by comparing observed wavelengths ($\lambda_{	ext{obs}}$) against stationary laboratory wavelengths ($\lambda_{	ext{rest}}$):

$$z = \frac{\lambda_{	ext{obs}} - \lambda_{	ext{rest}}}{\lambda_{	ext{rest}}}$$

## Time-Series Analysis (Light Curves)

By tracking brightness across multiple time steps, the API builds light curves used to discover transiting exoplanets or variable stars, applying detrending algorithms to strip out instrumental drift.
