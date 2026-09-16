# AstroLab — Project Specification

This is the authoritative design and engineering reference for AstroLab. It defines the architecture, engineering requirements, coding standards, domain rules, API conventions, and implementation patterns governing the repository.

`CLAUDE.md` provides Claude-specific working guidance, repository workflow, build/test commands, and local setup. **Do not duplicate those details here.**

### How to use this document

- `MUST` and `MUST NOT` requirements are hard constraints.
- A more specific rule overrides a general rule when an explicit exception exists.
- This document defines **what AstroLab must be**; `CLAUDE.md` defines **how Claude should work with it**.
- Before completing a change, review the resulting diff against the applicable requirements.

---

# 1. Overview

**AstroLab** is a high-performance .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and visualising FITS (Flexible Image Transport System) astronomical datasets from ESO and MAST archives as well as direct user uploads.

The system uses **Functional Core, Imperative Shell (FCIS)**:

- `AstroLab.Core` contains pure, deterministic scientific/domain logic.
- `AstroLab.Infrastructure` owns I/O, native interop, storage, archive integration, and concrete rendering.
- `AstroLab.Api` provides thin vertical API slices and HTTP contracts.

The API uses **Vertical Slice Architecture** and the **REPR (Request–Endpoint–Response)** pattern.

Expected domain and infrastructure outcomes use `Result<T>`. Exceptions are reserved for genuinely exceptional conditions and appropriate programmer misuse.

Large astronomical files require allocation-aware processing. `ReadOnlySpan<T>`, `NativeMemory`, unmanaged buffers, `System.IO.Pipelines`, and other performance techniques MAY be used where they provide a measurable benefit.

FITS image visualisation is a separate capability that converts 2D FITS image data into browser-consumable representations without mutating the original scientific data.

---

# 2. Technology and Constraints

- **Framework:** .NET 10
- **Language:** C# 14
- **API:** ASP.NET Core Minimal APIs
- **Architecture:** FCIS + Vertical Slice Architecture + REPR
- **Database:** None
- **Persistent dataset storage:** local filesystem
- **Native FITS dependency:** CFITSIO, isolated inside Infrastructure
- **Testing:** xUnit v3
- **API documentation:** ASP.NET Core OpenAPI/Swagger

The preferred solution structure is:

```text
AstroLab.slnx
├── src/
│   ├── AstroLab.Core/
│   ├── AstroLab.Infrastructure/
│   └── AstroLab.Api/
├── tests/
│   └── AstroLab.Tests/
└── storage/
```

The exact folder layout MAY evolve. A new project or major structural boundary SHOULD only be introduced when it represents a genuine separation of responsibility, deployment, dependency, or ownership.

No database or cloud object-storage dependency should be introduced merely to persist FITS datasets.

---

# 3. General Requirements

## 3.1 Production Quality

- **MUST:** Write production-ready, maintainable code.
- **MUST:** Prefer readability over cleverness.
- **MUST:** Keep methods focused on a single responsibility.
- **SHOULD:** Keep methods under 30 lines where practical. This is a refactoring signal, not an absolute limit.
- **MUST:** Avoid unnecessary duplication without over-abstracting.
- **MUST:** Avoid interfaces, base classes, generic abstractions, projects, or dependencies that exist only for hypothetical future requirements.
- **MUST:** Keep code testable through pure functions and constructor-injected dependencies rather than hidden state or ambient singletons.
- **SHOULD:** Prefer the simplest design satisfying the requirements.
- **SHOULD:** Avoid performance complexity or unsafe code without a concrete reason.

## 3.2 Validation and Invariants

- **MUST:** Validate external input before use, including HTTP input, uploaded files, and archive responses.
- **MUST:** Never allow invalid domain objects to enter an invalid state.
- **MUST:** Domain invariants belong to the domain type and MUST be enforced at its boundary.
- **MUST:** Use `Result<T>` for caller-handleable failures such as validation failures, missing data, unsupported capabilities, and expected infrastructure failures.
- **MUST NOT:** Use exceptions for expected domain validation, scientific calculation failures, invalid FITS data, or other normal control flow.
- **MAY:** Use `ArgumentException`, `ArgumentOutOfRangeException`, etc. for programmer misuse of a method contract.
- **MAY:** Request-boundary validation use exceptions when required by ASP.NET Core binding, provided they are translated into an appropriate HTTP response at the API boundary.
- **MUST:** Every `Error` contain a meaningful human-readable message.
- **SHOULD:** Provide stable machine-readable error codes rather than requiring clients to parse messages.

## 3.3 I/O and Cancellation

