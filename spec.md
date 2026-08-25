# AstroLab — Project Specification

This is the authoritative design and engineering reference for AstroLab. It defines the
architecture, engineering requirements, coding standards, and implementation patterns that
govern every project in this repository. It is intended for both human contributors and AI
coding agents.

For day-to-day operational details such as build/test commands, the current repository layout,
and local setup, see `CLAUDE.md`.

> **How to use this document**
>
> - **Humans:** §1–§2 provide context; §3–§7 are the standing engineering reference.
> - **AI agents:** Treat the **MUST** requirements in §3–§6 as hard constraints. Before
>   completing a task, check the resulting diff against the applicable requirements.
> - **Specific rules override general rules.** Where a section explicitly defines an exception
>   to an earlier rule, the more specific rule applies.
> - **Historical information:** §8 describes the original build sequence and is not an
>   outstanding task list.

## Contents

1. [Overview](#1-overview)
2. [Technology and Constraints](#2-technology-and-constraints)
3. [General Requirements](#3-general-requirements)
4. [Coding Standards](#4-coding-standards)
5. [Architecture](#5-architecture)
   - 5.1 [Solution Structure](#51-solution-structure)
   - 5.2 [Dependency Rules](#52-dependency-rules)
   - 5.3 [Request Flow](#53-request-flow)
   - 5.4 [FITS Dataset Classification](#54-fits-dataset-classification)
6. [Core Implementation Patterns](#6-core-implementation-patterns)
   - 6.1 [Result Pattern](#61-result-pattern)
   - 6.2 [Functional Core: Purity and Spans](#62-functional-core-purity-and-spans)
   - 6.3 [Unmanaged Native Buffers](#63-unmanaged-native-buffers)
   - 6.4 [Pipeline Streaming](#64-pipeline-streaming)
   - 6.5 [Vertical Slice API Endpoints (REPR Pattern)](#65-vertical-slice-api-endpoints-repr-pattern)
   - 6.6 [Archive Clients: ESO and MAST](#66-archive-clients-eso-and-mast)
   - 6.7 [Visualisation as a Separate Capability](#67-visualisation-as-a-separate-capability)
   - 6.8 [Global Exception Handling](#68-global-exception-handling)
7. [Testing Standards](#7-testing-standards)
8. [Appendix: Original Build Sequence (Historical)](#8-appendix-original-build-sequence-historical)

---

## 1. Overview

**AstroLab** is a high-performance .NET 10 RESTful API platform that downloads, stores,
parses, analyses, visualises, and renders FITS (Flexible Image Transport System) scientific
datasets from astronomical archives (ESO and MAST) as well as direct user uploads.

The system uses a **Functional Core, Imperative Shell (FCIS)** design. The pure,
allocation-conscious domain/scientific core (`AstroLab.Core`) is driven by an imperative shell
(`AstroLab.Infrastructure`, `AstroLab.Api`) that owns I/O, native interop, and other side
effects.

Expected domain and infrastructure outcomes are represented with `Result<T>` — a hand-rolled
discriminated union (§6.1). Exceptions are reserved for genuinely exceptional failures at the
imperative shell boundary.

Native memory management (`cfitsio` P/Invoke bindings, `ReadOnlySpan<T>`, `NativeMemory`, and
`System.IO.Pipelines`) is used to process large astronomical files while minimising managed
heap allocations on hot data-processing paths.

A dedicated **FITS Image Visualisation** capability provides browser-consumable representations
of 2D FITS image data, including pixel scaling, image stretching, colour mapping,
NaN/invalid-pixel handling, and image statistics.

---

## 2. Technology and Constraints

- **Target framework:** .NET 10 / C# 14.
- **Database:** None. Metadata and raw datasets are staged on local disk
  (`AstroLab.Infrastructure/Storage`), not a SQL or NoSQL database.
- **Architecture:** Functional Core, Imperative Shell (FCIS), combined with Vertical Slice
  Architecture in the API layer. Each endpoint follows the REPR (Request–Endpoint–Response)
  pattern.
- **Solution:** Exactly four projects: `AstroLab.Core`, `AstroLab.Infrastructure`,
  `AstroLab.Api`, and `AstroLab.Tests`. See §5.1–§5.2 for the complete structure and
  dependency rules.
- **Expected failures:** `Result<T>` is used for expected domain and infrastructure outcomes.
  Exceptions are reserved for genuinely exceptional failures (§6.1).
- **Hot-path allocation:** Core algorithms operating on existing pixel or byte spans MUST perform
  zero managed-heap allocations during steady-state processing. One-time setup and test-harness
  allocations are excluded from this requirement (§6.2, §7.2).
- **FITS visualisation:** 2D FITS image data MUST be transformable into a browser-displayable
  representation (PNG) without mutating the original FITS data.

---

## 3. General Requirements

These requirements define the engineering invariants that apply across the repository. They
describe **what the system must do**; §4 describes **how code is written**.

### 3.1 Production Quality

- **MUST:** Write production-ready, maintainable code. Do not leave TODOs in place of required
  implementations or knowingly ship temporary shortcuts.
- **MUST:** Prefer readability over cleverness. Optimise for the next reader of the code and
  the next reviewer of the diff.
- **MUST:** Keep methods focused on a single responsibility. If describing a method naturally
  requires "and", consider splitting it.
- **SHOULD:** Keep methods under 30 lines where practical. This is a refactoring signal, not
  a hard limit that justifies making otherwise coherent code less readable.
- **MUST:** Avoid unnecessary duplication, but do not over-abstract. Extract shared logic when
  it is genuinely duplicated; do not introduce an interface, base class, or generic abstraction
  solely for a single caller or hypothetical future use.
- **MUST:** Write code that is testable. Prefer pure functions and constructor-injected
  dependencies over static state, ambient context, or hidden singletons.

### 3.2 Validation and Invariants

- **MUST:** Validate all external input before use. This includes HTTP request bodies, query
  parameters, uploaded files, and archive HTTP responses.
- **MUST:** Never create a domain object with invalid properties. A domain object MUST be valid
  immediately after construction.
- **MUST:** For validated domain records that require invariant checking, put argument validation
  in the type's `<Name>Factory.Create(...)` method (§4). Once construction succeeds, callers
  MUST be able to rely on the object's invariants.
- **MUST:** Return `Result<T>` for operations that can fail for a reason a caller should handle,
  including validation failures, missing data, unsupported FITS kinds, and capabilities that
  are intentionally not implemented (§6.1).
- **MUST NOT:** Throw exceptions for expected validation, calculation, invalid-FITS, or other
  caller-handleable failures.
- **MUST:** Every `Error` carries a meaningful human-readable message that states what failed
  and why.

### 3.3 I/O and Cancellation

- **MUST:** Use `async`/`await` for disk, network, and pipeline I/O. Do not introduce synchronous
  fallback paths for these operations.
- **MUST:** Every asynchronous operation that can meaningfully be cancelled accepts a
  `CancellationToken` and propagates it to downstream I/O.
- **MUST NOT:** Block asynchronous code with `.Result` or `.Wait()`. Restructure callers to
  remain asynchronous.

### 3.4 Functional Core

- **MUST:** Scientific algorithms in `AstroLab.Core` be pure, deterministic functions of their
  inputs, with no I/O or side effects (§6.2).

---

## 4. Coding Standards

These conventions apply uniformly across all four projects. They define **how code is written**,
while §3 defines the higher-level engineering requirements.

### 4.1 Structure and Namespaces

- **MUST:** Use file-scoped namespaces. The namespace segments after the project root MUST mirror
  the file's folder path exactly.

  Example:

  `src/AstroLab.Api/Features/Fits/Upload/FitsUploadResponse.cs`

  declares:

  `namespace AstroLab.Api.Features.Fits.Upload;`

- **MUST:** Keep one primary type per file. Explicit companion types such as a type's factory or
  extension container MAY share the file when this specification explicitly permits it.
- **MUST:** Use C# 14 `extension(...)` member syntax for new extension members rather than the
  classic `this`-parameter extension-method form.
- **MUST NOT:** Use primary constructors on classes or structs. Classes and structs MUST use an
  explicit constructor. Positional records remain permitted and are the standard pattern for
  DTOs and value types.

### 4.2 Comments and Literals

- **MUST NOT:** Add `//` comments merely to explain what obvious code does.
- **MAY:** Use `///` XML documentation comments on public types and members where they improve
  API documentation.
- **MUST:** Extract numeric literals that encode domain meaning — scaling factors, thresholds,
  buffer sizes, fallback values, algorithm coefficients, and similar values — into named
  `private const` fields. Structurally self-evident literals such as `0`, `1`, and `2` used as
  indices or simple bounds are exempt.
- **MUST:** Enable nullable reference types in every project with
  `<Nullable>enable</Nullable>`. Use `T?` for legitimately absent references and perform a real
  null check rather than using `!` to suppress the compiler.

### 4.3 Control Flow and LINQ

- **SHOULD:** Prefer LINQ over `for`/`foreach` when the equivalent query remains clear and does
  not conflict with the performance requirements in §6.2.
- **MUST:** Prefer early returns for guard conditions rather than unnecessary `else` blocks or
  deep nesting.
- **SHOULD:** Prefer pattern matching (`is`, property patterns, relational patterns, and
  `switch` expressions) when branching on a value's type, state, or structure, where it
  improves clarity over equivalent `if`/`else` logic.
- **SHOULD:** Prefer switch expressions when a value is produced by branching on a discriminant
  and the branches can be expressed clearly as expressions.
- **MUST:** Suffix asynchronous methods returning `Task`, `Task<T>`, `ValueTask`, or
  `ValueTask<T>` with `Async`, including interface members.
- **SHOULD:** Prefer `var` when the right-hand side makes the type unambiguous at the call site.
  Use an explicit type when it improves clarity.

### 4.4 Immutability and Records

- **SHOULD:** Prefer immutable types by default.
- **MUST:** Configuration classes bound through the Options pattern (`IOptions<T>`) MAY remain
  mutable because the configuration binder requires settable properties.
- **SHOULD:** Types that own disposable/unmanaged resources or expose substantial behaviour may
  remain classes rather than records.
- **MUST:** Use records for immutable data-only types such as DTOs, request/response models,
  value objects, and measurement results. Small value types MAY use `readonly record struct`.
- **SHOULD:** Keep positional record parameter lists on one line where they remain readable.
  If a declaration becomes difficult to read, normal formatting takes precedence.
- **MUST:** Use `ImmutableList<T>` for collection-shaped properties on API-boundary records.
  `AstroLab.Core` hot-path types are exempt and MUST use span/array-based representations
  appropriate to their allocation constraints.
- **MUST:** Records requiring invariant validation MUST be constructed through their accompanying
  `<Name>Factory.Create(...)` method. The factory and record MAY share a file.
- **MAY:** Types with established semantic smart constructors, such as `Error.Validation(...)`
  and `Result<T>.Success(...)`, expose those constructors on the type itself. They MUST funnel
  through the type's factory where the factory convention applies.
- **EXCEPTION:** Request DTO records bound from HTTP request bodies are constructed directly by
  ASP.NET Core model binding during JSON deserialization. No custom `JsonConverter` is required
  solely to route framework construction through a factory. Hand-written construction of such
  DTOs SHOULD still use the factory when validation is required.

### 4.5 Line Endings and Formatting

- **MUST:** Repository files use CRLF line endings, enforced by `.gitattributes`
  (`* text eol=crlf`).
- **SHOULD:** Separate consecutive executable statements with a single blank line when doing so
  improves readability. Do not insert unnecessary blank lines immediately inside or before a
  closing brace.

---

## 5. Architecture

The following are architectural **MUST** constraints. A change that violates one of them is
incorrect even if the resulting code otherwise works.

1. `AstroLab.Core` MUST NOT reference `AstroLab.Infrastructure` or ASP.NET Core.
2. `AstroLab.Core` MUST NOT perform I/O, native interop, or access mutable global state.
3. `AstroLab.Core` MUST contain only pure, deterministic decision logic, scientific/domain
   models, validation, and result/error representations.
4. `AstroLab.Infrastructure` owns native memory, filesystem access, network communication, and
   other external side effects.
5. `AstroLab.Api` feature slices orchestrate `AstroLab.Infrastructure` and `AstroLab.Core`;
   they MUST NOT implement scientific/domain calculations.
6. Expected failures MUST be represented with `Result<T>`; exceptions MUST NOT be used for
   normal domain control flow.
7. Large FITS pixel buffers MUST remain outside the managed GC heap wherever practical.
8. Large network/file payloads MUST be streamed rather than fully buffered into a single
   `byte[]`.
9. Core hot paths MUST operate directly over spans or equivalent allocation-conscious
   representations without intermediate managed allocations.
10. Raw FITS bytes are read from disk only through `AstroLab.Infrastructure/Storage`'s reader
    types (`FitsHeaderReader`, `FitsPixelDataReader`, `FitsPixelConverter`). Decoding
    already-loaded bytes — header cards, keyword/value parsing — is pure, deterministic logic
    and belongs in `AstroLab.Core/Fits` (`FitsCardParser`, `FitsHeader`), per §5.1; it MUST NOT
    perform I/O itself, which is what keeps it Core-eligible despite operating on FITS binary
    structure. `AstroLab.Infrastructure/Fits` (`NativeMethods`, `UnmanagedFitsBuffer`) is
    reserved for a native cfitsio-backed adapter path, if one is wired in later. No layer may
    reinterpret FITS binary structure ad hoc outside these paved paths.
11. FITS header keywords and values MUST survive a read → process → write round trip unless the
    operation explicitly documents that it adds, removes, or rewrites a keyword. Scientific
    provenance is data, not incidental metadata.
12. Scientific analysis and visualisation MUST remain separate concerns. PNG encoding and colour
    mapping MUST NOT be mixed with scientific computation in the same method or call frame.

### 5.1 Solution Structure

Feature slices shape the API around capabilities rather than technical layers. The API separates
FITS inspection, data-type-specific scientific analysis, archive integration, and catalogue
integration. Visualisation remains a separate concern within the relevant data-type feature.

```text
AstroLab.slnx
│
├── src/
│   ├── AstroLab.Core/                              # Pure Functional Core (no dependencies)
│   │   ├── Fits/                                   # Domain models for HDUs and headers
│   │   │   ├── HduDescriptor.cs                    # Per-HDU metadata
│   │   │   ├── FitsDatasetKind.cs                  # Image / Spectrum / TimeSeries / Table / Unknown
│   │   │   └── FitsDatasetClassifier.cs            # Classify(...) + EnsureKind(...)
│   │   ├── Imaging/                                # Pure pixel/scaling/visualisation mathematics
│   │   │   ├── ImageScaler.cs
│   │   │   ├── ImageStatistics.cs
│   │   │   └── ColorMapper.cs
│   │   ├── Photometry/                             # Pure aperture-photometry algorithms
│   │   ├── Spectroscopy/                           # Pure wavelength/spectral algorithms
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
│   │   │   │   ├── Render/                         #   FITS → PNG visualisation
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
│   │   │   │   ├── PeriodSearch/                   #   Periodicity search — HTTP 501
│   │   │   │   └── Transit/                        #   Transit (brightness-dip) search — HTTP 501
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

**Roadmap:** `Images/Sources`, `Images/Astrometry`, `Spectroscopy/Calibrate`,
`Spectroscopy/Lines`, `Spectroscopy/Redshift`, `TimeSeries`, and `Catalogues` are currently
scaffolded at the API boundary but not implemented. Their endpoints return HTTP 501 via the
shared `NotImplementedResult` helper (§6.5). They MUST NOT contain fake success responses,
hard-coded results, or partial scientific implementations.

When a corresponding Core algorithm is implemented, replace the endpoint's
`NotImplementedResult.Value(...)` call with the normal Request → Infrastructure → Core →
`Result<T>` → Response flow. Existing routing and DTOs should remain stable where the new
implementation fits the existing contract.

### 5.2 Dependency Rules

Dependencies flow in one direction:

```text
AstroLab.Api
    │
    ├──► AstroLab.Infrastructure
    │         │
    │         └──► AstroLab.Core
    │
    └──► AstroLab.Core

AstroLab.Tests
    ├──► AstroLab.Api
    ├──► AstroLab.Infrastructure
    └──► AstroLab.Core
```

`AstroLab.Core` MUST NOT reference `AstroLab.Infrastructure` or `AstroLab.Api`.

### 5.3 Request Flow

Every API endpoint follows the same four-stage flow:

1. **Receive request** — route parameters, query parameters, request bodies, or uploaded files
   via ASP.NET Core Minimal APIs.
2. **Resolve infrastructure resources** — file paths, network streams, local FITS files,
   `UnmanagedFitsBuffer` instances, or archive clients.
3. **Invoke functional core** — pass resolved data into pure algorithms from `AstroLab.Core`.
   Mathematical and scientific calculations remain in Core.
4. **Map the result** — pattern-match on `Result<T>` and convert successes and known errors into
   appropriate HTTP responses without exception-based control flow.

```text
HTTP Request
     │
     ▼
AstroLab.Api
(feature endpoint)
     │
     ▼
AstroLab.Infrastructure
(file I/O, HTTP, CFITSIO)
     │
     ▼
UnmanagedFitsBuffer / ReadOnlySpan<T>
     │
     ▼
AstroLab.Core
(pure algorithm)
     │
     ▼
Result<T>
     │
     ▼
HTTP Response
```

### 5.4 FITS Dataset Classification

Before type-specific analysis runs, the system MUST identify what kind of scientific data a
staged FITS file contains and MUST reject analysis requests whose required type does not match
the actual dataset type.

The classification flow is:

```text
FITS File
    │
    ▼
Fits Reader
(AstroLab.Infrastructure.Storage)
    │
    ▼
Inspect HDUs
    │
    ▼
Read Metadata
(HduDescriptor per HDU)
    │
    ▼
Identify Data Type
(FitsDatasetClassifier.Classify — pure Core)
    │
    ├────────────┬─────────────┬──────────────┐
    ▼            ▼             ▼              ▼
  Image       Spectrum     TimeSeries    Table / Unknown
    │            │             │
    └────────────┴─────────────┘
                 │
                 ▼
FitsDatasetClassifier.EnsureKind(hdus, required)
                 │
          ┌──────┴──────┐
          ▼             ▼
        match        mismatch
          │             │
          ▼             ▼
      proceed       Result.Error
```

`FitsDatasetClassifier.Classify` examines the full ordered list of `HduDescriptor` values.

1. If any table HDU (`AsciiTable` or `BinaryTable`) has a `TTYPEn` column named `TIME`, the
   dataset is classified as `TimeSeries`. This check is file-wide because a light-curve table
   does not compete with an image HDU for pixel analysis.
2. Otherwise, find the first HDU with non-empty pixel data using the same
   `HasPixelData` predicate used by `FitsDatasetReader`. Classification and loading MUST agree
   on which HDU is being described.
3. Inspect only that selected HDU's own header for spectral markers:
   - `NAXIS = 1`
   - `DISPAXIS`
   - a `CTYPEn` value beginning with `WAVE`, `FREQ`, `ENER`, `AWAV`, or `VELO`

   A match classifies the dataset as `Spectrum`; otherwise it is `Image`. This permits a raw
   2D long-slit spectrogram to be treated as a spectrum even though its data is stored as a
   normal FITS image.

4. If no HDU has pixel data but a table HDU exists, classify the dataset as `Table`.
5. Otherwise, classify it as `Unknown`.

`FitsDatasetClassifier.EnsureKind(hdus, required)` returns `Result<FitsDatasetKind>`. It
returns `Success` when the classification matches `required`; otherwise it returns a validation
error naming both the required and actual kinds.

`FitsDatasetReader` MUST call `EnsureKind` before reading pixel data. `LoadImageAsync` requires
`Image`; `LoadSpectrumImageAsync` requires `Spectrum`. `Table` and `TimeSeries` classification
exists ahead of their roadmap consumers.

`FitsHeaderReader.ReadAllHeadersAsync` MUST fail with a validation error
(`fits.header.empty_file`) when a staged file contains zero HDUs. A table HDU's
`DataSizeBytes` is calculated from `(NAXIS1 × NAXIS2) + PCOUNT`, with each component clamped
to a non-negative value before combining them, so malformed headers cannot produce a negative
or nonsensical skip distance.

---

## 6. Core Implementation Patterns

### 6.1 Result Pattern

**Location:** `AstroLab.Core/Result/Result.cs` and `Error.cs`

C# has no native discriminated-union type. `Result<TValue>` is a `readonly record struct`
representing either a successful result containing a `TValue` or a failure containing an
`Error`.

The current implementation exposes:

`Success`, `Failure`, `Match`, `Bind`, `Map`, `MapError`, `Ensure`, and `Deconstruct`.

These operations allow Core and Infrastructure outcomes to be composed and mapped to HTTP
responses without exceptions.

`Result<TValue>` has a private constructor so the success/failure invariant is protected.
`Success` and `Failure` are its semantic smart constructors. `ResultFactory.Create<TValue>`
overloads may provide generic, type-inference-friendly entry points and MUST delegate to those
constructors rather than duplicate construction logic.

`Error` is a lightweight `readonly record struct` containing:

- a stable machine-readable code
- a human-readable message
- an `ErrorCategory`

Named constructors include:

`Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Infrastructure`,
`NotImplemented`, `Cancelled`, and `Unexpected`.

These constructors delegate to `ErrorFactory.Create(code, message, category)`, which validates
that `code` and `message` are non-empty.

`ErrorCategory.NotImplemented` represents a named capability whose implementation does not yet
exist. `ResultEndpointExtensions` maps it to HTTP 501.

Exceptions MUST NOT be used for normal domain validation, calculation failures, invalid FITS
data, or other expected failures. They MAY be used at the imperative shell boundary for
genuinely exceptional conditions such as unrecoverable infrastructure, native interop, or
process-level failures.

### 6.2 Functional Core: Purity and Spans

**Location:** `AstroLab.Core`

`AstroLab.Core` is the functional core and MUST remain isolated from infrastructure and external
side effects.

Core algorithms SHOULD be implemented as standard static pure functions wherever practical.
Pure functions:

- depend only on their input parameters
- produce deterministic outputs for identical inputs
- do not modify external or hidden global state
- do not perform I/O
- do not depend on infrastructure implementations

`AstroLab.Core` MUST have:

- zero disk access
- zero network access
- zero native interop
- zero filesystem dependencies
- zero references to `AstroLab.Infrastructure`
- zero dependencies on ASP.NET Core
- zero dependencies on archive clients or storage implementations

The Core project contains domain/scientific models, value types, mathematical algorithms,
validation logic, and result/error representations.

Algorithms operating on large pixel or byte buffers SHOULD accept `ReadOnlySpan<float>`,
`ReadOnlySpan<byte>`, or other span-based representations where appropriate. They SHOULD avoid
intermediate managed arrays.

**Hot-path allocation rule:** Core algorithms operating on existing pixel or byte spans MUST
perform zero managed-heap allocations during steady-state processing. APIs MAY use `Span<T>`,
`ReadOnlyMemory<T>`, `stackalloc`, and `ref struct` where these preserve the functional-core
design and do not compromise usability.

### 6.3 Unmanaged Native Buffers

**Location:** `AstroLab.Infrastructure/Fits`

Infrastructure owns native interop, filesystem access, network communication, and resource
management.

`cfitsio` raw pixel allocations are wrapped with `System.Runtime.InteropServices.NativeMemory`
and `IDisposable`-based ownership because large FITS image buffers may occupy several gigabytes.

`UnmanagedFitsBuffer` MUST:

- allocate native memory using `NativeMemory`
- expose memory to Core algorithms through spans where safe and appropriate
- deterministically release native allocations through `IDisposable`
- make ownership explicit
- prevent double-free operations
- avoid copying large pixel buffers into managed arrays

### 6.4 Pipeline Streaming

**Location:** `AstroLab.Infrastructure/Storage` and `AstroLab.Infrastructure/Archives`

Incoming archive data from ESO and MAST MUST be streamed directly to local storage without
unnecessarily buffering the entire FITS file in managed memory. Use
`System.IO.Pipelines.PipeReader` and `PipeWriter` where appropriate.

The implementation MUST:

- stream network responses incrementally to local staging storage
- avoid loading complete FITS files into a single `byte[]`
- minimise intermediate buffer allocations
- respect backpressure
- correctly complete and dispose pipeline resources
- propagate cancellation tokens throughout the pipeline

### 6.5 Vertical Slice API Endpoints (REPR Pattern)

**Location:** `AstroLab.Api/Features`

API functionality is organised into self-contained vertical slices using ASP.NET Core Minimal
APIs. Each endpoint follows the **REPR (Request–Endpoint–Response)** pattern.

Each endpoint is paired with its endpoint-specific request and response DTOs, defined at the API
boundary within the same feature slice.

- Each feature slice owns its request/response DTOs and endpoint mapping.
- A feature area such as `Images` is a route group, not a single endpoint.
- Each leaf such as `Render`, `Statistics`, `Photometry`, `Inspect`, `Upload`, `Extract`,
  `Search`, or `Download` represents one self-contained endpoint.
- Each leaf owns its `{Leaf}Endpoint.cs`, request/response DTOs, and endpoint-specific mapping.
- Endpoint namespaces follow `AstroLab.Api.Features.{Feature}.{Leaf}`.
- Endpoints MUST remain thin and MUST NOT implement photometry, image scaling, spectral
  extraction, or other scientific algorithms.

A domain or infrastructure model MUST NOT be returned directly from an HTTP endpoint. Every
HTTP response MUST have its own API DTO record under `Features/`, constructed from the
`Result<T>` value returned by Core/Infrastructure. This isolates the wire contract from internal
representation changes.

Shared boundary enums such as `StretchMode`, `ColorMap`, `DispersionAxis`, and `ArchiveSource`
are permitted when they are plain string-serialised discriminators rather than domain models.

### Roadmap Endpoint Rule

A feature scaffolded before its Core algorithm exists MUST return HTTP 501. Its handler MUST
call:

`AstroLab.Api.Features.NotImplementedResult.Value(code, message)`

which returns `Results.Problem(..., statusCode: 501, title: code)`.

Roadmap endpoints MUST NOT return fake success values, hard-coded scientific results, or partial
implementations. When the Core algorithm becomes available, replace the stub with the normal
Request → Infrastructure → Core → `Result<T>` → Response flow.

### 6.6 Archive Clients: ESO and MAST

**Location:** `AstroLab.Infrastructure/Archives`

ESO and MAST clients are HTTP client abstractions. Their real query/download surfaces may not
yet be fully implemented, but their HTTP plumbing is established using resilient
`HttpClient` instances resolved through `IHttpClientFactory`.

Clients SHOULD be registered with:

`AddHttpClient<TInterface, TImpl>()`

and:

`AddStandardResilienceHandler()`

Each client MUST be designed so its concrete request/response contracts can be implemented later
without changing callers, Core, or API feature slices.

Until the real archive query/download contract is known, `SearchAsync` and `DownloadAsync` MUST
return `Error.NotImplemented(...)` rather than sending requests to guessed URLs. A coincidental
2xx response from an unrelated page on the real host MUST NOT be interpreted as a successful
search with zero results.

### 6.7 Visualisation as a Separate Capability

**Location:** `AstroLab.Infrastructure/ImageRendering`,
`AstroLab.Api/Features/Images/Render`

Visualisation is an infrastructure/API concern, not a scientific one. It MUST NOT be implemented
inside `AstroLab.Core`.

Core produces scientific data such as scaled/stretched pixel values, statistics, and source
measurements. Core MUST NOT know about PNG, JPEG, image codecs, or other output encodings.

A concrete rendering dependency such as `PngRenderer` belongs in Infrastructure.

```text
HTTP request (RenderEndpoint)
        │
        ▼
AstroLab.Infrastructure/Fits
(cfitsio-backed pixel read)
        │
        ▼
UnmanagedFitsBuffer / spans
        │
        ▼
AstroLab.Core/Imaging
(ImageScaler, ImageStatistics, ColorMapper)
        │
        ▼
AstroLab.Infrastructure/ImageRendering
(FitsImageRenderer + PngRenderer)
        │
        ▼
HTTP response (image/png)
```

The same separation applies to future spectrum plots, light curves, source overlays, RGB
composites, and false-colour images. Core supplies scientific values; Infrastructure or a thin
API mapping turns those values into the requested visual/wire representation.

A Core algorithm MUST NOT know or care whether its output becomes a PNG, JSON response, FITS
file, or another representation.

### 6.8 Global Exception Handling

**Location:** `AstroLab.Api/GlobalExceptionHandler.cs`, `Program.cs`

`Result<T>` covers expected failures such as validation, missing data, and deliberately
unimplemented capabilities.

Unexpected exceptions escaping an endpoint are caught by `GlobalExceptionHandler`, registered
with `AddExceptionHandler<T>()` and `AddProblemDetails()`, and enabled with
`app.UseExceptionHandler()`.

The handler MUST:

- log the full exception server-side
- return a generic `ProblemDetails` response
- use HTTP 500 with title `unexpected_error`
- never expose stack traces or raw exception messages to callers

Global exception handling is a safety net, not a substitute for `Result<T>`. A failure mode that
can reasonably be anticipated MUST be represented explicitly with `Result<T>`.

---

## 7. Testing Standards

**Location:** `AstroLab.Tests`

Tests cover Core, Infrastructure, and API layers.

### 7.1 Core Unit Tests

**Location:** `AstroLab.Tests/Core/`

Tests MUST verify, at minimum:

- photometry calculations, including circular aperture flux and annular background estimation
- image scaling and expected normalised values, including logarithmic scaling
- spectrum extraction and expected one-dimensional output
- `Result<T>` success and failure behaviour
- expected domain failures without exception-based control flow

### 7.2 Allocation Tests

Performance/allocation tests MUST verify hot-path Core algorithms operating on existing spans.

The tests MUST detect:

- unnecessary managed-array allocations
- hidden LINQ allocations
- boxed value types
- unnecessary intermediate collections

Allocation is measured using `GC.GetAllocatedBytesForCurrentThread()` or a dedicated
benchmarking/allocation framework where appropriate.

Tests MUST distinguish one-time setup and test-harness allocations from allocations performed
by the algorithm under test.

The requirement applies specifically to the **hot data-processing path**, which MUST perform
zero managed-heap allocations.

See `AstroLab.Tests/Core/AllocationTests.cs` for the current enforcement pattern.

---

## 8. Appendix: Original Build Sequence (Historical)

AstroLab was originally scaffolded by an AI coding agent (Claude Code) using the build order
below. The solution described by this document already exists in the repository.

This appendix is **historical**. It is retained as a reference for extending the same
architectural pattern to new capability areas; it is not an outstanding task list.

| Phase | What was built                                        | Governing specification |
| ----- | ----------------------------------------------------- | ----------------------- |
| 1     | Solution and project scaffolding                      | §5.1, §5.2              |
| 2     | Core `Result` / `Error` types                         | §6.1                    |
| 3     | Core FITS/domain models                               | §5.1                    |
| 4     | Core photometry, imaging, and spectroscopy algorithms | §6.2                    |
| 5     | Core unit and allocation tests                        | §7                      |
| 6     | CFITSIO native bindings and `UnmanagedFitsBuffer`     | §6.3                    |
| 7     | `LocalFileStore` / pipeline streaming                 | §6.4                    |
| 8     | ESO and MAST archive clients                          | §6.6                    |
| 9     | API vertical slices                                   | §6.5, §5.3              |
| 10    | API integration tests                                 | §7                      |
| 11    | Full solution validation                              | —                       |

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

At each stage, the implementation compiled and its tests remained passing before proceeding to
the next stage. The same discipline applies to future work that extends this architectural
pattern.
