# `SPEC.md` — Project Specification: AstroLab FITS Platform

## 1. Executive Summary

**AstroLab** is a high-performance .NET 10 RESTful API platform designed to download, store, parse, analyze, visualise and render FITS (Flexible Image Transport System) scientific datasets from astronomical archives (ESO and MAST) as well as direct user uploads.

The system relies on a **Functional Core, Imperative Shell (FCIS)** design pattern, using **C# 14 Union Types** for functional error handling without exceptions, and native memory management (`cfitsio` P/Invoke bindings, `ReadOnlySpan<T>`, `System.IO.Pipelines`) to process multi-gigabyte astronomical files with minimal Garbage Collector (GC) overhead.

A dedicated **FITS Image Visualization** capability provides browser-consumable representations of 2D FITS image data, including pixel scaling, image stretching, colour mapping, NaN/invalid-pixel handling, image statistics, and interactive image navigation.

## 2. Core Constraints & Technical Requirements

- **Target Framework:** .NET 10 / C# 14
- **Database:** **None.** (No SQL or NoSQL database. Raw datasets and metadata staging are managed via local disk storage and file-system caches).
- **Architecture:** Functional Core, Imperative Shell (FCIS) + Vertical Slice Architecture inside the API layer.
- **Solution Layout:** Exactly 4 projects:
  1. `AstroLab.Api` (Host & Vertical Slice Endpoints)
  2. `AstroLab.Core` (Pure Domain & Mathematical Logic)
  3. `AstroLab.Infrastructure` (Native interop, HTTP archive clients, File I/O, image rendering)
  4. `AstroLab.Tests` (Unit and Integration test suites)
- **Error Handling:** C# 14 Native Union Types (`Result<T>`) for domain/infrastructure outcomes.
- **Performance Requirements:** High-performance focus. Zero heap allocation on raw pixel paths (`ReadOnlySpan<T>`, `NativeMemory`, `stackalloc`, `PipeReader`).
- **Image Visualization:** 2D FITS image data must be transformable into a browser-displayable image representation without modifying the original FITS data.

### General Requirements

These conventions apply across all four projects and are enforced regardless of feature area:

- **File-scoped namespaces.** Every `.cs` file declares its namespace with `namespace X.Y;`, never the block form (`namespace X.Y { ... }`).
- **Extension members over extension methods.** Static helper methods that logically extend a type must use the C# 14 `extension(...)` member syntax (grouped inside an `extension` block within the static class) rather than the classic `this`-parameter extension method form.
- **No primary constructors on classes or structs.** Classes and structs must declare an explicit constructor with a body (assigning to `private readonly` fields) rather than using C# 12 primary constructor syntax. This does **not** apply to positional records (`record`/`record struct` with a parameter list), which remain the standard pattern for DTOs and value types.
- **No line comments explaining code.** Do not use `//` comments to explain what code does. `///` XML documentation comments on public types/members are unaffected and remain expected wherever they aid API documentation.
- **One type per file.** Every class, record, struct, and enum — public or private, however small — is declared in its own file. A file may still contain multiple non-type declarations that belong to the same static class (e.g. an extension class with several `extension` blocks).
- **No `<LangVersion>` in `.csproj` files.** Do not pin or override the C# language version in any project file; the SDK's default (tied to the target framework) is always used.
- **CRLF line endings.** Every file in the repository uses CRLF line endings, enforced repo-wide by a `.gitattributes` rule (`* text eol=crlf`) rather than relying on each contributor's local `core.autocrlf` setting.

---

## 3. Target Solution Structure