- **MUST:** Use `async`/`await` for disk, network, and pipeline I/O.
- **MUST:** Asynchronous operations that can meaningfully be cancelled accept and propagate a `CancellationToken`.
- **MUST NOT:** Block asynchronous code with `.Result` or `.Wait()`.
- **SHOULD:** Honour cancellation promptly, particularly during large downloads, FITS reads, and streaming.
- **MUST:** Suffix methods returning `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` with `Async`, including interface members.

## 3.4 Functional Core

- **MUST:** Scientific algorithms in `AstroLab.Core` be pure and deterministic with no I/O or side effects.
- **MUST:** Keep filesystem access, network communication, native interop, image encoding, and archive protocols outside Core.
- **SHOULD:** Keep Core independent of API and Infrastructure representations.

## 3.5 NuGet Packages

- **MUST:** Search nuget.org for the latest stable version before adding or modifying a package reference.
- **MUST NOT:** Rely on training-data package versions.
- **SHOULD:** Prefer the BCL or an existing dependency when it clearly provides the required functionality.
- **MUST:** Treat third-party dependencies as implementation details unless the dependency itself forms an intentional architectural boundary.

---

# 4. Coding Standards

## 4.1 Structure and Namespaces

- **MUST:** Use file-scoped namespaces.
- **MUST:** Namespace segments after the project root exactly mirror the file's folder path.

Example:

```text
src/AstroLab.Api/Features/Fits/Upload/FitsUploadResponse.cs
```

```csharp
namespace AstroLab.Api.Features.Fits.Upload;
```

- **MUST:** Keep one primary type per file.
- An explicit companion extension container MAY share a file where appropriate.
- A record's `Create(...)` factory belongs on the record itself.
- **MUST:** Use C# 14 `extension(...)` syntax for new extension members.
- **MUST NOT:** Use primary constructors on classes, structs, or records.

## 4.2 Method Ordering

Within a class:

- **MUST:** Place public methods before private methods.
- **MUST:** Order private methods according to their usage, with a method appearing before private methods it calls where practical.
- **MUST:** Keep internally used methods private.
- **MUST NOT:** expose methods speculatively.
- A private method MAY become public when a real requirement requires it.

## 4.3 Comments and Documentation

- **MUST NOT:** Add `//` comments merely explaining obvious code.
- **MUST NOT:** Add XML documentation to models, DTOs, records, or their properties.
- **MUST:** Add XML documentation to every endpoint class (`{Leaf}Endpoint.cs`) describing its purpose and behaviour.
- **MUST:** Add XML documentation to every class in `AstroLab.Core` and `AstroLab.Infrastructure`, describing its responsibility.
- Names should make data-only types self-documenting.

## 4.4 Literals, Nullability and Formatting

- **MUST:** Extract domain-significant numeric literals such as thresholds, scaling factors, coefficients, buffer sizes, and fallback values into named `private const` fields.
- Obvious literals such as `0`, `1`, and `2` used as simple indices/bounds are exempt.
- **MUST:** Enable nullable reference types with `<Nullable>enable</Nullable>`.
- **MUST NOT:** Use `!` to suppress nullable warnings when a real null check is required.
- **MUST NOT:** Add redundant mathematical parentheses that do not alter evaluation order or materially improve readability.
- **MUST NOT:** Add optional trailing commas.
- **MUST:** Use CRLF line endings as enforced by `.gitattributes`.
- **SHOULD:** Use blank lines to separate logical executable statements where they improve readability.
- **MUST NOT:** Add unnecessary blank lines immediately inside or before closing braces.

## 4.5 Control Flow and LINQ

- **SHOULD:** Prefer LINQ when it improves readability without a meaningful performance/allocation cost.
- **SHOULD:** Prefer explicit loops for numerical, pixel, buffer, and other performance-critical algorithms when they provide clearer control over memory, allocations, or complexity.
- **MUST NOT:** Avoid LINQ merely because it is historically considered slow.
- **SHOULD:** Benchmark genuinely performance-sensitive alternatives.
- **MUST:** Prefer early returns for guard conditions over unnecessary nesting.
- **SHOULD:** Prefer pattern matching where it improves clarity.
- **SHOULD:** Prefer switch expressions when a discriminant produces a value.
- **SHOULD:** Prefer `var` when the type is obvious from the right-hand side; use an explicit type when it improves clarity.

## 4.6 Immutability and Records

