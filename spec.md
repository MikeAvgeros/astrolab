# AstroLab — Project Specification

This is the authoritative design and engineering reference for AstroLab. It defines the
architecture, coding standards, and implementation patterns that govern every project in this
repository — for human contributors and AI coding agents alike. It is also the original
specification the solution was scaffolded from (see §7 for that history).

For day-to-day operational details (build/test commands, current repo layout, local setup),
see `CLAUDE.md` instead.

> **How to use this document**
>
> - **Humans:** §1–§2 give context; §3–§6 are the standing reference to build against.
> - **AI agents:** §3 (Coding Standards) and §4.2 (Dependency Rules) are hard constraints —
>   check every diff you produce against them before finishing a task. §5 documents the
>   patterns each layer must follow; §6 documents what a passing test suite must prove.

## Contents

1. [Overview](#1-overview)
2. [Technology and Constraints](#2-technology-and-constraints)
3. [General Requirements (Coding Standards)](#3-general-requirements-coding-standards)
4. [Architecture](#4-architecture)
   - 4.1 [Solution Structure](#41-solution-structure)
   - 4.2 [Dependency Rules](#42-dependency-rules)
   - 4.3 [Request Flow](#43-request-flow)
   - 4.4 [FITS Dataset Classification](#44-fits-dataset-classification)
5. [Core Implementation Patterns](#5-core-implementation-patterns)
   - 5.1 [Result Pattern](#51-result-pattern)
   - 5.2 [Functional Core: Purity and Spans](#52-functional-core-purity-and-spans)
   - 5.3 [Unmanaged Native Buffers](#53-unmanaged-native-buffers)
   - 5.4 [Pipeline Streaming](#54-pipeline-streaming)
   - 5.5 [Vertical Slice API Endpoints (REPR Pattern)](#55-vertical-slice-api-endpoints-repr-pattern)
   - 5.6 [Archive Clients: ESO and MAST](#56-archive-clients-eso-and-mast)
   - 5.7 [Visualization as a Separate Capability](#57-visualization-as-a-separate-capability)
   - 5.8 [Global Exception Handling](#58-global-exception-handling)
6. [Testing Standards](#6-testing-standards)
   - 6.1 [Core Unit Tests](#61-core-unit-tests)
   - 6.2 [Allocation Tests](#62-allocation-tests)
7. [Appendix: Original Build Sequence (Historical)](#7-appendix-original-build-sequence-historical)

---

## 1. Overview

**AstroLab** is a high-performance .NET 10 RESTful API platform that downloads, stores, parses,
analyzes, visualizes, and renders FITS (Flexible Image Transport System) scientific datasets from
astronomical archives (ESO and MAST) as well as direct user uploads.

The system is built on a **Functional Core, Imperative Shell (FCIS)** design: a pure, allocation-
conscious domain core (`AstroLab.Core`) is driven by an imperative shell (`AstroLab.Infrastructure`,
`AstroLab.Api`) that owns all I/O, native interop, and side effects. Domain and infrastructure
outcomes are modeled with `Result<T>` — a hand-rolled discriminated union (§5.1), used instead of
exceptions for expected failures. Native memory management (`cfitsio` P/Invoke bindings,
`ReadOnlySpan<T>`, `System.IO.Pipelines`) lets the system process multi-gigabyte astronomical files
with minimal Garbage Collector (GC) overhead.

A dedicated **FITS Image Visualization** capability provides browser-consumable representations of
2D FITS image data, including pixel scaling, image stretching, color mapping, NaN/invalid-pixel
handling, and image statistics.

---

## 2. Technology and Constraints

- **Target Framework:** .NET 10 / C# 14.
- **Database:** None. Metadata and raw datasets are staged on local disk (`AstroLab.Infrastructure/Storage`), not a SQL or NoSQL database.
- **Architecture Pattern:** Functional Core, Imperative Shell (FCIS), combined with Vertical Slice Architecture in the API layer (§4).
- **Solution Layout:** Exactly four projects — `AstroLab.Core`, `AstroLab.Infrastructure`, `AstroLab.Api`, `AstroLab.Tests` (full layout and dependency rules in §4.1–§4.2).
- **Error Handling:** `Result<T>` — a hand-rolled discriminated union (`Result`/`Error`) used for all expected domain and infrastructure outcomes; exceptions are reserved for genuinely unrecoverable failures (§5.1).
- **Performance:** Zero managed-heap allocation on hot pixel/byte-buffer paths, backed by `ReadOnlySpan<T>`, `NativeMemory`, `stackalloc`, and `System.IO.Pipelines`.
- **Image Visualization:** 2D FITS image data must be transformable into a browser-displayable representation (PNG) without mutating the original FITS data.

---

## 3. General Requirements (Coding Standards)

These conventions apply uniformly across all four projects, regardless of feature area, and are
enforced for both human contributors and AI coding agents working in this repository:

- **File-scoped namespaces.** Every `.cs` file declares its namespace with `namespace X.Y;`, never the block form (`namespace X.Y { ... }`).
- **One type per file.** Every class, record, struct, and enum — public or private, however small — is declared in its own file. A file may still contain multiple non-type declarations that belong to the same static class (e.g. an extension class with several `extension` blocks).
- **Extension members over extension methods.** Static helper methods that logically extend a type must use the C# 14 `extension(...)` member syntax (grouped inside an `extension` block within the static class) rather than the classic `this`-parameter extension method form.
- **No primary constructors on classes or structs.** Classes and structs must declare an explicit constructor with a body (assigning to `private readonly` fields) rather than using C# 12 primary constructor syntax. This does **not** apply to positional records (`record`/`record struct` with a parameter list), which remain the standard pattern for DTOs and value types.
- **No line comments explaining code.** Do not use `//` comments to explain what code does. `///` XML documentation comments on public types/members are unaffected and remain expected wherever they aid API documentation.
- **No magic numbers.** Numeric literals used in a method body that encode domain meaning (scaling factors, thresholds, buffer sizes, default fallback values, algorithm coefficients, etc.) must be extracted into a named `private const` field on the containing class rather than appearing inline. This does not apply to structurally self-evident literals (e.g. `0`/`1`/`2` array indices, loop bounds derived from a collection's own length).
- **No `<LangVersion>` in `.csproj` files.** Do not pin or override the C# language version in any project file; the SDK's default (tied to the target framework) is always used.
- **CRLF line endings.** Every file in the repository uses CRLF line endings, enforced repo-wide by a `.gitattributes` rule (`* text eol=crlf`) rather than relying on each contributor's local `core.autocrlf` setting.

---

## 4. Architecture

### 4.1 Solution Structure

Feature slices drive the shape of the solution: `AstroLab.Api/Features` is organized around the
four conceptual layers a FITS dataset passes through — **understand the file** (Fits), **make the
data scientifically usable and learn from it** per data type (Images for 2D image data,
Spectroscopy for 1D spectra, with TimeSeries and Catalogues as planned future data-type slices —
see the roadmap note below), each slice pairing calibration/cleaning concerns with the analysis
they enable. Visualization (PNG rendering) is treated as its own concern within each data-type
slice, not folded into the scientific algorithms that back it — see §5.7.

```text
AstroLab.slnx
│
├── src/
│   ├── AstroLab.Core/                              # Pure Functional Core (Zero Dependencies)
│   │   ├── Fits/                                   # Domain models for HDUs and Headers
│   │   │   ├── HduDescriptor.cs                    # Per-HDU metadata (type, header, image shape, data size)
│   │   │   ├── FitsDatasetKind.cs                  # Image / Spectrum / TimeSeries / Table / Unknown
│   │   │   └── FitsDatasetClassifier.cs            # Classify(...) + EnsureKind(...) — see §4.4
│   │   ├── Imaging/                                # Pure pixel scaling, stretching & visualization math
│   │   │   ├── ImageScaler.cs
│   │   │   ├── ImageStatistics.cs
│   │   │   └── ColorMapper.cs
│   │   ├── Photometry/                             # Pure aperture photometry algorithms
│   │   ├── Spectroscopy/                           # Pure wavelength & spectral algorithms
│   │   └── Result/                                 # Result<T> / Error discriminated union
│   │
│   ├── AstroLab.Infrastructure/                    # Imperative Shell (Side Effects & Native Interop)
│   │   ├── Fits/                                   # Low-level cfitsio P/Invoke & Native Buffers
│   │   ├── Storage/                                # Local disk staging via System.IO.Pipelines
│   │   ├── Archives/                               # ESO and MAST archive HTTP clients + shared archive models
│   │   └── ImageRendering/                         # FITS → browser image rendering
│   │       ├── FitsImageRenderer.cs
│   │       ├── PngRenderer.cs
│   │       └── RenderOptions.cs
│   │
│   ├── AstroLab.Api/                               # API Host & Vertical Slice Endpoints
│   │   ├── Features/                               # Vertical Slices (REPR Pattern)
│   │   │   ├── Fits/                               # "What is this file?"
│   │   │   │   ├── Upload/                         #   Stage a raw FITS file to local storage
│   │   │   │   └── Inspect/                        #   Parse every HDU, classify data type, return metadata
│   │   │   ├── Images/                             # "What can I learn from this image?"
│   │   │   │   ├── Render/                         #   FITS → PNG visualization
│   │   │   │   ├── Statistics/                     #   Pixel statistics
│   │   │   │   ├── Photometry/                     #   Aperture flux measurement
│   │   │   │   ├── Sources/                        #   Source detection — roadmap, HTTP 501 (§4.1 note)
│   │   │   │   └── Astrometry/                     #   Pixel↔world WCS — roadmap, HTTP 501 (§4.1 note)
│   │   │   ├── Spectroscopy/                       # "What can I learn from this spectrum?"
│   │   │   │   ├── Extract/                        #   Boxcar extraction + wavelength calibration
│   │   │   │   ├── Calibrate/                      #   Wavelength-dispersion fitting — roadmap, HTTP 501
│   │   │   │   ├── Lines/                          #   Spectral line detection — roadmap, HTTP 501
│   │   │   │   └── Redshift/                       #   Redshift estimation — roadmap, HTTP 501
│   │   │   ├── TimeSeries/                         # "What can I learn from this time series?" — roadmap feature
│   │   │   │   ├── LightCurve/                     #   Flux-vs-time extraction — HTTP 501
│   │   │   │   ├── Detrend/                        #   Trend removal — HTTP 501
│   │   │   │   └── PeriodSearch/                   #   Periodicity search — HTTP 501
│   │   │   ├── Catalogues/                         # External catalogue integration — roadmap feature
│   │   │   │   ├── Query/                          #   Cone-search query — HTTP 501
│   │   │   │   └── CrossMatch/                     #   Source cross-match — HTTP 501
│   │   │   └── Archives/                           # Archive metadata search/download
│   │   │       ├── Search/
│   │   │       └── Download/
│   │   └── Program.cs                              # Web host & service registrations
│   │
│   └── AstroLab.Tests/                             # Comprehensive Test Suite
│       ├── Core/                                   # Pure domain algorithm unit tests
│       ├── Infrastructure/                         # CFITSIO native memory & rendering tests
│       └── Features/                               # Endpoint integration tests
│
└── storage/                                        # Local disk directory for raw FITS files (gitignored)
```

> **Roadmap (not yet implemented):** `Images/Sources`, `Images/Astrometry`, `Spectroscopy/Calibrate`,
> `Spectroscopy/Lines`, `Spectroscopy/Redshift`, and the `TimeSeries` and `Catalogues` features are
> scaffolded at the API boundary — real request/response DTOs, routed and visible in OpenAPI — but
> every one of them returns HTTP 501 via the shared `NotImplementedResult` helper (§5.5), because the
> Core algorithm behind each does not exist yet:
> - `Images/Sources` (source detection) and `Images/Astrometry` (WCS), backed by a future
>   `AstroLab.Core.Astrometry` namespace (`Wcs`) and image-domain source-detection primitives.
> - `Spectroscopy/Calibrate`, `Spectroscopy/Lines`, and `Spectroscopy/Redshift`, backed by a future
>   `SpectralLine`/`Redshift` primitive family in `AstroLab.Core.Spectroscopy`.
> - `TimeSeries` (`LightCurve`, `Detrend`, `PeriodSearch`), backed by a future
>   `AstroLab.Core.TimeSeries` namespace — and, before that, by table-HDU pixel-data reading in
>   `AstroLab.Infrastructure.Storage` (today's `FitsDatasetReader` only loads image-bearing HDUs).
> - `Catalogues` (`Query`, `CrossMatch`), backed by a future `AstroLab.Core.Catalogues` namespace and
>   an external catalogue HTTP client in `AstroLab.Infrastructure`.
>
> When one of these Core algorithms lands, replace its endpoint's `NotImplementedResult.Value(...)`
> body with the real Request → Infrastructure → Core → `Result<T>` → Response flow (§4.3) — the DTOs
> and routing already in place should not need to change shape for straightforward cases.

### 4.2 Dependency Rules

Dependencies flow one way only:

```text
AstroLab.Api
   │
   ├──► AstroLab.Infrastructure
   │        │
   │        └──► AstroLab.Core
   │
   └──► AstroLab.Core

AstroLab.Tests
   ├──► AstroLab.Api
   ├──► AstroLab.Infrastructure
   └──► AstroLab.Core
```

`AstroLab.Core` must never reference `AstroLab.Infrastructure` or `AstroLab.Api`. The following
rules are mandatory and are the fastest checklist for validating a change against the architecture:

1. `AstroLab.Core` must never reference `AstroLab.Infrastructure` or ASP.NET Core.
2. `AstroLab.Core` must never perform I/O or native interop, and must not depend on mutable global state.
3. `AstroLab.Core` must contain only pure, deterministic decision logic.
4. `AstroLab.Infrastructure` owns all native memory, filesystem access, and HTTP communication.
5. `AstroLab.Api` feature slices orchestrate `AstroLab.Infrastructure` and `AstroLab.Core`; they never implement scientific/domain calculations themselves.
6. Expected failures are represented with `Result<T>`; exceptions are never used for normal domain control flow (§5.1).
7. Large FITS pixel buffers stay outside the managed GC heap wherever possible (§5.3).
8. Large network/file payloads are streamed, never fully buffered into a single `byte[]` (§5.4).
9. Core hot paths operate directly over spans, without intermediate allocations (§5.2).

### 4.3 Request Flow

Every API endpoint follows the same four-step flow:

1. **Receive request** — route parameters, query parameters, request bodies, or uploaded files, via ASP.NET Core Minimal APIs.
2. **Resolve infrastructure resources** — file paths, network streams, local FITS files, `UnmanagedFitsBuffer` instances, ESO/MAST archive clients.
3. **Invoke functional core** — pass the resolved data into pure algorithms from `AstroLab.Core`; keep all mathematical/scientific calculations inside Core.
4. **Map the result** — pattern-match against `Result<T>`, converting successes and known domain errors into the appropriate HTTP response, without exception-based control flow.

```text
HTTP Request
     │
     ▼
AstroLab.Api          (feature endpoint)
     │
     ▼
AstroLab.Infrastructure (file I/O, HTTP, CFITSIO)
     │
     ▼
UnmanagedFitsBuffer / ReadOnlySpan<T>
     │
     ▼
AstroLab.Core          (pure algorithm)
     │
     ▼
Result<T>
     │
     ▼
HTTP Response
```

### 4.4 FITS Dataset Classification

**Location:** `AstroLab.Core/Fits/FitsDatasetKind.cs`, `FitsDatasetClassifier.cs`

Before any type-specific analysis runs, the system identifies *what kind of scientific data* a
staged FITS file actually contains, and refuses to run an analysis against the wrong kind:

```text
FITS File
     │
     ▼
Fits Reader                 (AstroLab.Infrastructure.Storage.FitsHeaderReader/FitsDatasetReader)
     │
     ▼
Inspect HDUs                (FitsHeaderReader.ReadAllHeadersAsync walks every HDU, skipping data
     │                        segments via HduDescriptor.DataSizeBytes)
     ▼
Read Metadata                (HduDescriptor.FromHeader per HDU — type, header, image shape)
     │
     ▼
Identify Data Type           (FitsDatasetClassifier.Classify — pure, Core)
     │
     ├─────────────┬─────────────┬─────────────┐
     ▼             ▼             ▼             ▼
   Image        Spectrum      TimeSeries      Table / Unknown
     │             │             │
     ▼             ▼             ▼
FitsDatasetClassifier.EnsureKind(hdus, required)   ◄── the validation gate every analysis
     │                                                  entry point calls first
     ├─ match ──────────────────────► proceed: FitsDatasetReader.LoadImageAsync /
     │                                          LoadSpectrumImageAsync → Core algorithm → Result<T>
     └─ mismatch ───────────────────► Error.Validation("fits.data.unsupported_type", ...)
                                       → HTTP 400 via ToApiResult/ToProblem (§5.1, §5.5)
```

`FitsDatasetClassifier.Classify` inspects the full ordered list of a file's `HduDescriptor`s and
returns a `FitsDatasetKind`:

1. Any table HDU (`AsciiTable`/`BinaryTable`) with a `TTYPEn` column named `TIME` → `TimeSeries`.
   This check alone is file-wide, since a lightcurve table doesn't compete with an image HDU for
   which data gets analyzed.
2. Otherwise, find the first HDU with non-empty pixel data (`FitsDatasetClassifier.HasPixelData` —
   the exact same predicate `FitsDatasetReader` uses to pick which HDU to load pixels from, so
   classification and loading can never disagree on *which* HDU is being described). That HDU alone
   is then checked: a single-axis (`NAXIS = 1`) array, or a `DISPAXIS` keyword, or a `CTYPEn` value
   starting with `WAVE`, `FREQ`, `ENER`, `AWAV`, or `VELO` — all evaluated only against *this* HDU's
   own header, never any other HDU's — classifies it `Spectrum` (this is what lets a raw 2D
   long-slit spectrogram — a spatial axis plus a dispersion axis, still stored as ordinary FITS
   image data — classify as `Spectrum` rather than `Image`); otherwise it classifies `Image`. An
   unrelated extension's stray `DISPAXIS`/`CTYPE` card must never reclassify a *different* HDU's
   plain image data — that was a real bug (classification scanned every HDU for markers while
   loading only ever looked at the first data-bearing HDU) fixed by scoping the marker check to the
   one HDU both operations agree on.
3. If no HDU has pixel data, any table HDU present → `Table`.
4. Otherwise → `Unknown`.

`FitsDatasetClassifier.EnsureKind(hdus, required)` classifies and returns `Result<FitsDatasetKind>`
— `Success` when the classification matches `required`, otherwise a validation `Error` naming both
the required and actual kind. `AstroLab.Infrastructure.Storage.FitsDatasetReader` calls this before
reading any pixel data (`LoadImageAsync` requires `Image`; `LoadSpectrumImageAsync` requires
`Spectrum`), so every Images-family and Spectroscopy-family endpoint is gated by it today. `Table`
and `TimeSeries` are classified but not yet consumed by any endpoint — `Catalogues` and `TimeSeries`
are still HTTP 501 roadmap stubs (§4.1) — but the same `EnsureKind` call is what their real Core
work will use once it lands.

Walking every HDU (`FitsHeaderReader.ReadAllHeadersAsync`) fails fast with a validation `Error`
(`fits.header.empty_file`) when a staged file contains zero HDUs — a 0-byte upload, most obviously
— rather than letting an empty result ripple downstream into an out-of-bounds access. A table HDU's
`DataSizeBytes` (`HduDescriptor`) is `(NAXIS1 * NAXIS2) + PCOUNT` (row bytes plus the
variable-length-array heap size), with every component clamped to non-negative before combining, so
a malformed header can't compute a negative or nonsensical skip distance to the next HDU.

---

## 5. Core Implementation Patterns

### 5.1 Result Pattern

**Location:** `AstroLab.Core/Result/Result.cs`, `Error.cs`

C# has no native discriminated-union type, so `Result<TValue>` provides one by hand: it represents
either a successful result containing a `TValue`, or a failure result containing an `Error`. The
current implementation exposes `Success`, `Failure`, `Match`, `Bind`, `Map`, `MapError`, `Ensure`,
and `Deconstruct` for composing and pattern-matching against outcomes, so API endpoints can
translate domain outcomes directly into HTTP responses via C# pattern matching.

`Error` is a lightweight value type carrying a stable machine-readable code, a human-readable
message, and an `ErrorCategory` used to map the failure onto a transport response. `ErrorCategory`
includes `NotImplemented` — for a real, named capability whose Core/Infrastructure implementation
doesn't exist yet (e.g. `EsoArchiveClient`/`MastArchiveClient`, §5.6) — which `ResultEndpointExtensions`
maps to HTTP 501, the same status the API-boundary-only `NotImplementedResult` helper (§5.5) returns
for roadmap endpoints that never produce a `Result<T>` in the first place.

Exceptions must not be used for normal domain validation, calculation failures, invalid FITS data,
or other expected failure conditions. Exceptions may still be used at the imperative shell boundary
for genuinely exceptional conditions — unrecoverable infrastructure failures, native interop
failures, or process-level failures.

### 5.2 Functional Core: Purity and Spans

**Location:** `AstroLab.Core`

`AstroLab.Core` is the functional core of the application and must remain completely isolated from
infrastructure and external side effects.

Every method inside `AstroLab.Core` must be implemented as a standard static pure function wherever
practical. Pure functions must:

- Depend only on their input parameters.
- Produce deterministic outputs for identical inputs.
- Avoid modifying external state or hidden global state.
- Avoid I/O and infrastructure dependencies.

`AstroLab.Core` must have:

- **Zero disk access.**
- **Zero network access.**
- **Zero native interop calls.**
- **Zero filesystem dependencies.**
- **Zero references to `AstroLab.Infrastructure`.**
- **Zero dependencies on ASP.NET Core.**
- **Zero dependencies on archive clients or storage implementations.**

The Core project contains only domain models, value types, mathematical algorithms, validation
logic, and result/error representations.

Processing functions operating on large pixel or byte buffers should accept `ReadOnlySpan<float>`
or `ReadOnlySpan<byte>` where appropriate, operating directly over contiguous memory spans rather
than requiring callers to create intermediate managed arrays. Hot-path algorithms should avoid
allocations on the managed GC heap. Where appropriate, APIs may also use `Span<T>`,
`ReadOnlyMemory<T>`, `stackalloc`, and `ref struct` types, provided that doing so does not
compromise the purity or usability of the functional core.

### 5.3 Unmanaged Native Buffers

**Location:** `AstroLab.Infrastructure/Fits`

`AstroLab.Infrastructure` contains all side effects: native interop, filesystem access, network
communication, and resource management. `cfitsio` raw pixel allocations are wrapped using
`System.Runtime.InteropServices.NativeMemory` and `IDisposable`-based wrappers, because large FITS
image buffers — potentially several gigabytes — must exist outside the managed GC heap.

`UnmanagedFitsBuffer` owns the lifetime of native FITS pixel memory and must:

- Allocate native memory using `NativeMemory`.
- Expose the memory to Core algorithms through spans where safe and appropriate.
- Correctly release native allocations, with deterministic disposal through `IDisposable`.
- Prevent double-free operations, with ownership of native memory always explicit.
- Avoid copying large pixel buffers into managed arrays.

### 5.4 Pipeline Streaming

**Location:** `AstroLab.Infrastructure/Storage`, `AstroLab.Infrastructure/Archives`

Incoming archive data from ESO and MAST must be streamed directly to local storage without
unnecessarily buffering the entire FITS file in managed memory, using
`System.IO.Pipelines.PipeReader`/`PipeWriter`. The implementation must:

- Stream network responses incrementally and write directly to local staging storage.
- Avoid loading complete FITS files into a single `byte[]`.
- Minimize intermediate buffer allocations and respect backpressure.
- Correctly complete and dispose pipeline resources.
- Propagate cancellation tokens throughout the pipeline.

### 5.5 Vertical Slice API Endpoints (REPR Pattern)

**Location:** `AstroLab.Api/Features`

API functionality is organized into self-contained vertical slices using ASP.NET Core Minimal APIs,
with each endpoint following the **REPR pattern** (Request-Endpoint-Response): a single endpoint is
paired with exactly one request DTO and one response DTO, both defined at the API boundary, in the
same feature slice.

- Each feature slice owns its endpoint-specific request/response DTOs (one type per file, per the
  §3 one-type-per-file rule) and endpoint mapping (a `Map*Endpoints()` extension member on
  `IEndpointRouteBuilder`) — see §4.3 for the request flow each endpoint follows.
- **Features drive the folder structure, at leaf granularity.** A top-level feature area (`Fits`,
  `Images`, `Spectroscopy`, `Archives`, ...) is a route group, not an endpoint: it owns a top-level
  `{Feature}Endpoints.cs` that creates the `RouteGroupBuilder` and composes the leaves beneath it. A
  leaf subfolder (`Render`, `Statistics`, `Photometry`, `Inspect`, `Upload`, `Extract`, `Search`,
  `Download`, ...) is one self-contained endpoint: its own `{Leaf}Endpoint.cs` (a `Map{Leaf}Endpoint`
  extension member on `IEndpointRouteBuilder` mapping exactly one route) plus its own request/response
  DTOs, all sharing the leaf's namespace (`AstroLab.Api.Features.{Feature}.{Leaf}`). This keeps each
  slice small enough to reason about in isolation and lets a feature area grow (e.g. `Images` gaining
  a `Sources` or `Astrometry` leaf) without touching unrelated leaves.
- **A domain or infrastructure model — anything defined in `AstroLab.Core` or
  `AstroLab.Infrastructure` (FITS domain models, `ArchiveObservation`, aperture/spectrum
  measurement results, etc.) — must never be returned directly as, or embedded unmapped inside, an
  HTTP response.** Every response is its own DTO record declared under `Features/`, constructed by
  the endpoint (or a small `FromX(...)` mapping helper on the response type) from the `Result<T>`
  value returned by Core/Infrastructure. This holds even when a response would otherwise be a
  trivial one-to-one field copy — the boundary DTO still isolates callers from Core/Infrastructure
  representation changes and is what defines the endpoint's actual wire contract.
  Enums shared across the boundary (`StretchMode`, `ColorMap`, `DispersionAxis`, `ArchiveSource`)
  are the one exception, since they are plain string-serialized discriminators rather than models.
- Avoid creating a large centralized controller or service containing unrelated application
  functionality; endpoints stay thin and never contain the implementation of photometry, image
  scaling, spectral extraction, or other scientific algorithms.
- **Roadmap slices return HTTP 501, never a half-built implementation.** A feature slice scaffolded
  ahead of its Core algorithm (§4.1's roadmap note) still gets real request/response DTOs and a
  mapped route, but its handler body is exactly one call to
  `AstroLab.Api.Features.NotImplementedResult.Value(code, message)`, which returns
  `Results.Problem(..., statusCode: 501, title: code)`. This keeps the wire contract final and
  visible in OpenAPI while being explicit that the analysis behind it doesn't exist yet — never
  fake success, a hardcoded value, or partial logic. When the Core algorithm lands, the stub call is
  replaced by the real Request → Infrastructure → Core → `Result<T>` → Response flow (§4.3).

### 5.6 Archive Clients: ESO and MAST

**Location:** `AstroLab.Infrastructure/Archives`

The ESO and MAST clients are stub HTTP client abstractions: each archive's real query/download
surface is not yet fully wired up, but the HTTP plumbing is — a resilient, retrying `HttpClient`
resolved via `IHttpClientFactory` (registered with `AddHttpClient<TInterface, TImpl>` and
`AddStandardResilienceHandler()` for retries). Both clients must be designed so the concrete
request/response contracts can be filled in later without touching callers, `AstroLab.Core`, or the
API feature slices, and so that MAST's implementation can evolve independently while following the
same architectural principles as ESO's.

Neither client issues a request against a guessed URL on the real archive host: until the real
query/download contract for an archive is known, `SearchAsync`/`DownloadAsync` return
`Error.NotImplemented(...)` directly rather than calling `_httpClient` against a fabricated path —
a coincidental 2xx from an unrelated page on the real host must never be reported as "search
succeeded, zero results." The injected, resilience-wrapped `HttpClient` stays in place so the real
implementation is a matter of filling in the method body, not re-wiring DI.

### 5.7 Visualization as a Separate Capability

**Location:** `AstroLab.Infrastructure/ImageRendering`, `AstroLab.Api/Features/Images/Render`

Visualization is an infrastructure-and-API concern, not a scientific one, and must never be
implemented inside `AstroLab.Core`. Core produces scientific results — scaled/stretched pixel
values, statistics, source measurements — as plain data; it has no knowledge of PNG, JPEG, or any
other output encoding, and no dependency on an imaging codec library. A rendering library
(`PngRenderer`) belongs in `AstroLab.Infrastructure` precisely because it is a concrete third-party
dependency with its own I/O and encoding concerns, exactly like `cfitsio` or an archive `HttpClient`.

The render request flow illustrates why this separation matters — each step touches exactly one
layer, and Core never learns that its output will become a PNG:

```text
HTTP request (RenderEndpoint)
     │
     ▼
AstroLab.Infrastructure/Fits    (cfitsio-backed pixel read → UnmanagedFitsBuffer / spans)
     │
     ▼
AstroLab.Core/Imaging           (ImageScaler, ImageStatistics, ColorMapper — pure scaling/stretch math)
     │
     ▼
AstroLab.Infrastructure/ImageRendering  (FitsImageRenderer + PngRenderer — pixels → PNG bytes)
     │
     ▼
HTTP response (image/png)
```

This same pattern generalizes to every other visualization the roadmap in §4.1 anticipates
(spectrum plots, light curves, source overlays, RGB composites, false-colour images): Core supplies
the scientific values, and a renderer living in `AstroLab.Infrastructure` (or a thin mapping in the
API feature slice, for simple JSON-shaped visualizations like a spectrum plot's point series) turns
them into the requested visual/wire representation. A Core algorithm must never be written to know
or care whether its result becomes a PNG, a JSON response, a FITS file, or something else.

### 5.8 Global Exception Handling

**Location:** `AstroLab.Api/GlobalExceptionHandler.cs`, `Program.cs`

`Result<T>` (§5.1) covers every *expected* failure — validation, missing data, an unimplemented
capability — and every endpoint maps one to a response via `ResultEndpointExtensions`. It does not,
and must not, cover genuinely unexpected failures (a native interop crash, a programmer error): an
exception escaping an endpoint is caught by `GlobalExceptionHandler` (registered via
`AddExceptionHandler<T>()` + `AddProblemDetails()` in `Program.cs`, and wired into the pipeline with
`app.UseExceptionHandler()`, ahead of every route). It logs the full exception server-side and
returns a generic `ProblemDetails` (HTTP 500, title `unexpected_error`) — the caller never sees a
stack trace or exception message, and the site the failure actually happened at is only ever visible
in the server-side log. This is a safety net, not a substitute for `Result<T>`: a new failure mode
that can be anticipated still belongs in `Result<T>`, not left to fall through to this handler.

---

## 6. Testing Standards

**Location:** `AstroLab.Tests`

Tests cover the Core, Infrastructure, and API layers.

### 6.1 Core Unit Tests

**Location:** `AstroLab.Tests/Core/`

Tests verify:

- Photometry calculations are mathematically correct — circular aperture flux, annular background estimation.
- Image scaling produces expected normalized values, including correct logarithmic-scaling behavior.
- Spectrum extraction produces expected one-dimensional output.
- `Result<T>` success and failure cases behave correctly, and expected domain failures do not require exceptions.

### 6.2 Allocation Tests

Performance/allocation tests for hot-path Core algorithms verify that algorithms operating on
existing spans:

- Do not allocate managed arrays unnecessarily, or allocate on the managed heap where zero-allocation execution is required.
- Do not create hidden LINQ allocations, box value types, or create unnecessary intermediate collections.

Allocation is measured with `GC.GetAllocatedBytesForCurrentThread()` (or a dedicated
benchmarking/allocation framework where appropriate), and must distinguish one-time setup
allocations and test-harness allocations from the actual allocations performed by the algorithm
under test. The requirement is specifically that the **hot data-processing path** performs zero
managed heap allocations. See `AstroLab.Tests/Core/AllocationTests.cs` for the current enforcement
pattern.

---

## 7. Appendix: Original Build Sequence (Historical)

AstroLab was originally scaffolded by an AI coding agent (Claude Code) following the build order
below, in strict sequence, keeping the solution compiling and its tests green at every stage. The
solution described throughout this document already exists in the repository — this appendix is
retained as a reference for the build order to follow when extending the same architectural pattern
to a new capability (e.g. a new Core algorithm family or archive integration), not as an outstanding
task list.

| Phase | What was built                                                         | Governing spec section |
| ----- | ---------------------------------------------------------------------- | ---------------------- |
| 1     | Solution and project scaffolding (four projects, dependency direction) | §4.1, §4.2             |
| 2     | Core `Result`/`Error` types                                            | §5.1                   |
| 3     | Core FITS/domain models                                                | §4.1                   |
| 4     | Core photometry, imaging, and spectroscopy algorithms                  | §5.2                   |
| 5     | Core unit and allocation tests                                         | §6                     |
| 6     | CFITSIO native bindings and `UnmanagedFitsBuffer`                      | §5.3                   |
| 7     | `LocalFileStore` / pipeline streaming                                  | §5.4                   |
| 8     | ESO and MAST archive clients                                           | §5.6                   |
| 9     | API vertical slices                                                    | §5.5, §4.3             |
| 10    | API integration tests                                                  | §6                     |
| 11    | Full solution validation                                               | —                      |

```text
1. Solution & project scaffolding
        │
        ▼
2. Core Result/Error types
        │
        ▼
3. Core FITS/domain models
        │
        ▼
4. Core photometry / imaging / spectroscopy algorithms
        │
        ▼
5. Core unit & allocation tests
        │
        ▼
6. CFITSIO bindings & UnmanagedFitsBuffer
        │
        ▼
7. LocalFileStore / Pipelines
        │
        ▼
8. ESO & MAST archive clients
        │
        ▼
9. API vertical slices
        │
        ▼
10. API integration tests
        │
        ▼
11. Full solution validation
```

At each stage, the implementation compiled and its tests remained passing before proceeding to the
next stage — the same discipline applies to any future work that extends this pattern.
