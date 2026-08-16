# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Keep this file current:** whenever you make a change to this project (new project/feature slice, changed architecture, new commands, changed dependency rules), update the relevant section of this CLAUDE.md in the same change.

## Project Overview

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analyzing, and visualizing FITS (Flexible Image Transport System) astronomical datasets from ESO/MAST archives and direct uploads. See `spec.md` for the full specification — architecture, coding standards, and implementation patterns — and the original build sequence this project was scaffolded from.

There is no database — metadata and raw datasets are staged on local disk under `storage/` (gitignored, path configurable via `Storage:RootPath`).

## General Requirements

These apply across all four projects (see `spec.md` § General Requirements for the source of truth):

- File-scoped namespaces everywhere (`namespace X.Y;`).
- Extension members (C# 14 `extension(...)` blocks) instead of classic `this`-parameter extension methods.
- No primary constructors on classes/structs — use an explicit constructor body assigning `private readonly` fields. Positional records are exempt and remain the norm for DTOs/value types.
- No `//` line comments explaining code. `///` XML doc comments are unaffected and still expected on public API surface.
- One type per file, no matter how small (including private/internal nested types) — a file may still hold multiple `extension` blocks for the same static class.
- Never add `<LangVersion>` to a `.csproj`.
- CRLF line endings on every file, enforced repo-wide via `.gitattributes` (`* text eol=crlf`).
- No magic numbers: numeric literals encoding domain meaning (scaling factors, thresholds, buffer sizes, default fallbacks, algorithm coefficients) must be extracted into a named `private const` field on the containing class rather than appearing inline in a method body. Structurally self-evident literals (array indices, loop bounds from a collection's own length) are exempt.

## Commands

```bash
dotnet build AstroLab.slnx                     # build the whole solution
dotnet test src/AstroLab.Tests                 # run all tests
dotnet test src/AstroLab.Tests --filter "FullyQualifiedName~ApertureEngineTests"   # run one test class
dotnet test src/AstroLab.Tests --filter "DisplayName~<test name>"                  # run one test
dotnet run --project src/AstroLab.Api          # run the API host
```

Tests use xUnit (`AstroLab.Tests`). `Microsoft.AspNetCore.Mvc.Testing` (`ApiFactory.cs`) is used for in-process endpoint integration tests against `Program`.

## Architecture

The solution is a strict 4-project **Functional Core, Imperative Shell (FCIS)** design, combined with **Vertical Slice Architecture** in the API layer. Dependency direction is one-way and enforced by convention (not by analyzer):

```
AstroLab.Api  ──►  AstroLab.Infrastructure  ──►  AstroLab.Core
     └──────────────────────────────────────────────┘
AstroLab.Tests ──► all three
```

`AstroLab.Core` must **never** reference `AstroLab.Infrastructure` or ASP.NET Core, and must have zero I/O, zero native interop, and no hidden global/mutable state. When adding a feature, put the math/domain logic in Core first, then wire it up from Infrastructure/Api — not the other way around.

### AstroLab.Core — pure functional core
Static, pure, deterministic functions only; operates over `ReadOnlySpan<float>` / `ReadOnlySpan<byte>` on hot paths to avoid managed allocations. No I/O, no exceptions for expected failures.
- `Fits/` — FITS domain models (headers, HDUs, keyword/value parsing)
- `Imaging/` — pixel scaling/stretch (`ImageScaler`), statistics (`ImageStatistics`), colour mapping (`ColorMapper`)
- `Photometry/` — `ApertureEngine`: circular/annular aperture flux and background estimation
- `Spectroscopy/` — `SpectrumExtractor`: 1D spectral extraction
- `Result/` — `Result<TValue>` / `Error`: hand-rolled discriminated union (C# lacks native DUs) used everywhere instead of throwing for expected failures (validation, missing data, calculation failures). Has `Match`, `Bind`, `Map`, `MapError`, `Ensure`, `Deconstruct` for pattern matching. Reserve real exceptions for genuinely unrecoverable infrastructure/interop failures at the shell boundary.

### AstroLab.Infrastructure — imperative shell
Owns all side effects: native interop, filesystem, HTTP, memory management.
- `CFITSIO/` — P/Invoke bindings to the native `cfitsio` library (`NativeMethods`); `UnmanagedFitsBuffer` owns native pixel memory lifetime via `NativeMemory`/`IDisposable` (double-free-safe, explicit ownership)
  - The native `cfitsio` binary (and any DLLs it's dynamically linked against) is **not committed to git** — drop it locally into `native/<rid>/` (e.g. `native/win-x64/cfitsio.dll`), gitignored. The root `Directory.Build.props` copies everything under `native/win-x64/*.dll` into the output directory of `AstroLab.Api` and `AstroLab.Tests` only (native library resolution happens relative to the running process's base directory, not `AstroLab.Infrastructure`'s output folder). Add a corresponding `native/<rid>/` item group there when Linux/macOS builds are introduced.
- `Storage/` — `LocalFileStore` (`ILocalFileStore`) streams FITS data to/from disk via `System.IO.Pipelines` (`PipeReader`/`PipeWriter`), never buffering full files in managed memory; `FitsDatasetReader`/`FitsHeaderReader`/`FitsPixelDataReader`/`FitsPixelConverter` read staged files
- `ESO/`, `MAST/` — archive HTTP clients (`IEsoArchiveClient`, `IMastArchiveClient`), registered via `AddHttpClient<TInterface, TImpl>` with `AddStandardResilienceHandler()` for retries
- `ImageRendering/` — `FitsImageRenderer` + `PngRenderer` turn FITS pixel data into browser-displayable PNGs via `RenderOptions`
- `InfrastructureServiceCollectionExtensions.AddAstroLabInfrastructure(...)` — single DI registration entry point called from `Program.cs`; options bound from config sections (`Storage`, `Eso`, `Mast` — see each `*Options.SectionName`)

### AstroLab.Api — host & vertical slices
Minimal APIs, organized as self-contained feature slices under `Features/` following the **REPR pattern** (Request-Endpoint-Response): each endpoint owns its own request/response DTOs (one per file, defined at the API boundary) and endpoint mapping (`Map*Endpoints()` extension member on `IEndpointRouteBuilder`), registered in `Program.cs`. A `Core` or `Infrastructure` domain model (FITS headers, `ArchiveObservation`, aperture/spectrum measurement results, etc.) is never returned directly as, or embedded unmapped inside, an HTTP response — every response is its own DTO record, built from the `Result<T>` value via a `FromX(...)` mapping helper or inline construction in the endpoint (see spec.md §5.5). The only exception is enums shared across the boundary (`StretchMode`, `ColorMap`, `DispersionAxis`, `ArchiveSource`), which are plain string-serialized discriminators, not models.
- `Features/Fits/` — upload staging + header inspection
- `Features/Imaging/` — FITS → image visualization
- `Features/Photometry/` — aperture measurement endpoints
- `Features/Spectroscopy/` — spectrum extraction endpoints
- `Features/Observations/` — archive metadata search/query
- `Features/ResultEndpointExtensions.cs` — `Result<T>.ToApiResult(...)` maps a Core/Infrastructure `Result` into an `IResult` HTTP response via pattern matching; use this instead of hand-rolling status code mapping in a new endpoint

Endpoint flow: receive request → resolve Infrastructure resources (file store, archive client, native buffer) → call into `AstroLab.Core` for any calculation → map the `Result<T>` to an HTTP response. Endpoints stay thin; no scientific/domain logic belongs in `Features/`.

Enums (e.g. `StretchMode`, `ColorMap`, `DispersionAxis`, `ArchiveSource`) are serialized as strings via a global `JsonStringEnumConverter` registered in `Program.cs`.

## Performance constraints

These are load-bearing, not stylistic — check `AllocationTests.cs` for the enforcement pattern (`GC.GetAllocatedBytesForCurrentThread()`):
- Core hot paths (photometry, imaging, spectroscopy) must not allocate on the managed GC heap, box, use LINQ, or create intermediate collections/arrays.
- Multi-gigabyte FITS pixel buffers live in unmanaged memory (`NativeMemory`, `UnmanagedFitsBuffer`), exposed to Core via spans — never copied wholesale into managed arrays.
- Network/file I/O is streamed end-to-end via `System.IO.Pipelines`, respecting backpressure and cancellation tokens — never fully buffered into a `byte[]`.