- **SHOULD:** Prefer immutable types.
- Options-pattern configuration classes MAY remain mutable because configuration binding requires settable properties.
- Resource-owning or behaviour-heavy types MAY remain classes.
- **MUST:** Use records for immutable data-only types such as DTOs, request/response models, value objects, and measurement results.
- Small value types MAY use `readonly record struct`.
- **MUST:** Concrete records default to `sealed`; unsealed records require an explicit documented inheritance requirement.
- `readonly record struct` MUST NOT be declared `sealed`.
- **MUST:** Record properties use explicit `{ get; }` accessors, never `{ get; init; }`.
- **MUST:** Record state is assigned only by its constructor.
- **MUST:** Normal records use a private constructor plus public static `Create(...)`.
- `Create(...)` validates its arguments inline and constructs the record.
- The private constructor performs no validation and is not called outside the record's own file.
- Records that are only constructed through `Create(...)` do not need a separate `Validate()`.
- When a framework can construct a record outside `Create(...)`, provide a public `Validate()` containing its invariants.
- `Create(...)` SHOULD call `Validate()` rather than duplicate those checks in such records.
- **MUST NOT:** Add an empty `Validate()` merely for symmetry.
- API-boundary records MAY use `ImmutableList<T>` for collection properties.
- Core hot-path types SHOULD use arrays/spans or other allocation-conscious representations.
- Established semantic constructors such as `Error.Validation(...)` and `Result<T>.Success(...)` MAY replace the generic `Create(...)` naming where appropriate.

### Request DTO exceptions

A request DTO bound directly from an HTTP body:

- MUST keep a private constructor marked `[JsonConstructor]`.
- MAY therefore be constructed directly by `System.Text.Json`.
- MUST expose `Validate()` when it has invariants.
- The endpoint MUST call `request.Validate()` after model binding where applicable.
- Hand-written construction SHOULD use `Create(...)`.

---

# 5. Architecture

The architectural dependency direction is:

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

## 5.1 Layer Responsibilities

### Core

`AstroLab.Core`:

- **MUST NOT** reference Infrastructure or ASP.NET Core.
- **MUST NOT** perform I/O, native interop, or access mutable global state.
- **MUST** contain pure deterministic scientific/domain logic, validation, models, algorithms, and result/error representations.
- MAY interpret already-loaded FITS metadata where no I/O is required.

### Infrastructure

`AstroLab.Infrastructure` owns:

- filesystem access
- native interop
- native memory
- network communication
- archive protocols
- catalogue communication
- FITS file access
- image encoding
- concrete rendering
- other external side effects

### API

`AstroLab.Api`:

- uses vertical slices
- coordinates Core and Infrastructure
- owns HTTP contracts
- **MUST NOT** implement scientific/domain calculations

### General architecture

- Expected failures **MUST** use `Result<T>`.
- Large FITS pixel buffers SHOULD remain outside the managed GC heap where practical.
- Large network/file payloads **MUST** be streamed rather than fully buffered into a single `byte[]`.
- Performance-critical Core algorithms SHOULD operate over spans or equivalent allocation-conscious representations.
- CFITSIO-specific types and APIs MUST remain behind the Infrastructure native adapter.
- Scientific analysis and visualisation MUST remain separate.
- PNG/image encoding and colour mapping MUST NOT be mixed into scientific computation.
- Core algorithms MUST NOT depend on whether their output becomes JSON, PNG, FITS, a chart, or another representation.

## 5.2 FITS Dataset Capabilities

FITS files MUST be treated as potentially **multi-capability datasets**, not as one mutually exclusive scientific type.

A dataset may contain:

```text
FITS
 │
 ├── Image data
 ├── Spectral data
 ├── Time-series/table data
 ├── WCS
 └── Other recognised capabilities
```

Capability detection MUST be deterministic and based on inspected FITS metadata.

Rules:

- `TIME` alone MUST NOT classify the entire file as a time series.
- The first pixel-containing HDU MUST NOT automatically be treated as the only relevant HDU.
- Spectral suitability SHOULD consider dimensionality and wavelength/frequency/energy/velocity metadata.
- Time-series suitability SHOULD consider appropriate time and measurement columns.
- Image suitability MUST be based on actual image data and dimensions.
- Table suitability MUST be based on structured table data.
- WCS availability SHOULD be detected independently.
- Multiple capabilities MAY coexist.

A legacy `FitsDatasetKind` MAY remain where a single primary kind is useful for compatibility, but it MUST NOT imply mutually exclusive scientific capabilities.

Analysis endpoints MUST validate the required capability before processing.

Example:

```text
Image Photometry       → ImageData
Astrometry             → ImageData + WCS
Spectral Extraction    → SpectralData
Time-Series Analysis   → TimeSeriesData
```

Capability detection belongs in Core.

