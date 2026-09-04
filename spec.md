# AstroLab — Project Specification

This is the authoritative design and engineering reference for AstroLab. It defines the architecture, engineering requirements, coding standards, and implementation patterns that govern the repository. It is intended for both human contributors and AI coding agents.

For day-to-day operational details such as build/test commands, the current repository layout, and local setup, see `CLAUDE.md`.

> **How to use this document**
>
> - **Humans:** §1–§2 provide context; §3–§7 are the standing engineering reference.
> - **AI agents:** Treat the **MUST** requirements in §3–§6 as hard constraints. Before completing a task, check the resulting diff against the applicable requirements.
> - **Specific rules override general rules.** Where a section explicitly defines an exception to an earlier rule, the more specific rule applies.
> - **Historical information:** §8 describes the original build sequence and is not an outstanding task list.

## Contents

1. [Overview](#1-overview)
2. [Technology and Constraints](#2-technology-and-constraints)
3. [General Requirements](#3-general-requirements)
4. [Coding Standards](#4-coding-standards)
   - 4.1 [Structure and Namespaces](#41-structure-and-namespaces)
   - 4.2 [Comments and Literals](#42-comments-and-literals)
   - 4.3 [Control Flow and LINQ](#43-control-flow-and-linq)
   - 4.4 [Immutability and Records](#44-immutability-and-records)
   - 4.5 [Line Endings and Formatting](#45-line-endings-and-formatting)
5. [Architecture](#5-architecture)
   - 5.1 [Solution Structure](#51-solution-structure)
   - 5.2 [Dependency Rules](#52-dependency-rules)
   - 5.3 [Request Flow](#53-request-flow)
   - 5.4 [FITS Dataset Capabilities](#54-fits-dataset-capabilities)
   - 5.5 [Deployment](#55-deployment)
6. [Core Implementation Patterns](#6-core-implementation-patterns)
   - 6.1 [Result Pattern](#61-result-pattern)
   - 6.2 [Functional Core: Purity and Allocation Awareness](#62-functional-core-purity-and-allocation-awareness)
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

**AstroLab** is a high-performance .NET 10 RESTful API platform that downloads, stores, parses, analyses, visualises, and renders FITS (Flexible Image Transport System) scientific datasets from astronomical archives such as ESO and MAST, as well as direct user uploads.

The system uses a **Functional Core, Imperative Shell (FCIS)** design. The pure, allocation-conscious domain/scientific core (`AstroLab.Core`) is driven by an imperative shell (`AstroLab.Infrastructure`, `AstroLab.Api`) that owns I/O, native interop, and other side effects.

Expected domain and infrastructure outcomes are represented with `Result<T>` — a hand-rolled discriminated union (§6.1). Exceptions are reserved for genuinely exceptional conditions and for programmer misuse at appropriate boundaries.

Native memory management, `ReadOnlySpan<T>`, `NativeMemory`, and `System.IO.Pipelines` may be used where they provide measurable benefits when processing large astronomical files. Performance-sensitive code should minimise unnecessary managed allocations while remaining readable and maintainable.

A dedicated **FITS Image Visualisation** capability provides browser-consumable representations of 2D FITS image data, including pixel scaling, image stretching, colour mapping, NaN/invalid-pixel handling, and image statistics.

---

## 2. Technology and Constraints

- **Target framework:** .NET 10 / C# 14.
- **Database:** None. Metadata and raw datasets are staged on local disk (`AstroLab.Infrastructure/Storage`), not a SQL or NoSQL database.
- **Architecture:** Functional Core, Imperative Shell (FCIS), combined with Vertical Slice Architecture in the API layer. Each endpoint follows the REPR (Request–Endpoint–Response) pattern.
- **Solution structure:** The current solution is organised into `AstroLab.Core`, `AstroLab.Infrastructure`, `AstroLab.Api`, and `AstroLab.Tests`. New projects MAY be introduced when a genuine architectural or operational boundary justifies them; project proliferation without a meaningful boundary is discouraged.
- **Expected failures:** `Result<T>` is used for expected domain and infrastructure outcomes. Exceptions are reserved for genuinely exceptional failures and programmer misuse.
- **Performance:** Performance-critical Core algorithms MUST avoid unnecessary allocations and intermediate representations. Zero-allocation implementations SHOULD be used where practical and demonstrably beneficial, but allocation avoidance MUST NOT be treated as an absolute requirement for every algorithm or result type.
- **FITS visualisation:** 2D FITS image data MUST be transformable into a browser-displayable representation (PNG) without mutating the original FITS data.

---

## 3. General Requirements

These requirements define the engineering invariants that apply across the repository. They describe **what the system must do**; §4 describes **how code is written**.

### 3.1 Production Quality

- **MUST:** Write production-ready, maintainable code. Do not leave TODOs in place of required implementations or knowingly ship temporary shortcuts.
- **MUST:** Prefer readability over cleverness. Optimise for the next reader of the code and the next reviewer of the diff.
- **MUST:** Keep methods focused on a single responsibility. If describing a method naturally requires "and", consider splitting it.
- **SHOULD:** Keep methods under 30 lines where practical. This is a refactoring signal, not a hard limit that justifies making otherwise coherent code less readable.
- **MUST:** Avoid unnecessary duplication, but do not over-abstract. Extract shared logic when it is genuinely duplicated; do not introduce an interface, base class, or generic abstraction solely for a single caller or hypothetical future use.
- **MUST:** Write code that is testable. Prefer pure functions and constructor-injected dependencies over static state, ambient context, or hidden singletons.
- **SHOULD:** Prefer the simplest design that satisfies the requirements. Do not introduce performance-oriented complexity, abstractions, or unsafe code without a clear reason.

### 3.2 Validation and Invariants

- **MUST:** Validate all external input before use. This includes HTTP request bodies, query parameters, uploaded files, and archive HTTP responses.
- **MUST:** Never allow an invalid domain object to enter a state where its invariants are violated.
- **MUST:** Domain invariants belong to the domain type and MUST be enforced at the domain boundary.
- **MUST:** Return `Result<T>` for operations that can fail for a reason a caller should handle, including domain validation failures, missing data, unsupported FITS capabilities, and expected infrastructure failures (§6.1).
- **MUST NOT:** Throw exceptions for expected domain validation, scientific calculation, invalid-FITS, or other caller-handleable failures.
- **MAY:** Use `ArgumentException`, `ArgumentOutOfRangeException`, or related exceptions for programmer misuse of an API where the caller has violated the method's contract rather than supplied ordinary invalid user input.
- **MAY:** Request-boundary validation use exceptions where required by the ASP.NET Core binding/design approach, provided those exceptions are caught and translated into an appropriate client response at the API boundary. Such exceptions MUST NOT leak into Core as normal domain control flow.
- **MUST:** Every `Error` carries a meaningful human-readable message that states what failed and why.
- **SHOULD:** Prefer stable machine-readable error codes over clients depending on error-message text.

### 3.3 I/O and Cancellation

- **MUST:** Use `async`/`await` for disk, network, and pipeline I/O. Do not introduce synchronous fallback paths for these operations.
- **MUST:** Every asynchronous operation that can meaningfully be cancelled accepts a `CancellationToken` and propagates it to downstream I/O.
- **MUST NOT:** Block asynchronous code with `.Result` or `.Wait()`. Restructure callers to remain asynchronous.
- **SHOULD:** Honour cancellation promptly, particularly during large FITS downloads, file reads, and streaming operations.

### 3.4 Functional Core

- **MUST:** Scientific algorithms in `AstroLab.Core` be pure, deterministic functions of their inputs, with no I/O or side effects (§6.2).
- **MUST:** Keep infrastructure concerns such as filesystem access, network communication, native interop, image encoding, and archive-specific protocols outside Core.
- **SHOULD:** Keep Core algorithms independent of the representation used by the API or Infrastructure layer.

### 3.5 NuGet Packages

- **MUST:** Always search nuget.org for the latest stable version of each package before writing or modifying any `.csproj` package reference.
- **MUST NOT:** Rely on training data for package version numbers, as they become outdated.
- **SHOULD:** Avoid adding a dependency when the required functionality can be implemented clearly using the BCL or an existing dependency.
- **MUST:** Treat third-party dependencies as implementation details unless the dependency itself forms an explicit architectural boundary.

---

## 4. Coding Standards

These conventions apply uniformly across the solution. They define **how code is written**, while §3 defines the higher-level engineering requirements.

### 4.1 Structure and Namespaces

- **MUST:** Use file-scoped namespaces. The namespace segments after the project root MUST mirror the file's folder path exactly.

  Example:

  `src/AstroLab.Api/Features/Fits/Upload/FitsUploadResponse.cs`

  declares:

  `namespace AstroLab.Api.Features.Fits.Upload;`

- **MUST:** Keep one primary type per file. An explicit companion extension container MAY share the file when this specification explicitly permits it. A record's `Create(...)` factory method lives on the record itself (§4.4), not in a companion type, so it never counts against this rule.
- **MUST:** Use C# 14 `extension(...)` member syntax for new extension members rather than the classic `this`-parameter extension-method form.
- **MUST NOT:** Use primary constructors on classes, structs, or records. All three use an explicit constructor body. Records follow the private-constructor-plus-`Create(...)` pattern in §4.4.

### 4.2 Comments and Literals

- **MUST NOT:** Add `//` comments to explain code whose purpose or behaviour is already clear from its implementation.
- **MUST NOT:** Add `///` XML documentation comments to models, DTOs, records, or their properties, including request/response DTOs and other data-only types. Type and property names MUST be sufficiently descriptive and self-documenting.
- **MUST:** Add a `///` XML documentation comment to:
  - Every endpoint class (`{Leaf}Endpoint.cs`), describing the endpoint's purpose and behaviour.
  - Every class in `AstroLab.Core` and `AstroLab.Infrastructure`, describing the class's responsibility.
- **SHOULD:** Add comments when they explain non-obvious reasoning, scientific assumptions, external protocol behaviour, safety requirements, or intentionally unusual implementation decisions.
- **MUST:** Extract numeric literals that encode domain meaning — scaling factors, thresholds, buffer sizes, fallback values, algorithm coefficients, and similar values — into named `private const` fields. Structurally self-evident literals such as `0`, `1`, and `2` used as indices or simple bounds are exempt.
- **MUST:** Enable nullable reference types in every project with `<Nullable>enable</Nullable>`. Use `T?` for legitimately absent references and perform a real null check rather than using `!` to suppress the compiler.
- **MUST NOT:** Add redundant parentheses to a mathematical expression — parentheses that restate C#'s existing operator precedence rather than changing evaluation order. Use parentheses only where they are required to produce the correct result, or where a mixed chain of different operator kinds would otherwise be genuinely ambiguous to a reader.
- **MUST NOT:** Add a trailing comma after the last member of an `enum` declaration.

### 4.3 Control Flow and LINQ

- **SHOULD:** Prefer LINQ for collection-oriented operations when it improves readability and does not introduce a meaningful performance or allocation cost.
- **SHOULD:** Prefer explicit `for`/`foreach` loops for numerical, pixel-processing, buffer-processing, or other performance-critical algorithms when they provide clearer control over iteration, memory access, allocations, or algorithmic complexity.
- **MUST NOT:** Avoid LINQ merely because it has historically been considered slow. Modern .NET provides highly optimised implementations for many common LINQ operations. Choose between LINQ and explicit iteration based on readability and the characteristics of the workload.
- **SHOULD:** Benchmark genuinely performance-critical alternatives rather than relying on assumptions about LINQ or loops.
- **MUST:** Prefer early returns for guard conditions rather than unnecessary `else` blocks or deep nesting.
- **SHOULD:** Prefer pattern matching (`is`, property patterns, relational patterns, and `switch` expressions) when branching on a value's type, state, or structure, where it improves clarity over equivalent `if`/`else` logic.
- **SHOULD:** Prefer switch expressions when a value is produced by branching on a discriminant and the branches can be expressed clearly as expressions.
- **MUST:** Suffix asynchronous methods returning `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` with `Async`, including interface members.
- **SHOULD:** Prefer `var` when the right-hand side makes the type unambiguous at the call site. Use an explicit type when it improves clarity.

### 4.4 Immutability and Records

- **SHOULD:** Prefer immutable types by default.
- **MUST:** Configuration classes bound through the Options pattern (`IOptions<T>`) MAY remain mutable because the configuration binder requires settable properties.
- **SHOULD:** Types that own disposable/unmanaged resources or expose substantial behaviour may remain classes rather than records.
- **MUST:** Use records for immutable data-only types such as DTOs, request/response models, value objects, and measurement results. Small value types MAY use `readonly record struct`.
- **MUST:** A concrete record type defaults to `sealed`. Leave a record unsealed only when inheritance/polymorphism is an explicit, documented part of its design. `readonly record struct` types are implicitly sealed and MUST NOT carry the modifier.
- **MUST:** Declare a record's properties explicitly with `{ get; }` accessors, never `{ get; init; }`, and set them only from the record's own constructor. Because properties are get-only, records do not support `with`-expression mutation; construct a new instance through `Create(...)` instead.
- **MUST:** A record is constructed through a private constructor plus a public static `Create(...)` method declared on the record type itself — not a companion `<Name>Factory` class. `Create(...)` validates its arguments and returns `new(...)`; the constructor performs no validation and MUST NOT be called from outside the record's own file. This makes the record impossible to construct in an invalid state.
- **MUST:** When a record's invariants need checking against an already-constructed instance, the record exposes a public `Validate()` instance method containing those checks, and `Create(...)` calls it internally instead of duplicating the checks inline.
- **MUST NOT:** Add an empty `Validate()` method purely for symmetry when a record has no invariants to check.
- **MUST:** Use `ImmutableList<T>` for collection-shaped properties on API-boundary records. `AstroLab.Core` hot-path types are exempt and MUST use span/array-based representations appropriate to their allocation constraints.
- **MAY:** Types with established semantic smart constructors, such as `Error.Validation(...)` and `Result<T>.Success(...)`, expose those constructors directly on the type instead of a generic `Create(...)`, as long as they still funnel through the same private constructor.
- **EXCEPTION:** A request DTO record bound directly from an HTTP request body (no `[AsParameters]`) keeps a **private** constructor but marks it `[JsonConstructor]` (`System.Text.Json.Serialization`) so `System.Text.Json` can still use it during model binding. Construction via the framework bypasses `Create`'s validation. Hand-written construction SHOULD still go through `Create(...)` when validation is required. Because the endpoint handler receives an already-constructed instance from model binding, it MUST call that instance's own `request.Validate()` when applicable.
- **EXCEPTION:** A request DTO record bound via `[AsParameters]` (query/route parameter binding) MUST keep a **public** constructor. ASP.NET Core's parameter-binding metadata cache requires a public constructor for `[AsParameters]` complex-type binding. The constructor still performs no validation, properties remain `{ get; }`-only, and `Create(...)` remains the validated entry point for hand-written construction.

Example:

```csharp
public sealed record ApertureMeasurement
{
    private ApertureMeasurement(double flux, double area, int sampledPixelCount)
    {
        Flux = flux;
        Area = area;
        SampledPixelCount = sampledPixelCount;
    }

    public double Flux { get; }

    public double Area { get; }

    public int SampledPixelCount { get; }

    public static ApertureMeasurement Create(
        double flux,
        double area,
        int sampledPixelCount)
    {
        var measurement = new ApertureMeasurement(
            flux,
            area,
            sampledPixelCount);

        measurement.Validate();

        return measurement;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Area);
        ArgumentOutOfRangeException.ThrowIfNegative(SampledPixelCount);
    }
}
```

### 4.5 Line Endings and Formatting

- **MUST:** Repository files use CRLF line endings, enforced by `.gitattributes` (`* text eol=crlf`).
- **SHOULD:** Separate consecutive executable statements with a single blank line when doing so improves readability, except between variable assignments in constructors.
- **MUST NOT:** Insert unnecessary blank lines immediately inside or before a closing brace.

---

## 5. Architecture

The following are architectural **MUST** constraints. A change that violates one of them is incorrect even if the resulting code otherwise works.

1. `AstroLab.Core` MUST NOT reference `AstroLab.Infrastructure` or ASP.NET Core.
2. `AstroLab.Core` MUST NOT perform I/O, native interop, or access mutable global state.
3. `AstroLab.Core` MUST contain pure deterministic decision logic, scientific/domain models, validation, algorithms, and result/error representations.
4. `AstroLab.Infrastructure` owns native memory, filesystem access, network communication, archive protocols, image encoding, and other external side effects.
5. `AstroLab.Api` feature slices orchestrate Infrastructure and Core; they MUST NOT implement scientific/domain calculations.
6. Expected failures MUST be represented with `Result<T>`; exceptions MUST NOT be used for normal domain control flow.
7. Large FITS pixel buffers SHOULD remain outside the managed GC heap where practical.
8. Large network/file payloads MUST be streamed rather than fully buffered into a single `byte[]`.
9. Core performance-critical paths SHOULD operate directly over spans or equivalent allocation-conscious representations without unnecessary intermediate managed allocations.
10. Raw FITS bytes are read from disk through Infrastructure storage/FITS reader types. Decoding already-loaded FITS structures — such as header cards and keyword/value parsing — is pure deterministic logic and belongs in Core where appropriate.
11. **CFITSIO is an implementation detail.** The architecture MUST NOT depend on CFITSIO-specific types or APIs outside the Infrastructure boundary that owns the native adapter.
12. Scientific analysis and visualisation MUST remain separate concerns. PNG encoding and colour mapping MUST NOT be mixed with scientific computation in the same method or call frame.
13. FITS read/write round-trip preservation is **not** a repository-wide architectural requirement. If FITS writing is introduced, the writing capability MUST explicitly define which metadata and provenance must be preserved.

### 5.1 Solution Structure

Feature slices shape the API around capabilities rather than technical layers. The API separates FITS inspection, data-type-specific scientific analysis, archive integration, and catalogue integration. Visualisation remains a separate concern within the relevant data-type feature.

The current solution is structured as follows:

```text
AstroLab.slnx
│
├── src/
│   ├── AstroLab.Core/                         # Pure Functional Core
│   │   ├── Fits/                              # FITS domain models and parsing
│   │   │   ├── HduDescriptor.cs
│   │   │   ├── FitsDatasetKind.cs
│   │   │   └── FitsDatasetClassifier.cs
│   │   ├── Imaging/                           # Pure image/scaling mathematics
│   │   │   ├── ImageScaler.cs
│   │   │   ├── ImageStatistics.cs
│   │   │   └── ColorMapper.cs
│   │   ├── Astrometry/                         # WCS parsing and conversion
│   │   │   └── Wcs.cs
│   │   ├── Photometry/                         # Aperture-photometry algorithms
│   │   ├── Sources/                            # Source detection
│   │   │   └── SourceDetector.cs
│   │   ├── Spectroscopy/                       # Spectral algorithms
│   │   └── Result/                             # Result<T> / Error
│   │
│   ├── AstroLab.Infrastructure/                # Imperative Shell
│   │   ├── Fits/                               # FITS adapter / native interop
│   │   ├── Storage/                            # Local storage and streaming
│   │   ├── Archives/                           # ESO and MAST clients
│   │   └── ImageRendering/                     # FITS → browser image rendering
│   │
│   ├── AstroLab.Api/                           # API Host & Vertical Slices
│   │   ├── Features/
│   │   │   ├── Fits/
│   │   │   │   ├── Upload/
│   │   │   │   └── Inspect/
│   │   │   ├── Images/
│   │   │   │   ├── Render/
│   │   │   │   ├── Statistics/
│   │   │   │   ├── Photometry/
│   │   │   │   ├── Sources/
│   │   │   │   ├── Astrometry/
│   │   │   │   ├── MultiPhotometry/
│   │   │   │   ├── DifferentialPhotometry/
│   │   │   │   ├── SourceCharacterization/
│   │   │   │   ├── Background/
│   │   │   │   ├── Segmentation/
│   │   │   │   ├── Compare/
│   │   │   │   ├── Align/
│   │   │   │   ├── Stack/
│   │   │   │   ├── Separation/
│   │   │   │   ├── Footprint/
│   │   │   │   └── Overlay/
│   │   │   ├── Spectroscopy/
│   │   │   │   ├── Extract/
│   │   │   │   ├── Calibrate/
│   │   │   │   ├── Lines/
│   │   │   │   ├── Redshift/
│   │   │   │   └── Compare/
│   │   │   ├── TimeSeries/
│   │   │   │   ├── LightCurve/
│   │   │   │   ├── Detrend/
│   │   │   │   ├── PeriodSearch/
│   │   │   │   ├── Transit/
│   │   │   │   └── Compare/
│   │   │   ├── Catalogues/
│   │   │   │   ├── Query/
│   │   │   │   └── CrossMatch/
│   │   │   ├── Measurements/
│   │   │   │   ├── StellarColour/
│   │   │   │   ├── StellarTemperature/
│   │   │   │   ├── SpectralClassification/
│   │   │   │   ├── RadialVelocity/
│   │   │   │   ├── GalaxyMorphology/
│   │   │   │   ├── SurfaceBrightness/
│   │   │   │   └── PhysicalSize/
│   │   │   └── Archives/
│   │   │       ├── Search/
│   │   │       └── Download/
│   │   └── Program.cs
│   │
│   └── AstroLab.Tests/
│       ├── Core/
│       ├── Infrastructure/
│       └── Features/
│
└── storage/
```

The exact folder layout MAY evolve as the system grows. A new project or major structural boundary SHOULD only be introduced when it represents a genuine separation of responsibility, deployment, dependency, or ownership.

The current four-project arrangement is the preferred default, not an immutable requirement.

Roadmap features remain explicitly represented at the API boundary where they have been intentionally scaffolded. They MUST return HTTP 501 until their corresponding implementation exists and MUST NOT return fake scientific results.

### 5.2 Dependency Rules

Dependencies flow inward toward Core:

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

- `AstroLab.Core` MUST NOT reference `AstroLab.Infrastructure` or `AstroLab.Api`.
- `AstroLab.Infrastructure` MAY reference Core abstractions and models required to implement infrastructure capabilities.
- `AstroLab.Api` MAY reference both Core and Infrastructure.
- Tests MAY reference all production projects.
- Infrastructure-specific wire formats, native handles, archive DTOs, and persistence representations MUST NOT leak into API contracts.

### 5.3 Request Flow

Every API endpoint follows the same conceptual four-stage flow:

1. **Receive request** — route parameters, query parameters, request bodies, or uploaded files via ASP.NET Core Minimal APIs.
2. **Resolve infrastructure resources** — file paths, network streams, local FITS files, native buffers, archive clients, or other external resources.
3. **Invoke functional core** — pass resolved data and validated inputs into pure algorithms from `AstroLab.Core`.
4. **Map the result** — pattern-match on `Result<T>` and convert successes and known errors into appropriate HTTP responses without exception-based control flow.

```text
HTTP Request
     │
     ▼
AstroLab.Api
(feature endpoint)
     │
     ▼
AstroLab.Infrastructure
(file I/O, HTTP, FITS adapter)
     │
     ▼
Native buffers / spans / managed representations
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

Endpoints MUST remain orchestration code. Scientific calculations belong in Core.

### 5.4 FITS Dataset Capabilities

The system MUST determine which operations a FITS dataset can legitimately support before executing type-specific analysis.

A FITS file is not necessarily one scientifically exclusive "type". A dataset can contain multiple HDUs and can support multiple forms of analysis. For example, a file may contain an image HDU, a catalogue table, and metadata relevant to time-series analysis.

Therefore, classification SHOULD be understood as **capability detection** rather than an assertion that the entire FITS file belongs to exactly one scientific category.

The Core should expose an appropriate representation of capabilities, for example:

```text
FITS File
   │
   ▼
Inspect HDUs
   │
   ▼
Extract metadata/capabilities
   │
   ├── Image data available
   ├── Spectral data available
   ├── Time-series/table data available
   ├── WCS available
   └── Other recognised capabilities
```

The implementation MAY continue to expose `FitsDatasetKind` where a single primary kind is useful for backwards compatibility or routing, but the classifier MUST NOT rely on simplistic heuristics that incorrectly imply scientific certainty.

In particular:

- The presence of a `TIME` column alone MUST NOT automatically imply that the entire FITS dataset is a time series.
- The first HDU containing pixels MUST NOT automatically be treated as the sole scientifically relevant HDU.
- Spectral suitability SHOULD consider the relevant HDU's dimensionality and wavelength/frequency/energy/velocity metadata.
- Time-series suitability SHOULD consider the table structure and the presence of appropriate time and measurement columns.
- Image suitability SHOULD be based on actual image data and its dimensions.
- Table suitability SHOULD be based on the presence of structured table data.
- WCS availability SHOULD be detected independently from image/spectrum/table classification.
- Multiple capabilities MAY coexist.

Capability detection MUST be deterministic and based only on the inspected FITS metadata.

Analysis endpoints MUST validate that the required capability is available before attempting the operation.

For example:

```text
Image Photometry
    requires: ImageData

Astrometry
    requires: ImageData + WCS

Spectral Extraction
    requires: SpectralData

Time-Series Analysis
    requires: TimeSeriesData
```

`FitsDatasetClassifier`/capability detection belongs in Core because it is pure metadata interpretation.

`FitsDatasetReader` MUST ensure that the required capability is available before loading the associated data.

`FitsHeaderReader.ReadAllHeadersAsync` MUST fail with a validation error (`fits.header.empty_file`) when a staged file contains zero HDUs.

Malformed FITS metadata MUST NOT result in negative or nonsensical skip distances or buffer sizes. Numeric sizes derived from FITS headers MUST be validated and bounded before being used for I/O.

### 5.5 Deployment

**Location:** `Dockerfile` (repo root)

`Dockerfile` is a multi-stage build producing a Linux container image.

The runtime stage uses:

- `mcr.microsoft.com/dotnet/aspnet:10.0`
- a non-root application user
- port `8080`
- `/app/storage` as the persistent storage volume

The FITS native dependency is installed through the Linux distribution's package manager rather than relying on manually copied native binaries.

CFITSIO remains an Infrastructure implementation detail. The application architecture MUST NOT depend on a particular filesystem location or native deployment mechanism beyond the Infrastructure adapter's documented requirements.

---

## 6. Core Implementation Patterns

### 6.1 Result Pattern

**Location:** `AstroLab.Core/Result/Result.cs` and `Error.cs`

C# has no native discriminated-union type. `Result<TValue>` is a `readonly record struct` representing either a successful result containing a `TValue` or a failure containing an `Error`.

The current implementation exposes:

- `Success`
- `Failure`
- `Match`
- `Bind`
- `Map`
- `MapError`
- `Ensure`
- `Deconstruct`

These operations allow Core and Infrastructure outcomes to be composed and mapped to HTTP responses without exceptions.

`Result<TValue>` has a private constructor so the success/failure invariant is protected.

`Success` and `Failure` are semantic smart constructors and are the only supported ways to obtain a `Result<TValue>`.

`Error` is a lightweight `readonly record struct` containing:

- a stable machine-readable code
- a human-readable message
- an `ErrorCategory`

Named constructors include:

- `Validation`
- `NotFound`
- `Conflict`
- `Unauthorized`
- `Infrastructure`
- `NotImplemented`
- `Cancelled`
- `Unexpected`

These constructors delegate to `Error`'s private constructor, which validates that `code` and `message` are non-empty.

`ErrorCategory.NotImplemented` represents a named capability whose implementation does not yet exist. `ResultEndpointExtensions` maps it to HTTP 501.

Exceptions MUST NOT be used for normal domain validation, scientific calculation failures, invalid FITS data, or other expected failures.

They MAY be used at the imperative shell boundary for genuinely exceptional conditions such as unrecoverable infrastructure, native interop, or process-level failures.

Programmer misuse MAY use standard .NET argument exceptions where appropriate.

### 6.2 Functional Core: Purity and Allocation Awareness

**Location:** `AstroLab.Core`

`AstroLab.Core` is the functional core and MUST remain isolated from infrastructure and external side effects.

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

The Core project contains:

- domain/scientific models
- value types
- mathematical algorithms
- validation logic
- result/error representations
- FITS metadata interpretation that does not require I/O

Algorithms operating on large pixel or byte buffers SHOULD accept `ReadOnlySpan<T>`, `ReadOnlyMemory<T>`, arrays, or other appropriate representations depending on the lifetime and ownership requirements of the algorithm.

Use spans when they provide a meaningful advantage, particularly for:

- avoiding unnecessary copies
- processing existing buffers
- expressing contiguous memory access
- enabling allocation-conscious hot paths

However, spans, `ref struct`, `stackalloc`, and unsafe constructs MUST NOT be introduced merely to satisfy an abstract zero-allocation rule.

### Performance and Allocation Rule

Performance-critical Core algorithms SHOULD minimise managed allocations and unnecessary intermediate collections.

For genuinely hot data-processing paths:

- avoid unnecessary heap allocations
- avoid unnecessary LINQ pipelines when they materially affect allocations or performance
- avoid repeated temporary arrays
- avoid boxing value types
- prefer spans or equivalent representations where appropriate
- consider vectorisation where measurement demonstrates a meaningful benefit
- benchmark before introducing complex optimisations

A Core algorithm MAY allocate when the allocation is part of its natural result.

For example, source detection may reasonably return a collection of detected sources, and a photometry operation may reasonably return a measurement object.

The requirement is therefore:

> **Avoid unnecessary allocations, especially in hot loops and per-pixel processing, rather than requiring every Core method to be absolutely allocation-free.**

Allocation behaviour SHOULD be measured for algorithms identified as performance-critical.

### 6.3 Unmanaged Native Buffers

**Location:** `AstroLab.Infrastructure/Fits`

Infrastructure owns native interop, filesystem access, network communication, and resource management.

When large FITS image buffers justify unmanaged storage, they MAY be allocated using `System.Runtime.InteropServices.NativeMemory` or an equivalent mechanism.

`UnmanagedFitsBuffer` MUST:

- allocate native memory using the selected native allocation mechanism
- expose memory to Core algorithms through spans where safe and appropriate
- deterministically release native allocations through `IDisposable`
- make ownership explicit
- prevent double-free operations
- avoid copying large pixel buffers into managed arrays unnecessarily

CFITSIO-specific handles and P/Invoke declarations MUST remain inside Infrastructure.

The rest of the application MUST depend on AstroLab abstractions rather than CFITSIO APIs.

If a future implementation replaces CFITSIO with another FITS reader, Core and API code SHOULD require no changes.

### 6.4 Pipeline Streaming

**Location:** `AstroLab.Infrastructure/Storage` and `AstroLab.Infrastructure/Archives`

Incoming archive data from ESO and MAST MUST be streamed directly to local storage without unnecessarily buffering the entire FITS file in managed memory.

Use `System.IO.Pipelines.PipeReader` and `PipeWriter` where appropriate.

The implementation MUST:

- stream network responses incrementally to local staging storage
- avoid loading complete FITS files into a single `byte[]`
- minimise intermediate buffer allocations
- respect backpressure where pipelines are used
- correctly complete and dispose pipeline resources
- propagate cancellation tokens throughout the pipeline
- avoid retrying large downloads automatically unless the operation explicitly supports safe resumability

### 6.5 Vertical Slice API Endpoints (REPR Pattern)

**Location:** `AstroLab.Api/Features`

API functionality is organised into self-contained vertical slices using ASP.NET Core Minimal APIs. Each endpoint follows the **REPR (Request–Endpoint–Response)** pattern.

Each endpoint is paired with its endpoint-specific request and response DTOs, defined at the API boundary within the same feature slice.

- Each feature slice owns its request/response DTOs and endpoint mapping.
- A feature area such as `Images` is a route group, not a single endpoint.
- Each leaf such as `Render`, `Statistics`, `Photometry`, `Inspect`, `Upload`, `Extract`, `Search`, or `Download` represents one self-contained endpoint.
- Each leaf owns its `{Leaf}Endpoint.cs`, request/response DTOs, and endpoint-specific mapping.
- Endpoint namespaces follow `AstroLab.Api.Features.{Feature}.{Leaf}`.
- Endpoints MUST remain thin and MUST NOT implement photometry, image scaling, spectral extraction, or other scientific algorithms.

A domain or infrastructure model MUST NOT be returned directly from an HTTP endpoint.

Every HTTP response MUST have its own API DTO record under `Features/`, constructed from the `Result<T>` value returned by Core/Infrastructure.

This isolates the HTTP wire contract from internal representation changes.

Shared boundary enums such as `StretchMode`, `ColorMap`, `DispersionAxis`, and `ArchiveSource` are permitted when they are plain API discriminators rather than domain models.

### Request Validation: GET vs. POST

A request DTO's private-constructor-plus-`Create(...)` pattern (§4.4) means the two HTTP binding paths reach validation differently.

- **GET/query-bound requests:** Query and route primitives are supplied to the handler, which constructs the validated request using `XxxRequest.Create(...)`.
- **POST/body-bound requests:** `System.Text.Json` constructs the DTO directly using the `[JsonConstructor]` exception described in §4.4. Where the request has invariants requiring explicit validation, the handler MUST call `request.Validate()` before using it.
- Invalid request-bound values MAY surface as `ArgumentException`/`ArgumentOutOfRangeException` and are mapped to HTTP 400 by `RequestValidationExceptionHandler`.
- Domain operations that fail after request validation MUST use `Result<T>` rather than throwing validation exceptions.

This keeps HTTP binding concerns at the API boundary while ensuring domain logic remains exception-free for expected failures.

### Roadmap Endpoint Rule

A feature scaffolded before its Core algorithm exists MUST return HTTP 501.

Its handler MUST call:

```csharp
AstroLab.Api.Features.NotImplementedResult.Value(code, message)
```

which returns `Results.Problem(..., statusCode: 501, title: code)`.

Roadmap endpoints MUST NOT return fake success values, hard-coded scientific results, or partial scientific implementations.

When the Core algorithm becomes available, replace the stub with the normal:

```text
Request
  → Infrastructure
  → Core
  → Result<T>
  → Response
```

flow.

Existing routing and DTOs SHOULD remain stable where the new implementation fits the existing contract.

### 6.6 Archive Clients: ESO and MAST

**Location:** `AstroLab.Infrastructure/Archives`

ESO and MAST clients are HTTP client abstractions over each archive's real documented query/download surfaces.

Each archive is split into two dedicated typed `HttpClient`s so the resilience policy sized for small metadata requests can never be accidentally applied to a large FITS transfer.

#### Archive API clients

Examples:

- `IEsoArchiveApiClient` / `EsoArchiveApiClient`
- `IMastArchiveApiClient` / `MastArchiveApiClient`

These clients handle:

- search
- metadata
- target resolution
- product discovery
- DataLink/product APIs

They SHOULD use `IHttpClientFactory` and appropriate resilience policies.

The resilience policy MUST be appropriate to metadata/query requests and MUST NOT be reused blindly for large file transfers.

#### Archive download clients

Examples:

- `IEsoArchiveDownloadClient` / `EsoArchiveDownloadClient`
- `IMastArchiveDownloadClient` / `MastArchiveDownloadClient`

These clients handle FITS file transfers.

They MUST:

- use streaming responses
- use `ResponseHeadersRead`
- propagate the caller's cancellation token
- avoid buffering the entire FITS file
- avoid automatic retries unless safe resumability is explicitly implemented
- avoid short fixed request timeouts that can terminate legitimate large transfers

`IEsoArchiveClient` / `EsoArchiveClient` and `IMastArchiveClient` / `MastArchiveClient` remain the application-facing abstractions.

Each is a thin orchestrator that delegates search/product discovery to the API client and downloads to the download client.

Each client MUST be designed so refinements to its request/response contracts can land without changing callers, Core, or API feature slices.

`SearchAsync` MUST honour every filter carried by `ArchiveSearchQuery` (`Target`, `Instrument`, `From`, `To`, `MaxResults`, and other supported filters) that the upstream archive's query surface supports.

The implementation MUST translate filters into the archive's native query shape rather than silently dropping them.

A coincidental 2xx response from an unrelated page on the real host MUST NOT be interpreted as a successful search with zero results. Response parsing MUST fail closed when the payload does not match the expected contract shape.

Archive-specific request/response payloads such as `EsoTapResponse` and `MastMashupRequest` are private wire-format DTOs, not domain models.

Map them into shared records such as `ArchiveObservation` and `ArchiveDownload` before returning from the client.

Optional metadata such as:

- collection
- data product type
- calibration level
- right ascension
- declination
- exposure time
- wavelength range
- proposal information
- data rights

MUST remain `null` when the archive does not provide the information. Do not invent values.

Both MJD-based archives' `t_min` fields MUST use the shared `ModifiedJulianDate` conversion rather than duplicating conversion logic.

#### MAST specifics

`IMastArchiveClient` MAY extend `IArchiveClient` with MAST-specific operations such as:

- `ResolveTargetAsync`
- `GetProductsAsync`
- `DownloadAsync(MastProduct, ct)`

These MUST remain off the shared interface where they are genuinely MAST-specific.

`SearchAsync` resolves `ArchiveSearchQuery.Target` to sky coordinates using MAST's name-resolution capability before running positional archive searches where appropriate.

It MUST NOT rely solely on textual target-name matching where that would produce unreliable results.

`DownloadAsync(string)` MUST NOT construct product URIs from assumptions about filenames or collection layouts.

It MUST discover the observation's actual products and select a suitable product using `MastProductSelectionPolicy`.

The product-selection policy SHOULD prefer an appropriate public, science-grade, calibrated FITS product over a raw or intermediate product where the archive exposes those distinctions.

The selected product's actual `DataUri` MUST be used for the download.

#### ESO specifics

An ESO ObsCore dataset identifier (`dp_id`) is not itself assumed to be a downloadable filename.

`DownloadAsync(string)` MUST use ESO's product/DataLink mechanism to discover the dataset's actual downloadable products.

It MUST NOT construct a FITS filename or URL from assumptions about `dp_id`.

`EsoProductSelectionPolicy` selects the most appropriate discovered product.

ESO tabular responses SHOULD resolve columns by name and handle:

- missing columns
- nulls
- JSON primitive/string numeric conversion
- optional fields

in one reusable mapping mechanism rather than duplicating column-index lookups.

ESO date filtering MUST use observation-overlap semantics:

```text
t_max >= From
t_min <= To
```

rather than assuming that `t_min` alone must fall inside the requested window.

If an archive's real query/download contract is genuinely not yet known for a capability, `SearchAsync`/`DownloadAsync` MUST return `Error.NotImplemented(...)` rather than sending requests to a guessed URL.

### 6.7 Visualisation as a Separate Capability

**Location:** `AstroLab.Infrastructure/ImageRendering`, `AstroLab.Api/Features/Images/Render`

Visualisation is an infrastructure/API concern, not a scientific one.

It MUST NOT be implemented inside `AstroLab.Core` when it refers to concrete output formats or codecs.

Core produces scientific data such as:

- scaled pixel values
- statistics
- source measurements
- WCS coordinates
- photometric measurements

Core MUST NOT know about:

- PNG
- JPEG
- image codecs
- HTTP image responses
- browser-specific formats

A concrete rendering dependency such as `PngRenderer` belongs in Infrastructure.

Conceptually:

```text
HTTP request
(RenderEndpoint)
       │
       ▼
AstroLab.Infrastructure/Fits
(FITS pixel read)
       │
       ▼
Native buffer / spans
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
HTTP response
(image/png)
```

The same separation applies to future:

- spectrum plots
- light curves
- source overlays
- RGB composites
- false-colour images

Core supplies scientific values. Infrastructure/API mapping turns those values into the requested visual or wire representation.

A Core algorithm MUST NOT know or care whether its output becomes a PNG, JSON response, FITS file, chart, or another representation.

### 6.8 Global Exception Handling

**Location:** `AstroLab.Api/RequestValidationExceptionHandler.cs`, `AstroLab.Api/GlobalExceptionHandler.cs`, `Program.cs`

`Result<T>` covers expected failures such as:

- validation failures arising from domain operations
- missing data
- unsupported capabilities
- archive failures that the caller can reasonably handle
- deliberately unimplemented capabilities

Request-boundary validation MAY use `ArgumentException` or `ArgumentOutOfRangeException` where required by the request DTO construction model.

`RequestValidationExceptionHandler`, registered ahead of `GlobalExceptionHandler`, catches those request-validation exceptions and maps them to HTTP 400.

Unexpected exceptions escaping an endpoint are caught by `GlobalExceptionHandler`, registered with `AddExceptionHandler<T>()` and `AddProblemDetails()`, and enabled with `app.UseExceptionHandler()`.

The global handler MUST:

- log the full exception server-side
- return a generic `ProblemDetails` response
- use HTTP 500 with title `unexpected_error`
- never expose stack traces or raw exception messages to callers

Global exception handling is a safety net, not a substitute for `Result<T>`.

A failure mode that can reasonably be anticipated MUST be represented explicitly with `Result<T>`.

---

## 7. Testing Standards

**Location:** `AstroLab.Tests`

Tests cover Core, Infrastructure, and API layers.

Use xUnit v3.

### 7.1 Core Unit Tests

**Location:** `AstroLab.Tests/Core/`

Tests MUST verify, at minimum:

- photometry calculations, including circular aperture flux and annular background estimation
- image scaling and expected normalised values, including logarithmic scaling
- spectrum extraction and expected one-dimensional output
- `Result<T>` success and failure behaviour
- expected domain failures without exception-based control flow
- FITS capability detection and capability mismatches
- WCS coordinate transformations where implemented
- source detection behaviour and edge cases where implemented

Tests SHOULD include:

- empty inputs
- NaN/infinite values where scientifically meaningful
- negative or zero values where algorithms permit them
- boundary conditions
- malformed or incomplete metadata
- representative scientific examples with known expected results

### 7.2 Allocation and Performance Tests

Performance/allocation tests SHOULD verify Core algorithms identified as performance-sensitive.

Tests SHOULD detect:

- unnecessary managed-array allocations
- hidden LINQ allocations where relevant
- boxed value types
- unnecessary intermediate collections
- unexpected per-element allocations
- significant regressions in execution time

Allocation is measured using `GC.GetAllocatedBytesForCurrentThread()` or a dedicated benchmarking/allocation framework where appropriate.

Tests MUST distinguish one-time setup and test-harness allocations from allocations performed by the algorithm under test.

The purpose of these tests is to protect genuinely performance-sensitive paths, not to enforce an arbitrary zero-allocation rule on every Core operation.

Where an algorithm naturally produces a result collection, the cost of producing that result is expected and SHOULD NOT be treated as an accidental allocation.

### 7.3 Infrastructure Tests

Infrastructure tests SHOULD verify:

- native buffer ownership and disposal
- double-disposal safety
- FITS header reading
- FITS pixel conversion
- malformed FITS handling
- archive response parsing
- archive product discovery
- product-selection policies
- streaming behaviour
- cancellation
- rendering correctness
- correct handling of missing archive metadata

Native-library-dependent tests SHOULD be isolated from pure Core tests.

### 7.4 API Tests

API integration tests SHOULD verify:

- request binding
- request validation
- expected HTTP status codes
- response DTO mapping
- Result-to-response mapping
- unsupported capability responses
- HTTP 501 roadmap endpoints
- global exception handling
- cancellation behaviour where practical
- end-to-end FITS workflows for representative datasets

API tests MUST NOT require external ESO or MAST services unless explicitly designated as integration/acceptance tests.

Tests against external archives SHOULD be separated from deterministic application tests and SHOULD NOT be required for every local build.

---

## 8. Appendix: Original Build Sequence (Historical)

AstroLab was originally scaffolded by an AI coding agent using the build order below. The solution described by this document already exists in the repository.

This appendix is **historical**. It is retained as a reference for extending the same architectural pattern to new capability areas; it is not an outstanding task list.

| Phase | What was built                                        | Governing specification |
| ----- | ----------------------------------------------------- | ----------------------- |
| 1     | Solution and project scaffolding                      | §5.1, §5.2              |
| 2     | Core `Result` / `Error` types                         | §6.1                    |
| 3     | Core FITS/domain models                               | §5.1                    |
| 4     | Core photometry, imaging, and spectroscopy algorithms | §6.2                    |
| 5     | Core unit and allocation tests                        | §7                      |
| 6     | FITS native bindings and unmanaged buffers            | §6.3                    |
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
6. FITS native bindings & unmanaged buffers
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

At each stage, the implementation compiled and its tests remained passing before proceeding to the next stage.

The same discipline applies to future work that extends this architectural pattern.