```text
AstroLab.slnx
│
├── src/
│   ├── AstroLab.Core/                              # Pure Functional Core (Zero Dependencies)
│   │   ├── Fits/                                   # Domain models for HDUs and Headers
│   │   ├── Imaging/                                # Pure pixel scaling, stretching & visualization math
│   │   │   ├── ImageScaler.cs
│   │   │   ├── ImageStatistics.cs
│   │   │   └── ColorMapper.cs
│   │   ├── Photometry/                             # Pure aperture photometry algorithms
│   │   ├── Spectroscopy/                           # Pure wavelength & spectral algorithms
│   │   └── Result/                                 # C# 14 Native Union Result & Error types
│   │
│   ├── AstroLab.Infrastructure/                    # Imperative Shell (Side Effects & Native Interop)
│   │   ├── CFITSIO/                                # Low-level cfitsio P/Invoke & Native Buffers
│   │   ├── Storage/                                # Local disk staging via System.IO.Pipelines
│   │   ├── ESO/                                    # European Southern Observatory HTTP Client
│   │   ├── MAST/                                   # Mikulski Archive HTTP Client
│   │   └── ImageRendering/                         # FITS → browser image rendering
│   │       ├── FitsImageRenderer.cs
│   │       ├── PngRenderer.cs
│   │       └── RenderOptions.cs
│   │
│   ├── AstroLab.Api/                               # API Host & Vertical Slice Endpoints
│   │   ├── Features/                               # Vertical Slices (REPR Pattern)
│   │   │   ├── Observations/                       # Archive metadata search/query
│   │   │   ├── Fits/                               # File upload & header inspection
│   │   │   ├── Imaging/                            # FITS image visualization endpoints
│   │   │   ├── Photometry/                         # Aperture measurement endpoints
│   │   │   └── Spectroscopy/                       # Spectrum extraction endpoints
│   │   └── Program.cs                              # Web host & service registrations
│   │
│   └── AstroLab.Tests/                             # Comprehensive Test Suite
│       ├── Core/                                   # Pure domain algorithm unit tests
│       ├── Infrastructure/                         # CFITSIO native memory & rendering tests
│       └── Features/                               # Endpoint integration tests
│
└── storage/                                        # Local disk directory for raw FITS files (gitignored)
```

---

## 4. Key Architectural Implementations

### A. Core Union Result Pattern

**Location:** `AstroLab.Core/Result/Result.cs`

Use C# 14 native union types to model successful and failed operations without throwing exceptions for expected control-flow outcomes.

`Result<TValue>` must represent either:

- A successful result containing a `TValue`.
- A failure result containing an `Error`.

Exceptions must not be used for normal domain validation, calculation failures, invalid FITS data, or other expected failure conditions.

Exceptions may still be used at the imperative shell boundary for genuinely exceptional conditions such as unrecoverable infrastructure failures, native interop failures, or process-level failures where appropriate.

The `Result<TValue>` implementation must support C# 14 pattern matching so API endpoints can translate domain outcomes directly into HTTP responses.

---

### B. Functional Core Guidelines

**Location:** `AstroLab.Core`

`AstroLab.Core` is the functional core of the application and must remain completely isolated from infrastructure and external side effects.

#### Strict Purity

Every method inside `AstroLab.Core` must be implemented as a standard static pure function wherever practical.

Pure functions must:

- Depend only on their input parameters.
- Produce deterministic outputs for identical inputs.
- Avoid modifying external state.
- Avoid hidden global state.
- Avoid I/O and infrastructure dependencies.

#### No Side Effects

`AstroLab.Core` must have:

- **Zero disk access.**
- **Zero network access.**
- **Zero native interop calls.**
- **Zero filesystem dependencies.**
- **Zero references to `AstroLab.Infrastructure`.**
- **Zero dependencies on ASP.NET Core.**
- **Zero dependencies on archive clients or storage implementations.**

The Core project should contain only domain models, value types, mathematical algorithms, validation logic, and result/error representations.

#### Span-Based Performance

Processing functions operating on large pixel or byte buffers should accept:

```csharp
ReadOnlySpan<float>
```

or:

```csharp
ReadOnlySpan<byte>
```

where appropriate.

Algorithms must operate directly over contiguous memory spans rather than requiring callers to create intermediate managed arrays.

Hot-path algorithms should avoid allocations on the managed GC heap.

Where appropriate, APIs may also use:

- `Span<T>`
- `ReadOnlySpan<T>`
- `ReadOnlyMemory<T>`
- `stackalloc`
- `ref struct` types

provided that doing so does not compromise the purity or usability of the functional core.

---

### C. Imperative Shell & Unmanaged Native Buffers

**Location:** `AstroLab.Infrastructure`