The FITS reader MUST verify the required capability before loading associated data.

`FitsHeaderReader.ReadAllHeadersAsync` MUST return `fits.header.empty_file` when a staged file contains zero HDUs.

Malformed FITS metadata MUST NOT produce negative/nonsensical skip distances or buffer sizes. Header-derived numeric sizes MUST be validated and bounded before I/O.

### Scientific result requirements

Endpoints exposing measured, instrumental, or model-derived values MUST expose, where applicable:

- explicit physical units;
- propagated uncertainty when an existing Core algorithm can calculate it;
- the estimation method when the value is model-derived;
- quality indicators already produced by the underlying algorithm.

An endpoint MUST NOT fabricate an uncertainty or report a hard-coded zero uncertainty. If Core cannot statistically derive a valid uncertainty, the field SHOULD be omitted.

These requirements do not authorise inventing new statistics merely to populate a response; they require existing scientifically meaningful information to be surfaced rather than discarded.

## 5.3 Request Flow

Every endpoint follows:

```text
HTTP Request
     │
     ▼
API Endpoint
     │
     ▼
Infrastructure
(file I/O / HTTP / FITS adapter)
     │
     ▼
Core
(pure scientific/domain logic)
     │
     ▼
Result<T>
     │
     ▼
API Response DTO
     │
     ▼
HTTP Response
```

Endpoints MUST remain orchestration code.

The endpoint:

1. receives and validates HTTP input;
2. resolves required Infrastructure resources;
3. invokes Core;
4. maps `Result<T>` to the HTTP response.

## 5.4 Persistent Storage and Deployment

AstroLab stages FITS files on local disk rather than in a database or object store.

The application storage root is configurable through:

```text
Storage:RootPath
```

Docker uses:

```text
/app/storage
```

as the container-side storage root.

The recommended Docker deployment uses a **host bind mount**:

```yaml
volumes:
  - ./storage:/app/storage
```

The host directory is therefore the persistence boundary:

```text
Host
└── storage/
    └── observation.fits
            │
            │ bind mount
            ▼
Container
└── /app/storage/
    └── observation.fits
```

Requirements:

- FITS files MUST survive container deletion/recreation.
- The application MUST NOT contain Docker-specific storage logic.
- The application MUST NOT copy persistent FITS files into another container directory.
- FITS files MUST NOT be stored in a database merely for persistence.
- S3/object storage MUST NOT be introduced merely for persistence.
- The host `storage/` directory MUST NOT be committed to source control.
- Important staged data SHOULD be backed up independently of Docker.

Local non-Docker execution MAY use:

```text
Storage:RootPath=./storage
```

Docker MUST use `/app/storage`.

The Docker image:

- MUST be multi-stage;
- MUST use the .NET 10 ASP.NET runtime image;
- MUST run as a non-root application user;
- MUST expose port `8080`;
- MUST contain the required CFITSIO runtime library.

CFITSIO MUST be built from a pinned upstream source release with checksum verification in a dedicated build stage rather than relying on an arbitrary OS-package version or manually copied binary.

CFITSIO remains an Infrastructure implementation detail and the application MUST NOT depend on a particular native-library filesystem location.

---

# 6. Core Implementation Patterns

## 6.1 Result Pattern

`Result<TValue>` is a `readonly record struct` representing either success with a value or failure with an `Error`.

It exposes the existing composition operations:

- `Success`
- `Failure`
- `Match`
- `Bind`
- `Map`
- `MapError`
- `Ensure`
- `Deconstruct`

`Result<TValue>` has a private constructor.

`Success` and `Failure` are the supported semantic constructors.

`Error` is a lightweight `readonly record struct` containing:

- stable machine-readable code
- human-readable message
- `ErrorCategory`

Named constructors include:

- `Validation`
- `NotFound`
- `Conflict`
- `Unauthorized`
- `Infrastructure`
- `NotImplemented`
- `Cancelled`
- `Unexpected`

The `Error` constructor MUST reject empty codes/messages.

`NotImplemented` represents a deliberately unavailable capability and maps to HTTP 501.

Exceptions MUST NOT be used for normal domain validation, scientific failures, or invalid FITS data.

They MAY be used for genuinely exceptional Infrastructure/native/process failures and programmer misuse.

## 6.2 Functional Core and Allocation Awareness

Core algorithms SHOULD be static pure functions where practical.

Pure Core code:

- depends only on inputs;
- produces deterministic results;
- performs no I/O;
- modifies no external/hidden state;
- references no Infrastructure implementation.

Core MUST have:

