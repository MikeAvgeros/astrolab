# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Keep this file current:** whenever you make a change to this project that affects the project structure, feature slices, architecture, dependency rules, commands, or development workflow, update the relevant section of this file in the same change.

The authoritative engineering specification is `spec.md`. This file contains the operational guidance and the rules most relevant to an AI coding agent.

---

## Project Overview

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and visualising FITS (Flexible Image Transport System) astronomical datasets from ESO/MAST archives and direct uploads.

The architecture uses:

- **Functional Core, Imperative Shell (FCIS)**
- **Vertical Slice Architecture** in the API
- **REPR (Request–Endpoint–Response)** endpoint structure
- `Result<T>` for expected failures
- native/unmanaged memory where appropriate for large FITS data
- streaming I/O for large files

See `spec.md` for the complete architectural and coding specification.

There is currently no database. Metadata and raw datasets are staged on local disk under `storage/`, which is gitignored and configurable through `Storage:RootPath`.

---

## Working Rules

### Before Making Changes

1. Read the relevant section of `spec.md` before implementing a new feature or changing architecture.
2. Inspect the existing implementation before introducing new abstractions or patterns.
3. Prefer extending an existing pattern over introducing a competing pattern.
4. Keep changes focused on the requested feature.
5. Do not introduce speculative abstractions, projects, dependencies, or Core namespaces for functionality that has not been implemented.
6. Do not change public API contracts unnecessarily.
7. After making changes, build and run the relevant tests.
8. Check the final diff against both `spec.md` and this file.

### Package Dependencies

- Always check nuget.org for the latest stable version before adding or changing a NuGet package reference.
- Never rely on remembered package versions.
- Avoid adding a package when the BCL or an existing dependency provides the required functionality adequately.
- Do not introduce a dependency solely for convenience when it creates an unnecessary architectural coupling.

---

## Commands

```bash
dotnet build AstroLab.slnx
dotnet test src/AstroLab.Tests

dotnet test src/AstroLab.Tests --filter "FullyQualifiedName~ApertureEngineTests"
dotnet test src/AstroLab.Tests --filter "DisplayName~<test name>"

dotnet run --project src/AstroLab.Api

docker build -t astrolab-api .
docker run -p 8080:8080 -v astrolab-storage:/app/storage astrolab-api
```