`AstroLab.Infrastructure` contains all side effects, native interop, filesystem access, network communication, and resource management.

#### Unmanaged Memory Management

Wrap `cfitsio` raw pixel allocations using:

```csharp
System.Runtime.InteropServices.NativeMemory
```

and `IDisposable`-based wrappers.

Large FITS image buffers, potentially several gigabytes in size, must exist outside the managed GC heap.

The implementation must:

- Allocate native memory using `NativeMemory`.
- Expose the memory to Core algorithms through spans where safe and appropriate.
- Correctly release native allocations.
- Implement deterministic disposal through `IDisposable`.
- Prevent double-free operations.
- Ensure ownership of native memory is explicit.
- Avoid copying large pixel buffers into managed arrays.

`UnmanagedFitsBuffer` is responsible for owning the lifetime of native FITS pixel memory.

---

### D. Pipeline Streaming

Incoming archive data from ESO and MAST must be streamed directly to local storage without unnecessarily buffering the entire FITS file in managed memory.

Use:

```csharp
System.IO.Pipelines.PipeReader
System.IO.Pipelines.PipeWriter
```

to process streaming data.

The implementation should:

- Stream network responses incrementally.
- Write directly to local staging storage.
- Avoid loading complete FITS files into `byte[]`.
- Minimize intermediate buffer allocations.
- Respect backpressure.
- Correctly complete and dispose pipeline resources.
- Propagate cancellation tokens throughout the pipeline.

---

### E. Vertical Slice API Endpoints

**Location:** `AstroLab.Api/Features`

API functionality must be organized into self-contained vertical slices using ASP.NET Core Minimal APIs.

Each feature slice owns the endpoint-specific request handling, orchestration, validation, and response mapping required for that use case.

Each endpoint should follow this general flow:

1. **Receive request**
   - Receive route parameters, query parameters, request bodies, or uploaded files through ASP.NET Core Minimal APIs.

2. **Resolve infrastructure resources**
   - Interact with Infrastructure services to resolve file paths.
   - Open network streams.
   - Access local FITS files.
   - Allocate or open `UnmanagedFitsBuffer` instances.
   - Interact with ESO or MAST archive clients where required.

3. **Invoke functional core**
   - Pass the required data into pure algorithms from `AstroLab.Core`.
   - Keep all mathematical and scientific calculations inside the Core layer.
   - Avoid performing domain calculations directly inside API endpoints.

4. **Map the result**
   - Use C# 14 pattern matching against `Result<T>`.
   - Convert successful results into appropriate HTTP responses.
   - Convert known domain errors into appropriate HTTP status codes.
   - Avoid exception-based control flow for expected failures.

Example architectural flow:

```text
HTTP Request
     │
     ▼
AstroLab.Api Feature
     │
     ▼
Infrastructure
 ┌───┴─────────────────┐
 │ File / HTTP / CFITSIO│
 └───┬─────────────────┘
     │
     ▼
UnmanagedFitsBuffer / Span<T>
     │
     ▼
AstroLab.Core
     │
     ▼
Result<T>
     │
     ▼
API HTTP Response
```

---

## 5. Setup & Implementation Directives for Claude

When executing this specification, Claude must follow these implementation steps in strict sequence.

### 1. Scaffold Solution & Project Files

Create the following solution structure:

- Create solution `AstroLab.slnx`.
- Create `AstroLab.Core` class library targeting `.NET 10`.
- Create `AstroLab.Infrastructure` class library targeting `.NET 10`.
  - References `AstroLab.Core`.

- Create `AstroLab.Api` web project targeting `.NET 10`.
  - References `AstroLab.Core`.
  - References `AstroLab.Infrastructure`.

- Create `AstroLab.Tests` xUnit test project targeting `.NET 10`.
  - References `AstroLab.Core`.
  - References `AstroLab.Infrastructure`.
  - References `AstroLab.Api`.

The dependency direction must remain:

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

`AstroLab.Core` must never reference `AstroLab.Infrastructure` or `AstroLab.Api`.

---

### 2. Core Layer Implementation

**Location:** `AstroLab.Core`

Implement the functional core first. All functions should be static and pure.

#### Result Pattern