- no disk access;
- no network access;
- no native interop;
- no filesystem dependency;
- no Infrastructure reference;
- no ASP.NET Core dependency;
- no archive/storage implementation dependency.

Core contains:

- domain/scientific models;
- value types;
- mathematical algorithms;
- validation;
- result/error types;
- pure FITS metadata interpretation.

Large-buffer algorithms MAY accept `ReadOnlySpan<T>`, `ReadOnlyMemory<T>`, arrays, or other appropriate representations.

Use spans where they provide meaningful benefits such as:

- avoiding copies;
- processing existing buffers;
- contiguous memory access;
- allocation-conscious hot paths.

`ref struct`, `stackalloc`, unsafe code, and similar techniques MUST NOT be introduced merely to satisfy a theoretical zero-allocation rule.

### Performance

Performance-sensitive algorithms SHOULD:

- avoid unnecessary heap allocations;
- avoid unnecessary intermediate collections;
- avoid repeated temporary arrays;
- avoid boxing;
- use spans or equivalent representations where appropriate;
- consider vectorisation only where measurement demonstrates a benefit;
- be benchmarked before introducing substantial complexity.

Natural result allocations are acceptable. For example, a source detector may allocate a collection of sources.

The requirement is to avoid **unnecessary** allocations, particularly inside hot loops and per-pixel processing.

## 6.3 Native Buffers and CFITSIO

**Location:** `AstroLab.Infrastructure/Fits`

Infrastructure owns native resources and their lifetime.

Large FITS buffers MAY use `NativeMemory` or an equivalent unmanaged allocator.

`UnmanagedFitsBuffer` MUST:

- allocate native memory through the selected allocator;
- expose it through spans where safe;
- deterministically release it through `IDisposable`;
- make ownership explicit;
- prevent double-free;
- avoid unnecessary copies into managed arrays.

CFITSIO handles and P/Invoke declarations MUST remain in Infrastructure.

The rest of the application MUST depend on AstroLab abstractions rather than CFITSIO APIs.

Replacing CFITSIO in future SHOULD require no Core/API changes.

### CFITSIO scope

FITS header parsing and image pixel decoding are implemented in managed C# and do not require CFITSIO.

CFITSIO is used for binary/ASCII table column reading where it avoids reimplementing complex FITS table semantics such as:

- `TSCAL`/`TZERO`;
- variable-length columns;
- other established FITS table behaviour.

`FitsFileHandle` owns the native `fitsfile*` returned by `ffopen` and MUST provide deterministic disposal and double-free protection.

`CfitsIoErrorMapper` MUST convert CFITSIO status/error information into `Result<T>`-compatible `Error` values. Native status codes and raw native error text MUST NOT reach API clients.

### Native integer marshaling

CFITSIO row/element-count parameters such as `ffgcvd`'s `firstrow`, `firstelem`, `nelem`, and `ffgpxv`'s `nelem` use fixed-width C `long long` and MUST be marshaled as C# `long`.

`CLong` is reserved for parameters representing the platform-dependent C `long`, such as appropriate axis-length/pixel-coordinate parameters.

This distinction MUST be preserved because incorrect Windows marshaling can silently corrupt native calls.

### Time-series table descriptors

Choosing which HDU/column to read is pure FITS-header interpretation and belongs in Core.

`TimeSeriesTableDescriptor.Resolve` MUST validate `TFORMn` and reject non-scalar columns with:

```text
fits.data.unsupported_column_shape
```

It MUST NOT silently interpret fixed-repeat, `P`, or `Q` variable-length columns as one scalar value per row.

Tests requiring the real CFITSIO library SHOULD be isolated from pure Core tests and MUST dynamically skip when the native library cannot be loaded.

## 6.4 Pipeline Streaming

**Location:** `AstroLab.Infrastructure/Storage` and `AstroLab.Infrastructure/Archives`

Archive data MUST be streamed directly to local staging storage.

The implementation MUST:

- stream network responses incrementally;
- avoid buffering complete FITS files into one `byte[]`;
- minimise intermediate allocations;
- respect backpressure where pipelines are used;
- correctly complete/dispose pipeline resources;
- propagate cancellation;
- avoid automatic retries of large downloads unless safe resumability is explicitly implemented.

`System.IO.Pipelines` MAY be used where appropriate.

## 6.5 Vertical Slice API / REPR

**Location:** `AstroLab.Api/Features`

API functionality is organised into self-contained vertical slices using Minimal APIs.

Each endpoint has:

- endpoint-specific request DTOs;
- endpoint-specific response DTOs;
- endpoint mapping;
- endpoint-specific result mapping.