Tests use xUnit v3 (`xunit.v3` package). `AstroLab.Tests` builds as an executable (`OutputType=Exe`, required by xUnit v3's Microsoft.Testing.Platform model) and `dotnet test` runs it via the native MTP mode enabled by the root `global.json`'s `test.runner` setting — do not remove that setting or revert `OutputType` to `Library`.

`Microsoft.AspNetCore.Mvc.Testing` and `ApiFactory.cs` are used for in-process API integration tests against `Program`.

When changing behaviour, run the smallest relevant test set first, followed by the complete test suite before considering the work complete.

---

# Architecture

AstroLab uses Functional Core, Imperative Shell combined with Vertical Slice Architecture.

The dependency direction is:

```text
AstroLab.Api
      │
      ├──────────────► AstroLab.Infrastructure
      │                       │
      │                       ▼
      └──────────────────► AstroLab.Core

AstroLab.Tests ─────────► all production projects
```

### Non-negotiable dependency rules

- `AstroLab.Core` MUST NOT reference `AstroLab.Infrastructure`.
- `AstroLab.Core` MUST NOT reference ASP.NET Core.
- `AstroLab.Core` MUST NOT perform disk, network, filesystem, or native I/O.
- `AstroLab.Core` MUST NOT contain hidden mutable/global state.
- Infrastructure owns external side effects.
- API endpoints orchestrate Infrastructure and Core.
- Scientific/domain calculations MUST NOT be implemented in API feature endpoints.
- API DTOs MUST NOT expose Infrastructure or Core models directly.

When implementing a new capability:

```text
Scientific/domain logic
        ↓
AstroLab.Core
        ↓
Infrastructure integration
        ↓
API vertical slice
```

Do not implement the feature backwards by putting domain logic in the API and later attempting to extract it into Core.

---

# Coding Standards

The complete coding standards are in `spec.md` §4. The following are the rules most likely to affect implementation.

## Structure

- Use file-scoped namespaces.
- Namespace segments MUST match the directory structure.
- One primary type per file.
- Use C# 14 `extension(...)` syntax for new extension members.
- Do not add `<LangVersion>` to a `.csproj`.
- Use CRLF line endings.
- Do not add trailing commas after the final member of an enum.

## Constructors and Records

- Do not use primary constructors on classes, structs, or records.
- Use explicit constructors.
- Records use the private-constructor + static `Create(...)` pattern defined in `spec.md`.
- Record properties use `{ get; }`, not `{ get; init; }`.
- Concrete record types are sealed by default.
- `Create(...)` performs validation; constructors do not.
- Use `Validate()` where a framework can construct a record without going through `Create(...)`, such as request DTO model binding.
- `ImmutableList<T>` is required for collection-shaped API-boundary record properties.
- Core hot-path representations are exempt and should use arrays/spans or other appropriate representations.

Do not introduce an alternative record-construction convention without updating `spec.md`.

## Comments

- Do not add `//` comments merely to explain obvious code.
- Do not add XML documentation to models, DTOs, records, or their properties.
- Add XML documentation to endpoint classes.
- Add XML documentation to Core and Infrastructure classes.
- Comments are appropriate when they explain non-obvious reasoning, scientific assumptions, external protocol behaviour, safety constraints, or deliberately unusual implementation decisions.

## Control Flow

- Prefer early returns over unnecessary nesting.
- Prefer pattern matching when it makes branching clearer.
- Prefer switch expressions when producing a value from a discriminant.
- Prefer `var` when the type is obvious from the right-hand side.
- Async methods returning `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` use the `Async` suffix.

## LINQ

LINQ is **not prohibited** in performance-sensitive code simply because it is LINQ.

Prefer LINQ when it makes collection-oriented code clearer and does not introduce a meaningful performance or allocation cost.

Prefer explicit `for`/`foreach` loops when working on:

- per-pixel operations
- numerical algorithms
- large contiguous buffers
- tight hot loops
- code requiring precise control over memory access or allocation behaviour

Do not assume loops are automatically faster or LINQ is automatically slower. Benchmark genuinely performance-critical alternatives.

## Magic Numbers

Numeric literals that encode domain meaning MUST be extracted into named `private const` fields.

Examples include:

- scientific thresholds
- scaling factors
- algorithm coefficients
- buffer sizes
- default fallbacks

Self-evident values such as `0`, `1`, and collection indexes are exempt where their meaning is obvious.

---

# Result<T> and Error Handling

Expected failures use `Result<T>`.

Use `Result<T>` for:

- validation failures
- missing data
- unsupported capabilities
- invalid FITS data
- expected archive failures
- expected calculation failures
- explicitly unimplemented capabilities

Do **not** throw exceptions for expected domain/application failures.

Exceptions MAY be used for:

- programmer misuse of an API
- genuinely unexpected infrastructure failures
- unrecoverable native/interop failures
- other genuinely exceptional conditions

Request-boundary validation MAY use `ArgumentException`/`ArgumentOutOfRangeException` where required by the ASP.NET Core binding model. These are converted to HTTP 400 at the API boundary.

Unexpected exceptions are handled by `GlobalExceptionHandler`.

Never expose raw exception messages or stack traces to API clients.

When adding an expected failure:

1. Add an appropriate `Error` category/code.
2. Return `Result<T>`.
3. Map it through the existing result-to-HTTP mechanism.
4. Do not add a scenario-specific global exception handler.

---

# Functional Core

`AstroLab.Core` is the pure scientific/domain core.

It contains:

- FITS domain models
- FITS metadata interpretation
- scientific algorithms
- mathematical operations
- validation
- value types
- `Result<T>` / `Error`

It must not contain:

- filesystem access
- HTTP
- ASP.NET Core
- native P/Invoke
- CFITSIO types
- PNG/JPEG encoding
- JSON/API response shaping
- archive-specific HTTP implementation

## Allocation Awareness

Core algorithms should be allocation-conscious, especially when processing large pixel or numerical datasets.

Do not impose a blanket "zero allocations everywhere" rule.

Instead:

- avoid unnecessary allocations
- avoid unnecessary intermediate arrays
- avoid unnecessary materialisation
- avoid boxing
- avoid repeated temporary objects inside hot loops
- use spans when they provide a meaningful benefit
- benchmark performance-critical algorithms

Natural result allocations are acceptable.

For example, an algorithm returning a collection of detected sources is expected to allocate the result collection.

Do not introduce `ref struct`, `stackalloc`, unsafe code, or other complexity solely to satisfy an arbitrary allocation target.

---

# FITS Capabilities

A FITS file should **not** be treated as necessarily belonging to one mutually exclusive scientific type.

A file may contain multiple HDUs and support multiple capabilities.

Think in terms of:

```text
FITS Dataset
     │
     ├── Image data
     ├── Spectral data
     ├── Time-series/table data
     ├── WCS
     └── Other recognised capabilities
```

Capability detection belongs in Core.

Do not use simplistic assumptions such as:

- any `TIME` column means the entire dataset is a time series
- the first pixel HDU is automatically the scientifically relevant image
- one FITS file can have only one useful scientific interpretation

Analysis endpoints should verify that the required capability exists before performing analysis.

For example:

```text
Image Photometry     → Image capability
Astrometry           → Image + WCS
Spectral Extraction  → Spectral capability
Time-Series Analysis → Time-Series capability
```

If a primary `FitsDatasetKind` enum remains useful to the existing API, it may be retained, but it must not prevent the system from representing multiple capabilities.

---

# Infrastructure

`AstroLab.Infrastructure` owns side effects and implementation details.

It contains areas such as:

```text
Infrastructure/
├── Fits/
├── Storage/
├── Archives/
└── ImageRendering/
```

## FITS and CFITSIO

CFITSIO is an **implementation detail**.

- Keep P/Invoke declarations inside Infrastructure.
- Keep CFITSIO-specific handles/types out of Core.
- Do not make API contracts depend on CFITSIO.
- Native buffer ownership belongs to Infrastructure.
- Core should operate on appropriate managed/span-based representations without knowing how the data was obtained.

If CFITSIO is replaced in the future, Core and API code should require minimal or no changes.

## Native Memory

Large FITS pixel buffers MAY use unmanaged memory.

`UnmanagedFitsBuffer` is responsible for:

- ownership
- allocation
- disposal
- preventing double-free
- exposing data safely to Core

Do not copy multi-gigabyte FITS datasets wholesale into managed arrays.

---

# Streaming

Large FITS files MUST be streamed.

Use `System.IO.Pipelines` where appropriate.

Network/file operations should:

- stream incrementally
- avoid whole-file `byte[]` buffering
- propagate cancellation
- respect backpressure
- dispose resources deterministically

Large downloads should not use generic automatic retries unless resumability/safe retry semantics have explicitly been implemented.

---

# API Vertical Slices

API functionality lives under:

```text
AstroLab.Api/Features/
```

Each leaf feature represents a self-contained endpoint.

The general structure is:

```text
Features/
└── <Feature>/
    └── <Leaf>/
        ├── <Leaf>Endpoint.cs
        ├── <Request>.cs
        └── <Response>.cs
```

Endpoints follow REPR:

```text
Request
   ↓
Endpoint
   ↓
Infrastructure
   ↓
Core
   ↓
Result<T>
   ↓
Response
```

Endpoints must remain thin.

They should:

- receive/bind input
- validate request-bound input
- resolve Infrastructure dependencies
- load required data
- call Core algorithms
- map `Result<T>` to HTTP responses

They must not contain scientific calculations.

## API DTOs

Every API response gets its own API DTO.

Do not return:

- FITS domain models
- archive infrastructure models
- Core measurement models
- Infrastructure implementation types

directly from an HTTP endpoint.

Map internal models to API response records.

Shared enums such as `StretchMode`, `ColorMap`, `DispersionAxis`, and `ArchiveSource` may cross the API boundary where they are simple discriminators rather than domain models.

---

# Roadmap / HTTP 501

Scaffolded roadmap endpoints may return HTTP 501 until their underlying implementation exists.

They MUST NOT:

- return fake scientific results
- return hard-coded measurements
- pretend an operation succeeded
- partially implement an algorithm in the endpoint

Use the existing `NotImplementedResult` mechanism.

When the actual implementation lands, replace the stub with:

```text
Request
   ↓
Infrastructure
   ↓
Core
   ↓
Result<T>
   ↓
Response
```

Do not create Core namespaces solely to support an endpoint that has no real implementation yet.

---

# Archive Integrations

ESO and MAST use separate HTTP clients for metadata/query operations and large FITS downloads.

Conceptually:

```text
Archive API Client
    ├── search
    ├── metadata
    ├── target resolution
    └── product discovery

Archive Download Client
    └── streamed FITS download
```

Metadata/query clients may use standard HTTP resilience policies.

Download clients must be configured for large streaming transfers and must not inherit inappropriate short-lived request policies.

## Important Rules

- Do not guess archive URLs.
- Do not construct product download URLs from filenames unless the archive contract explicitly guarantees that convention.
- Discover actual downloadable products through the archive's product/DataLink APIs.
- Preserve optional metadata when available.
- Do not invent missing metadata.
- Map archive-specific wire DTOs into shared application models.
- Archive-specific protocols remain inside Infrastructure.

For ESO specifically, do not assume an ObsCore `dp_id` is itself a downloadable URI.

For MAST specifically, resolve targets and discover actual products rather than relying on guessed collection/file paths.

---

# Visualisation

Scientific computation and rendering are separate concerns.

Core may calculate:

- pixel statistics
- scaling
- stretching
- colour-map values
- photometric measurements
- WCS coordinates

Infrastructure owns:

- PNG encoding
- image codecs
- concrete rendering implementations
- browser/output representations

Core must not depend on PNG, JPEG, HTTP image responses, or browser-specific formats.

The general flow is:

```text
FITS data
   ↓
Infrastructure FITS reader
   ↓
Core scientific/image calculations
   ↓
Infrastructure renderer
   ↓
PNG / other output
```

---

# Testing

Use xUnit v3.

Tests are organised around:

```text
AstroLab.Tests/
├── Core/
├── Infrastructure/
└── Features/
```

## Core Tests

Test:

- scientific correctness
- boundary conditions
- invalid input
- NaN/infinite handling where applicable
- expected `Result<T>` failures
- FITS capability detection
- WCS calculations where implemented
- image/science algorithms

## Performance Tests

Performance tests are for genuinely performance-sensitive algorithms.

Measure:

- managed allocations
- execution time
- unnecessary intermediate collections
- boxing
- repeated temporary allocations

Use `GC.GetAllocatedBytesForCurrentThread()` or an appropriate benchmarking framework.

Do not classify a natural result allocation as a performance regression merely because the algorithm returns a collection.

## Infrastructure Tests

Test:

- FITS parsing
- native buffer ownership
- disposal
- malformed FITS handling
- archive response mapping
- product discovery
- product-selection policies
- streaming
- cancellation
- image rendering

## API Tests

Test:

- request binding
- request validation
- HTTP status codes
- response mapping
- `Result<T>` mapping
- global exception handling (`GlobalExceptionHandler`, `RequestValidationExceptionHandler`)
- representative end-to-end FITS workflows

Scaffolded HTTP 501 roadmap endpoints do not get dedicated tests — a stub returning `NotImplementedResult` is not yet a real code path, and testing it only pins down a value that changes the moment the real implementation lands. Cover a roadmap endpoint once its Request → Infrastructure → Core → `Result<T>` → Response flow is actually implemented.

External ESO/MAST calls should not be required for normal deterministic application tests.

---

# Adding a New Scientific Feature

When adding a new scientific capability:

1. Determine the required FITS capability.
2. Define the scientific/domain model in Core if needed.
3. Implement the pure algorithm in Core.
4. Add focused Core tests.
5. Add allocation/performance tests if the algorithm is performance-sensitive.
6. Add or extend the Infrastructure capability needed to supply the data.
7. Add the API vertical slice.
8. Define API-specific request/response DTOs.
9. Map `Result<T>` through the existing API result mechanism.
10. Add API integration tests.
11. Update `spec.md` and this file if the architecture, project structure, commands, or development conventions changed.

Do not put scientific logic in the API endpoint merely because it is convenient.

---

# Adding a New Archive

When adding a new astronomical archive:

1. Define the archive-specific API client.
2. Define a separate download client if large file transfers are involved.
3. Keep archive-specific request/response DTOs inside Infrastructure.
4. Map archive responses to shared application models.
5. Implement product discovery using the archive's real API.
6. Do not guess download URLs.
7. Add deterministic tests around response parsing and product selection.
8. Add the API slice only after the underlying capability exists.

---

# Before Finishing Any Task

The implementation is not complete until:

- the code follows `spec.md`
- dependency direction is preserved
- Core remains free of Infrastructure/ASP.NET dependencies
- scientific logic remains in Core
- expected failures use `Result<T>`
- API contracts remain isolated from internal models
- large files remain streamed
- unnecessary allocations are avoided in hot paths
- LINQ is used or avoided based on clarity and measured performance, not outdated assumptions
- new dependencies have been checked against current stable NuGet versions
- relevant tests pass
- the full test suite passes where practical
- no fake scientific implementation has been introduced
- `spec.md` and `CLAUDE.md` are updated if the change affects their documented rules

When uncertain between two implementations, prefer the simpler design that satisfies the specification and existing architectural boundaries.