Create:

```text
AstroLab.Core/
└── Result/
    ├── Result.cs
    └── Error.cs
```

Implement:

- `Result<TValue>` using the C# 14 native union approach defined by this specification.
- `Error` as a lightweight value type.
- Success and failure construction.
- Pattern matching support.
- Appropriate error categorization.

#### Photometry

Create:

```text
AstroLab.Core/Photometry/ApertureEngine.cs
```

Implement pure aperture photometry algorithms including:

- Circular aperture flux integration.
- Annular aperture/background integration.
- Background estimation.
- Net source flux calculation.

Methods must operate on spans where appropriate and must not perform I/O or allocate large intermediate arrays.

#### Imaging

Create:

```text
AstroLab.Core/Imaging/
├── ImageScaler.cs
├── ImageStatistics.cs
└── ColorMapper.cs
```

Implement:

- Linear stretch.
- Logarithmic stretch.
- Square-root stretch.
- Asinh stretch.
- Pixel normalization.
- Black/white point handling.
- Invalid/NaN handling.
- Image statistics.
- Colour mapping.

All algorithms must be pure.

Methods should operate directly on `ReadOnlySpan<float>` or `ReadOnlySpan<byte>` where applicable.

#### Spectroscopy

Create:

```text
AstroLab.Core/Spectroscopy/SpectrumExtractor.cs
```

Implement pure one-dimensional spectral extraction algorithms.

The implementation must remain independent of FITS file access, native libraries, HTTP clients, and filesystem APIs.

Methods must operate on spans where appropriate and must not perform I/O or allocate large intermediate arrays.

---

### 3. Infrastructure Layer Implementation

**Location:** `AstroLab.Infrastructure`

Implement all imperative and external integrations.

#### CFITSIO Native Interop

Create:

```text
AstroLab.Infrastructure/CFITSIO/NativeMethods.cs
```

Implement P/Invoke declarations for the required `cfitsio` functions.

Native interop must remain completely outside `AstroLab.Core`.

#### Unmanaged FITS Buffer

Create:

```text
AstroLab.Infrastructure/CFITSIO/UnmanagedFitsBuffer.cs
```

Implement:

- Native allocation.
- Native deallocation.
- Ownership semantics.
- `IDisposable`.
- Safe access to native memory.
- Span-based access where appropriate.
- Protection against double disposal.
- Correct handling of allocation failures.

Large image buffers must not be copied into managed arrays unnecessarily.

#### Local File Store

Create:

```text
AstroLab.Infrastructure/Storage/LocalFileStore.cs
```

The implementation must use `System.IO.Pipelines` where appropriate for streaming FITS files to disk.

The store is responsible for:

- Creating staging paths.
- Writing FITS data.
- Opening stored files.
- Resolving local file paths.
- Handling cancellation.
- Managing file streams.

#### ESO Archive Client

Create:

```text
AstroLab.Infrastructure/ESO/EsoArchiveClient.cs
```

Implement the initial ESO integration as a stub HTTP client abstraction.

The client should be designed so that the HTTP implementation can later be expanded without changing Core algorithms. Make sure you implement retry functionality on the HTTP client.

#### MAST Archive Client

Create:

```text
AstroLab.Infrastructure/MAST/MastArchiveClient.cs
```

Implement the initial MAST integration as a stub HTTP client abstraction following the same architectural principles as the ESO client.

#### FITS Image Renderer

Create:

```text
AstroLab.Infrastructure/ImageRendering/
├── FitsImageRenderer.cs
├── PngRenderer.cs
└── RenderOptions.cs
```

Implement the FITS → browser image rendering pipeline.

The renderer must remain outside Core.

---

### 4. API Feature Endpoints

**Location:** `AstroLab.Api/Features`

Implement API endpoints using vertical slices and ASP.NET Core Minimal APIs.

Organize features as:

```text
Features/
├── Observations/
├── Fits/
├── Imaging/
├── Photometry/
└── Spectroscopy/
```

Each feature should contain the endpoint-specific request/response models and orchestration required for that feature.

Avoid creating a large centralized controller or service containing unrelated application functionality.

Configure `Program.cs` to:

- Register Infrastructure dependencies.
- Register archive clients.
- Register storage services.
- Register image rendering services.
- Configure application services.
- Map feature endpoints.
- Configure middleware.
- Configure dependency injection cleanly.

API endpoints should remain thin orchestration layers.

They must not contain the implementation of photometry, image scaling, spectral extraction, or other scientific algorithms.

---

### 5. Unit & Allocation Testing

**Location:** `AstroLab.Tests`

Write tests covering the Core, Infrastructure, and API layers.

#### Core Unit Tests

Create tests under:

```text
AstroLab.Tests/Core/
```

Verify:

- Photometry calculations are mathematically correct.
- Circular aperture calculations produce expected flux values.
- Annular background calculations produce expected results.
- Image scaling produces expected normalized values.
- Logarithmic scaling behaves correctly.
- Spectrum extraction produces expected one-dimensional output.
- `Result<T>` success and failure cases behave correctly.
- Expected domain failures do not require exceptions.

#### Allocation Tests

Add performance/allocation tests for hot-path Core algorithms.

Verify that algorithms operating on existing spans:

- Do not allocate managed arrays unnecessarily.
- Do not allocate on the managed heap where zero-allocation execution is required.
- Do not create hidden LINQ allocations.
- Do not box value types.
- Do not create unnecessary intermediate collections.

Use appropriate allocation measurement mechanisms such as:

```csharp
GC.GetAllocatedBytesForCurrentThread()
```

or a dedicated benchmarking/allocation testing framework where appropriate.

The allocation tests must distinguish between:

- One-time setup allocations.
- Test harness allocations.
- Actual allocations performed by the algorithm under test.

The requirement is specifically that the **hot data-processing path** performs zero managed heap allocations.

---

### 6. Architectural Enforcement

The implementation must preserve the following dependency rules:

```text
                    ┌───────────────────┐
                    │   AstroLab.Api    │
                    │ Vertical Slices   │
                    └─────────┬─────────┘
                              │
                 ┌────────────┴────────────┐
                 ▼                         ▼
      ┌─────────────────────┐   ┌─────────────────────┐
      │ AstroLab.Infrastructure│   │   AstroLab.Core    │
      │ Imperative Shell     │   │ Functional Core    │
      └──────────┬──────────┘   └─────────────────────┘
                 │                         ▲
                 └─────────────────────────┘
```

The following rules are mandatory:

1. `AstroLab.Core` must never reference Infrastructure.
2. `AstroLab.Core` must never reference ASP.NET Core.
3. `AstroLab.Core` must never perform I/O.
4. `AstroLab.Core` must never perform native interop.
5. `AstroLab.Core` must not depend on mutable global state.
6. Infrastructure owns all native memory.
7. Infrastructure owns all filesystem access.
8. Infrastructure owns all HTTP communication.
9. API features orchestrate Infrastructure and Core.
10. Scientific calculations belong in Core.
11. Expected failures are represented using `Result<T>`.
12. Exceptions must not be used as normal domain control flow.
13. Large FITS pixel buffers must remain outside the managed GC heap whenever possible.
14. Large network responses must be streamed rather than fully buffered.
15. Core hot paths should operate directly over spans without intermediate allocations.

---

### 7. Implementation Order

Claude must implement the project in the following order and should not skip ahead unless required to resolve a dependency:

```text
1. Solution and project scaffolding
          │
          ▼
2. Core Result/Error types
          │
          ▼
3. Core FITS/domain models
          │
          ▼
4. Core photometry algorithms
          │
          ▼
5. Core imaging algorithms
          │
          ▼
6. Core spectroscopy algorithms
          │
          ▼
7. Core unit and allocation tests
          │
          ▼
8. CFITSIO native bindings
          │
          ▼
9. UnmanagedFitsBuffer
          │
          ▼
10. LocalFileStore / Pipelines
          │
          ▼
11. ESO archive client
          │
          ▼
12. MAST archive client
          │
          ▼
13. API vertical slices
          │
          ▼
14. API integration tests
          │
          ▼
15. Full solution validation
```

At each stage, the implementation should compile and tests should remain passing before proceeding to the next stage.