Namespaces follow:

```text
AstroLab.Api.Features.{Feature}.{Leaf}
```

A feature such as `Images` is a route group. Each leaf such as `Render`, `Statistics`, `Photometry`, `Upload`, `Extract`, `Search`, or `Download` is a self-contained endpoint.

Endpoints MUST remain thin and MUST NOT contain scientific algorithms.

Domain and Infrastructure models MUST NOT be returned directly from HTTP endpoints.

Every HTTP response MUST have an API DTO record.

This isolates HTTP contracts from internal representations.

Shared boundary enums MAY be used when they are API discriminators rather than domain models.

### Request validation

GET/query-bound requests:

- route/query values are supplied to the handler;
- the handler constructs the validated request through `Create(...)`.

POST/body-bound requests:

- `System.Text.Json` constructs the DTO through the `[JsonConstructor]` exception;
- the handler MUST call `request.Validate()` where applicable.

Request-bound `ArgumentException`/`ArgumentOutOfRangeException` failures are mapped to HTTP 400 by `RequestValidationExceptionHandler`.

After request validation, domain failures MUST use `Result<T>` rather than validation exceptions.

## 6.6 ESO and MAST Archive Clients

**Location:** `AstroLab.Infrastructure/Archives`

ESO and MAST clients represent real documented archive APIs.

Each archive uses separate typed HTTP clients for metadata/query requests and large FITS downloads so query resilience policies cannot accidentally be applied to large transfers.

### Archive API clients

Examples:

```text
IEsoArchiveApiClient / EsoArchiveApiClient
IMastArchiveApiClient / MastArchiveApiClient
```

They handle:

- search;
- metadata;
- target resolution;
- product discovery;
- DataLink/product APIs.

They SHOULD use `IHttpClientFactory` and appropriate resilience policies.

Query policies MUST NOT be blindly reused for large downloads.

### Download clients

Examples:

```text
IEsoArchiveDownloadClient / EsoArchiveDownloadClient
IMastArchiveDownloadClient / MastArchiveDownloadClient
```

They MUST:

- stream responses;
- use `ResponseHeadersRead`;
- propagate cancellation;
- avoid buffering complete FITS files;
- avoid automatic retries unless safe resumability exists;
- use timeouts appropriate for legitimate large transfers.

`IEsoArchiveClient` / `EsoArchiveClient` and `IMastArchiveClient` / `MastArchiveClient` remain the application-facing abstractions and orchestrate their respective API/download clients.

`SearchAsync` MUST honour all supported filters from `ArchiveSearchQuery`, including:

- `Target`;
- `Instrument`;
- `From`;
- `To`;
- `MaxResults`;
- other supported archive filters.

Filters MUST be translated into the archive's native query model rather than silently dropped.

An unrelated 2xx response MUST NOT be interpreted as a successful empty search. Response parsing MUST fail closed when the payload does not match the expected contract.

Archive wire DTOs such as `EsoTapResponse` and `MastMashupRequest` MUST remain Infrastructure types.

Map them into shared records such as `ArchiveObservation` and `ArchiveDownload`.

Optional metadata MUST remain `null` when the archive does not provide it. Never invent values.

This includes:

- collection;
- data product type;
- calibration level;
- RA/Dec;
- exposure time;
- wavelength range;
- proposal information;
- data rights.

MJD-based archive filters MUST use the shared `ModifiedJulianDate` conversion.

### MAST

MAST-specific operations MAY extend the MAST abstraction, including:

- `ResolveTargetAsync`
- `GetProductsAsync`
- `DownloadAsync(MastProduct, ct)`

MAST-specific operations MUST NOT be forced onto the shared archive interface.

Target searches SHOULD resolve names to sky coordinates before positional searches where appropriate.

MAST searches MUST NOT rely solely on textual target-name matching when that is unreliable.

`DownloadAsync(string)` MUST discover the observation's actual products rather than constructing a URI from filename or collection assumptions.

`MastProductSelectionPolicy` selects the appropriate product.

Where the archive exposes distinctions, the policy SHOULD prefer an appropriate public, science-grade, calibrated FITS product over raw/intermediate data.

The selected product's actual `DataUri` MUST be downloaded.

### ESO

An ESO `dp_id` MUST NOT be assumed to be a downloadable filename.

`DownloadAsync(string)` MUST use ESO's product/DataLink mechanisms to discover actual downloadable products.

It MUST NOT construct FITS filenames or URLs from `dp_id`.

`EsoProductSelectionPolicy` selects the appropriate discovered product.

ESO tabular response mapping SHOULD resolve columns by name and handle:

