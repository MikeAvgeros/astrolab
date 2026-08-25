# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Keep this file current:** whenever you make a change to this project (new project/feature slice, changed architecture, new commands, changed dependency rules), update the relevant section of this CLAUDE.md in the same change.

## Project Overview

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and visualising FITS (Flexible Image Transport System) astronomical datasets from ESO/MAST archives and direct uploads. See `spec.md` for the full specification — architecture, coding standards, and implementation patterns — and the original build sequence this project was scaffolded from.

There is no database — metadata and raw datasets are staged on local disk under `storage/` (gitignored, path configurable via `Storage:RootPath`).

## General Requirements

These apply across all four projects (see `spec.md` §3–§4 for the source of truth):

- File-scoped namespaces everywhere (`namespace X.Y;`).
- Extension members (C# 14 `extension(...)` blocks) instead of classic `this`-parameter extension methods.
- No primary constructors on classes/structs — use an explicit constructor body assigning `private readonly` fields. Positional records are exempt and remain the norm for DTOs/value types.
- Construct validated domain records via `<Name>Factory.Create(...)` (except HTTP request DTOs deserialized by model binding).
- Use `ImmutableList<T>` for collection properties on API-boundary DTO records (Core hot-path types are exempt and use span/array-based representations).
- No `//` line comments explaining code. `///` XML doc comments are unaffected and still expected on public API surface.
- One type per file, no matter how small (including private/internal nested types) — a file may still hold multiple `extension` blocks for the same static class.
- Never add `<LangVersion>` to a `.csproj`.
- CRLF line endings on every file, enforced repo-wide via `.gitattributes` (`* text eol=crlf`).
- No magic numbers: numeric literals encoding domain meaning (scaling factors, thresholds, buffer sizes, default fallbacks, algorithm coefficients) must be extracted into a named `private const` field on the containing class rather than appearing inline in a method body. Structurally self-evident literals (array indices, loop bounds from a collection's own length) are exempt.
- Propagate `CancellationToken` across all async operations that can be cancelled, and pass it to downstream I/O. Never block asynchronous code with `.Result` or `.Wait()`.
- Ensure FITS header keywords and values survive read → process → write round trips unless explicitly modified; scientific provenance is data, not incidental metadata.

## Commands

```bash
dotnet build AstroLab.slnx                     # build the whole solution
dotnet test src/AstroLab.Tests                 # run all tests
dotnet test src/AstroLab.Tests --filter "FullyQualifiedName~ApertureEngineTests"   # run one test class
dotnet test src/AstroLab.Tests --filter "DisplayName~<test name>"                  # run one test
dotnet run --project src/AstroLab.Api          # run the API host
docker build -t astrolab-api .                 # build the container image (see Dockerfile)
docker run -p 8080:8080 -v astrolab-storage:/app/storage astrolab-api   # run it, with staged FITS files on a named volume
```

Tests use xUnit (`AstroLab.Tests`). `Microsoft.AspNetCore.Mvc.Testing` (`ApiFactory.cs`) is used for in-process endpoint integration tests against `Program`.

## Architecture

The solution is a strict 4-project **Functional Core, Imperative Shell (FCIS)** design, combined with **Vertical Slice Architecture** in the API layer. Feature slices drive the shape of the API: the top-level feature areas track the four conceptual layers a FITS dataset passes through — understand the file (`Fits`), then make each data type's data scientifically usable and analyse it (`Images` for 2D image data, `Spectroscopy` for 1D spectra, `TimeSeries` for tabular time-series data — scaffolded but HTTP 501 today) — plus `Archives` for upstream archive search/download and `Catalogues` for external catalogue integration (also scaffolded, HTTP 501). Before any type-specific analysis runs, `AstroLab.Core.Fits.FitsDatasetClassifier` identifies the file's `FitsDatasetKind` (Image/Spectrum/TimeSeries/Table/Unknown) from its HDU metadata and gates the analysis on it — see spec.md §5.4. Visualisation (PNG rendering) is deliberately kept out of `AstroLab.Core` and lives in `AstroLab.Infrastructure`/the API boundary instead — see spec.md §6.7. Dependency direction is one-way and enforced by convention (not by analyzer):

```
AstroLab.Api  ──►  AstroLab.Infrastructure  ──►  AstroLab.Core
     └──────────────────────────────────────────────┘
AstroLab.Tests ──► all three
```

`AstroLab.Core` must **never** reference `AstroLab.Infrastructure` or ASP.NET Core, and must have zero I/O, zero native interop, and no hidden global/mutable state. When adding a feature, put the math/domain logic in Core first, then wire it up from Infrastructure/Api — not the other way around.

### AstroLab.Core — pure functional core

Static, pure, deterministic functions only; operates over `ReadOnlySpan<float>` / `ReadOnlySpan<byte>` on hot paths to avoid managed allocations. No I/O, no exceptions for expected failures. Contains only scientific primitives — never rendering/encoding logic (PNG, JSON shaping, etc.), which belongs in Infrastructure/Api instead (spec.md §6.7).

- `Fits/` — FITS domain models (headers, HDUs, keyword/value parsing); `FitsDatasetKind` and `FitsDatasetClassifier` classify a file's scientific data type (Image/Spectrum/TimeSeries/Table/Unknown) from its HDU metadata and gate analysis endpoints on the required kind — see spec.md §5.4
- `Imaging/` — pixel scaling/stretch (`ImageScaler`), statistics (`ImageStatistics`), colour mapping (`ColorMapper`)
- `Photometry/` — `ApertureEngine`: circular/annular aperture flux and background estimation
- `Spectroscopy/` — `SpectrumExtractor`: 1D spectral extraction
- `Result/` — `Result<TValue>` / `Error`: hand-rolled discriminated union (C# lacks native DUs) used everywhere instead of throwing for expected failures (validation, missing data, calculation failures, or a named capability that isn't implemented yet — `ErrorCategory.NotImplemented`, mapped to HTTP 501). Has `Match`, `Bind`, `Map`, `MapError`, `Ensure`, `Deconstruct` for pattern matching. Reserve real exceptions for genuinely unrecoverable infrastructure/interop failures at the shell boundary — and even those are ultimately caught by `GlobalExceptionHandler` (see below) rather than surfacing a stack trace to the caller.
- **Roadmap (not yet implemented):** an `Astrometry/` namespace (`Wcs`) and `TimeSeries/` and `Catalogues/` namespaces are still unbuilt. Their API-boundary feature slices already exist (scaffolded, returning HTTP 501 — see below), but do not scaffold the Core namespaces themselves ahead of the real algorithm work landing.

### AstroLab.Infrastructure — imperative shell

Owns all side effects: native interop, filesystem, HTTP, memory management.

- `Fits/` — P/Invoke bindings to the native `cfitsio` library (`NativeMethods`); `UnmanagedFitsBuffer` owns native pixel memory lifetime via `NativeMemory`/`IDisposable` (double-free-safe, explicit ownership)
  - The native `cfitsio` binary (and any DLLs it's dynamically linked against) is **not committed to git** — drop it locally into `native/<rid>/` (e.g. `native/win-x64/cfitsio.dll`), gitignored. The root `Directory.Build.props` copies everything under `native/win-x64/*.dll` into the output directory of `AstroLab.Api` and `AstroLab.Tests` only (native library resolution happens relative to the running process's base directory, not `AstroLab.Infrastructure`'s output folder). Add a corresponding `native/<rid>/` item group there when Linux/macOS builds are introduced.
- `Storage/` — `LocalFileStore` (`ILocalFileStore`) streams FITS data to/from disk via `System.IO.Pipelines` (`PipeReader`/`PipeWriter`), never buffering full files in managed memory; `FitsDatasetReader`/`FitsHeaderReader`/`FitsPixelDataReader`/`FitsPixelConverter` read staged files. `LocalFileStoreOptions.MaxUploadSizeBytes` (default 10 GiB, `Storage:MaxUploadSizeBytes` in config, `null` = unlimited) is applied by `UploadEndpoint` via `IHttpMaxRequestBodySizeFeature` — Kestrel's own default (~28.6 MB) would otherwise reject any real FITS upload regardless of the streaming code path.
- `Archives/` — archive metadata models (`ArchiveObservation`, `ArchiveDownload`, `ArchiveSearchQuery`, `ArchiveSource`), the shared `IArchiveClient` contract, and the ESO/MAST HTTP clients (`IEsoArchiveClient`/`EsoArchiveClient`, `IMastArchiveClient`/`MastArchiveClient`), registered via `AddHttpClient<TInterface, TImpl>` with `AddStandardResilienceHandler()` for retries. Both clients return `Error.NotImplemented(...)` directly rather than calling a guessed URL on the real archive host — see spec.md §6.6.
- `ImageRendering/` — `FitsImageRenderer` (fully static — no instance state) + `PngRenderer` turn FITS pixel data into browser-displayable PNGs via `RenderOptions`
- `InfrastructureServiceCollectionExtensions.AddAstroLabInfrastructure(...)` — single DI registration entry point called from `Program.cs`; options bound from config sections (`Storage`, `Archives:Eso`, `Archives:Mast` — see each `*Options.SectionName`)

### AstroLab.Api — host & vertical slices

Minimal APIs, organised as self-contained feature slices under `Features/` following the **REPR pattern** (Request-Endpoint-Response). Features drive the folder structure at leaf granularity (spec.md §6.5): a top-level feature area owns a `{Feature}Endpoints.cs` that creates the route group and composes leaves; each leaf subfolder is one self-contained endpoint with its own `{Leaf}Endpoint.cs`, request/response DTOs (one per file, defined at the API boundary), and namespace (`AstroLab.Api.Features.{Feature}.{Leaf}`), registered from the parent `{Feature}Endpoints.cs` and ultimately `Program.cs`. A `Core` or `Infrastructure` domain model (FITS headers, `ArchiveObservation`, aperture/spectrum measurement results, etc.) is never returned directly as, or embedded unmapped inside, an HTTP response — every response is its own DTO record, built from the `Result<T>` value via a `FromX(...)` mapping helper or inline construction in the endpoint (see spec.md §6.5). The only exception is enums shared across the boundary (`StretchMode`, `ColorMap`, `DispersionAxis`, `ArchiveSource`), which are plain string-serialised discriminators, not models.

- `Features/Fits/` — "what is this file?": `Upload/` (stage a raw FITS file), `Inspect/` (walk every HDU, classify the file's `FitsDatasetKind`, and return that plus per-HDU/header metadata)
- `Features/Images/` — 2D image data type: `Render/` (FITS → PNG), `Statistics/` (pixel statistics), `Photometry/` (aperture flux measurement) all require the file to classify as `Image` (`FitsDatasetReader.LoadImageAsync`); `Sources/` (source detection) and `Astrometry/` (WCS) are scaffolded (DTOs + routes) but return HTTP 501 pending their Core primitives
- `Features/Spectroscopy/` — 1D spectrum data type: `Extract/` (boxcar extraction + wavelength calibration) requires the file to classify as `Spectrum` (`FitsDatasetReader.LoadSpectrumImageAsync`); `Calibrate/`, `Lines/`, and `Redshift/` are scaffolded but return HTTP 501 pending their Core primitives
- `Features/TimeSeries/` — scaffolded roadmap feature (`LightCurve/`, `Detrend/`, `PeriodSearch/`, `Transit/`); every route returns HTTP 501 pending both an `AstroLab.Core.TimeSeries` namespace and table-HDU pixel reading in `FitsDatasetReader`
- `Features/Catalogues/` — scaffolded roadmap feature (`Query/`, `CrossMatch/`); every route returns HTTP 501 pending an `AstroLab.Core.Catalogues` namespace and an external catalogue HTTP client
- `Features/Archives/` — archive metadata search/download: `Search/`, `Download/`, plus a shared `ArchiveClientResolver`
- `Features/ResultEndpointExtensions.cs` — `Result<T>.ToApiResult(...)` maps a Core/Infrastructure `Result` into an `IResult` HTTP response via pattern matching; use this instead of hand-rolling status code mapping in a new endpoint
- `Features/NotImplementedResult.cs` — the standard HTTP 501 body every roadmap slice's handler returns (see spec.md §6.5) — replace the call with the real Request → Infrastructure → Core → `Result<T>` → Response flow once that slice's Core algorithm lands, rather than scaffolding further ahead of it.

Endpoint flow: receive request → resolve Infrastructure resources (file store, archive client, native buffer) → call into `AstroLab.Core` for any calculation → map the `Result<T>` to an HTTP response. Endpoints stay thin; no scientific/domain logic belongs in `Features/`.

Enums (e.g. `StretchMode`, `ColorMap`, `DispersionAxis`, `ArchiveSource`) are serialised as strings via a global `JsonStringEnumConverter` registered in `Program.cs`.

`GlobalExceptionHandler` (root of `AstroLab.Api`, registered via `AddExceptionHandler<T>()`/`AddProblemDetails()` and `app.UseExceptionHandler()` ahead of every route in `Program.cs`) is the last-resort catch for exceptions that escape `Result<T>` handling — see spec.md §6.8. It always returns a generic `ProblemDetails` (500, `unexpected_error`); never add a scenario-specific `catch` here — a failure mode you can name belongs in `Result<T>` instead.

## Deployment

`Dockerfile` (repo root) is a multi-stage build producing a Linux container image (`mcr.microsoft.com/dotnet/aspnet:10.0`, non-root user, listens on `:8080`, `/app/storage` as a volume). It does not include the native `cfitsio` binary — harmless today since `AstroLab.Infrastructure.Fits.NativeMethods` isn't called from anywhere in the current FITS read/write path (see spec.md §6.3) — but wiring up real cfitsio P/Invoke calls will require adding a `native/linux-x64/*.so` item group to `Directory.Build.props` and copying it into the image's runtime stage.

## Performance constraints

These are load-bearing, not stylistic — check `AllocationTests.cs` for the enforcement pattern (`GC.GetAllocatedBytesForCurrentThread()`):

- Core hot paths (photometry, imaging, spectroscopy) must not allocate on the managed GC heap, box, use LINQ, or create intermediate collections/arrays.
- Multi-gigabyte FITS pixel buffers live in unmanaged memory (`NativeMemory`, `UnmanagedFitsBuffer`), exposed to Core via spans — never copied wholesale into managed arrays.
- Network/file I/O is streamed end-to-end via `System.IO.Pipelines`, respecting backpressure and cancellation tokens — never fully buffered into a `byte[]`.