- missing columns;
- nulls;
- JSON primitive/string numeric conversion;
- optional fields.

ESO date filtering uses observation-overlap semantics:

```text
t_max >= From
t_min <= To
```

If a real archive contract is genuinely unknown for a capability, return `Error.NotImplemented(...)` rather than sending requests to a guessed URL.

## 6.7 VizieR Catalogue Client

**Location:** `AstroLab.Infrastructure/Catalogues`

`ICatalogueClient` / `VizierTapClient` provides VizieR catalogue integration.

It supports:

- direct catalogue cone searches;
- image-source cross-matching.

The pure cross-match algorithm belongs in:

```text
AstroLab.Core.Catalogues.CatalogueCrossMatcher
```

A VizieR table is identified by its catalogue-native table name.

RA, Dec, identifier, and magnitude column names MUST NOT be hard-coded because they vary between catalogues.

`VizierTapClient` MUST discover these roles through the TAP service's `TAP_SCHEMA.columns`, using IVOA UCD1+ metadata:

- RA → `pos.eq.ra`
- Dec → `pos.eq.dec`
- identifier → `meta.id` / `meta.record`
- magnitude → `phot.mag`

When multiple candidates exist, prefer one additionally tagged `meta.main`.

Magnitude is optional. If no magnitude column exists, the query MUST select literal `NULL` and `CatalogueRecord.Magnitude` MUST be `null`.

Responses use IVOA VOTable XML.

`VoTableParser` MUST tolerate differing VOTable namespaces by matching elements by local name.

A DALI/TAP `QUERY_STATUS=ERROR` MUST be treated as an Infrastructure failure even if HTTP status is 200.

A response without a recognisable `TABLE` MUST fail closed rather than being treated as a successful empty result.

`CatalogueCrossMatcher.Match` is pure Core logic:

- for each detected source, find the nearest candidate across requested catalogues;
- use `AngularSeparation`;
- respect the requested matching radius;
- omit sources with no candidate within the radius.

## 6.8 Visualisation

**Location:** `AstroLab.Infrastructure/ImageRendering`, `AstroLab.Api/Features/Images`

Visualisation is an Infrastructure/API concern.

Core MAY produce:

- scaled pixel values;
- image statistics;
- source measurements;
- WCS coordinates;
- photometric measurements.

Core MUST NOT know about:

- PNG;
- JPEG;
- codecs;
- HTTP image responses;
- browser-specific representations.

Concrete rendering such as `PngRenderer` belongs in Infrastructure.

Conceptual flow:

```text
HTTP request
     │
     ▼
API endpoint
     │
     ▼
Infrastructure/Fits
     │
     ▼
Native buffer / spans
     │
     ▼
Core/Imaging
     │
     ├── ImageScaler
     ├── ImageStatistics
     └── ColorMapper
     │
     ▼
Infrastructure/ImageRendering
     │
     ├── FitsImageRenderer
     └── PngRenderer
     │
     ▼
HTTP image response
```

The same separation applies to spectrum plots, light curves, false-colour images, source overlays, and RGB composites.

Core algorithms MUST NOT care whether their output becomes an image, JSON, FITS file, chart, or another representation.

## 6.9 Global Exception Handling

**Location:**

```text
AstroLab.Api/RequestValidationExceptionHandler.cs
AstroLab.Api/GlobalExceptionHandler.cs
Program.cs
```

`Result<T>` handles expected failures such as:

- validation failures from domain operations;
- missing data;
- unsupported capabilities;
- expected archive failures;
- intentionally unavailable capabilities.

`RequestValidationExceptionHandler` handles request-boundary validation exceptions and maps them to HTTP 400.

It also handles `BadHttpRequestException` from Minimal API parameter binding.

`Program.cs` MUST explicitly configure:

```text
RouteHandlerOptions.ThrowOnBadRequest = true
```

so missing/invalid required parameters behave consistently across hosting environments.

`GlobalExceptionHandler` handles unexpected exceptions.

It MUST:

- log the full exception server-side;
- return generic `ProblemDetails`;
- use HTTP 500;
- use title `unexpected_error`;
- never expose stack traces;
- never expose raw exception messages.

Global exception handling is a safety net, not a replacement for `Result<T>`.

Any reasonably anticipated failure MUST have an explicit `Result<T>` representation.

---

# 7. Testing Standards

**Location:** `AstroLab.Tests`

Use xUnit v3.

Tests cover Core, Infrastructure, and API layers.

## 7.1 Core Tests

Core tests MUST cover, where implemented:

- photometry calculations, including circular aperture flux and annular background estimation;
- image scaling and expected normalised values, including logarithmic scaling;
- spectrum extraction and expected one-dimensional output;
- `Result<T>` success/failure behaviour;
- expected failures without exception-based control flow;
- FITS capability detection and capability mismatches;
- WCS transformations;
- source detection and relevant edge cases.

Tests SHOULD include:

- empty inputs;
- NaN/infinite values where scientifically meaningful;
- negative/zero values where algorithms permit them;
- boundaries;
- malformed/incomplete metadata;
- representative scientific examples with known expected results.

## 7.2 Allocation and Performance Tests

Performance/allocation tests SHOULD protect algorithms identified as genuinely performance-sensitive.

Where relevant, tests SHOULD detect:

- unnecessary managed-array allocations;
- hidden LINQ allocations;
- boxing;
- unnecessary intermediate collections;
- per-element allocations;
- significant execution-time regressions.

Allocation MAY be measured with:

```csharp
GC.GetAllocatedBytesForCurrentThread()
```

or an appropriate benchmarking/allocation framework.

Tests MUST distinguish test setup/harness allocations from allocations caused by the algorithm.

Do not enforce an arbitrary zero-allocation requirement on every Core operation.

Natural result allocations SHOULD NOT be treated as accidental allocations.

## 7.3 Infrastructure Tests

Infrastructure tests SHOULD cover:

- native buffer ownership;
- disposal and double-disposal;
- FITS header reading;
- FITS pixel conversion;
- malformed FITS handling;
- archive response parsing;
- archive product discovery;
- product-selection policies;
- streaming;
- cancellation;
- rendering correctness;
- missing archive metadata.

Native-library-dependent tests SHOULD be isolated from pure Core tests.

Tests requiring the real CFITSIO library SHOULD dynamically skip when the native library is unavailable rather than failing solely because a developer machine lacks the library.

## 7.4 API Tests

API integration tests SHOULD cover:

- request binding;
- request validation;
- expected HTTP status codes;
- response DTO mapping;
- `Result<T>` to HTTP mapping;
- unsupported capability responses;
- global exception handling;
- request-validation exception handling;
- cancellation where practical;
- representative end-to-end FITS workflows.

API tests MUST NOT depend on live ESO or MAST services unless explicitly designated as integration/acceptance tests.

External archive tests SHOULD be separated from deterministic application tests and MUST NOT be required for every local build.

---

# 8. Scientific and Engineering Invariants

The following principles apply across all scientific capabilities.

### Correctness over apparent sophistication

Algorithms MUST produce scientifically defensible results rather than impressive-looking approximations.

Do not fabricate:

- measurements;
- uncertainties;
- metadata;
- calibration values;
- confidence values;
- scientific classifications.

### Units

Measured and derived physical quantities MUST expose their units explicitly.

Unit conversions SHOULD be centralised rather than duplicated across endpoints.

### Uncertainty

Use propagated uncertainty when an existing Core algorithm can derive it.

Do not invent an uncertainty merely because an API response has an uncertainty field.

### Metadata and provenance

When relevant, preserve and surface available metadata such as:

- observation date/time;
- instrument;
- exposure time;
- gain;
- filter;
- RA/Dec;
- image dimensions;
- pixel scale;
- WCS;
- wavelength information.

Missing metadata MUST remain missing rather than being inferred without a documented basis.

### Data quality

Algorithms SHOULD account for:

- NaN/invalid pixels;
- saturation;
- background;
- noise;
- dynamic range;
- insufficient samples;
- malformed metadata;
- physically invalid inputs.

### Reproducibility

Core scientific calculations SHOULD be deterministic for identical inputs.

Algorithm parameters and methods used to derive scientific results SHOULD be represented explicitly where required for interpretation or reproducibility.

### Scientific vs presentation layers

Scientific values are produced by Core.

Presentation, encoding, visualisation, and HTTP representation belong outside Core.

---

# 9. Change Discipline

When extending AstroLab:

1. Preserve the FCIS boundary.
2. Put scientific/domain logic in Core.
3. Put I/O and external integrations in Infrastructure.
4. Keep API slices thin.
5. Reuse existing abstractions before introducing new ones.
6. Preserve existing public contracts unless the requirement explicitly changes them.
7. Add focused tests for changed behaviour.
8. Avoid unrelated refactoring.
9. Avoid speculative abstractions and dependencies.
10. Review the final implementation against this specification.

A change is incorrect if it works functionally but violates an applicable `MUST` or `MUST NOT` requirement.

The specification takes precedence over patterns found in older code when the older code conflicts with an explicit requirement here.
