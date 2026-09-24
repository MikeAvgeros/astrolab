# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

`spec.md` is the **authoritative project specification** for architecture, engineering requirements, coding standards, domain rules, API contracts, and implementation constraints. **Do not duplicate it here.** This file describes how Claude should approach work in the repository.

Project-specific skills are under `.claude/skills/`. Use a skill when it clearly applies to the task; do not invoke unrelated skills. If a skill conflicts with `spec.md`, `spec.md` wins.

## Specification and repository rules

- Read the relevant `spec.md` sections before implementing a feature or changing architecture.
- Treat `MUST` and `MUST NOT` requirements as hard constraints.
- Prefer the most specific rule when an explicit exception exists.
- Inspect existing implementations and follow established repository patterns before introducing new abstractions, dependencies, or projects.
- Preserve existing public API contracts unless the task requires a change.
- If the requested implementation conflicts with `spec.md`, follow the specification and explain the conflict.
- Review the final diff against the applicable specification requirements.

Keep information in the correct file:

- **`spec.md`** — what the system must be and how code must conform.
- **`CLAUDE.md`** — how Claude should work in the repository.

Update `CLAUDE.md` only when a change affects Claude's workflow, tooling, or repository-specific working guidance. Update `spec.md` when a change affects architecture, coding standards, domain rules, API contracts, or implementation requirements. If both are affected, update both without duplicating the same rule.

## Project context

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and visualising FITS astronomical datasets from ESO/MAST archives and direct uploads.

Architecture:

- **Functional Core, Imperative Shell (FCIS)**
- **Vertical Slice Architecture** in the API
- **REPR (Request–Endpoint–Response)** endpoint structure
- `Result<T>` for expected failures

Layers:

- **Core** — pure, deterministic scientific/domain logic.
- **Infrastructure** — I/O, native interop, storage, archive integration, and concrete rendering.
- **API** — thin vertical slices coordinating layers and exposing HTTP contracts.
- **Tests** — verification of Core, Infrastructure, and API behaviour.

Use `spec.md` for detailed dependency and architectural rules.

## Before implementing a task

1. **Understand the existing code**
   - Locate the relevant feature, Core algorithm, Infrastructure service, tests, and registrations.
   - Read nearby implementations and identify existing abstractions/patterns before designing something new.
   - Avoid unrelated changes.

2. **Place the responsibility correctly**
   - Scientific/domain decisions belong in Core.
   - External I/O belongs in Infrastructure.
   - Endpoint handlers should coordinate layers and HTTP concerns.
   - Keep API DTOs at the API boundary.

3. **Check the FITS capability model**
   - A FITS file can provide multiple scientific capabilities; do not treat it as one exclusive type.
   - Determine the capability actually required by the operation.
   - Reuse existing capability detection and validation rather than adding endpoint-specific heuristics.

4. **Prefer the simplest suitable implementation**
   - Reuse existing types and patterns.
   - Avoid speculative abstractions, interfaces, projects, dependencies, and extension points.
   - Do not optimise prematurely; measure before introducing performance complexity.

5. **Trace failure handling**
   - Distinguish expected, caller-handleable failures from exceptional failures.
   - Use the existing `Result<T>` and HTTP mapping path.
   - Do not introduce a new exception-handling mechanism for one feature.

6. **Consider data size and ownership**
   - FITS files and pixel buffers can be large.
   - Avoid whole-file buffering and unnecessary copies.
   - Make native/unmanaged ownership and disposal explicit.
   - Keep Core algorithms independent of how their input data was obtained.

7. **Check dependencies**
   - For a required NuGet package, verify the current stable version on nuget.org.
   - Prefer the BCL or an existing dependency when it adequately provides the required functionality.
   - Treat third-party libraries as implementation details unless they define an intentional architectural boundary.

## Scientific functionality

Use this implementation path:

```text
Required scientific capability
        ↓
Core model / algorithm
        ↓
Focused Core tests
        ↓
Infrastructure data access, if required
        ↓
API vertical slice
        ↓
API integration tests
```

Do not put scientific calculations in an endpoint with the intention of extracting them later.

For algorithms:

- Identify inputs, outputs, invariants, and required FITS capability first.
- Keep algorithms deterministic and independently testable.
- Reuse existing Core value types and `Result<T>`/error patterns.
- For large numerical workloads, consider allocation behaviour, but only introduce spans, unmanaged memory, vectorisation, `stackalloc`, unsafe code, or similar techniques when there is a concrete measured benefit.
- Keep rendering and wire-format concerns outside scientific algorithms.
- If the scientific implementation does not exist, use the repository's existing `NotImplemented`/501 mechanism rather than inventing placeholder results.

## Archive integrations

Treat archive APIs as external contracts, not predictable URL schemes.

For ESO, MAST, or future archives:

1. Inspect the existing archive client and tests.
2. Keep archive-specific wire DTOs in Infrastructure.
3. Map external responses into shared application models.
4. Discover real products through documented product/DataLink mechanisms.
5. Keep large downloads separate from metadata/query operations.
6. Preserve optional metadata when supplied; do not invent missing values.
7. Add deterministic tests for response mapping and product selection.
8. Never guess an endpoint or download URL from a plausible URL pattern.

If an upstream contract for a capability is genuinely unknown, use the specification's `NotImplemented` approach instead of sending an unverified request.

## API features

Use the existing vertical-slice structure under `AstroLab.Api/Features`.

For a new endpoint:

1. Find the closest existing slice and follow its structure.
2. Keep request/response DTOs at the API boundary.
3. Use the established request validation pattern.
4. Resolve Infrastructure dependencies through the existing DI/registration approach.
5. Load or resolve external data in Infrastructure.
6. Invoke Core for scientific/domain work.
7. Map `Result<T>` to the API response.
8. Add appropriate API integration coverage.

If substantial scientific or data-processing logic starts accumulating in a handler, move it to the appropriate layer.

## FITS, native code, and performance

CFITSIO is isolated behind Infrastructure.

When changing FITS/native code:

- Inspect the existing native adapter before adding another P/Invoke surface.
- Keep native handles, status codes, marshaling, and ownership inside Infrastructure.
- Follow existing bindings for native integer types and platform-dependent C types.
- Ensure native resources have deterministic ownership and disposal.
- Keep pure FITS metadata interpretation in Core when it does not require I/O.
- Keep ordinary developer builds independent of a locally installed native library; isolate native-library-dependent tests appropriately.

For performance work:

- Identify the actual hot path first.
- Benchmark meaningful alternatives rather than relying on assumptions such as "LINQ is always slow".
- Distinguish necessary result allocations from accidental intermediate allocations.
- Prefer straightforward code until measurement demonstrates a problem.

## Refactoring and existing code

Before refactoring:

- Understand why the current implementation is structured as it is.
- Preserve behaviour unless the task explicitly changes it.
- Prefer a small, coherent diff.
- Do not include unrelated cleanup.
- If existing code violates `spec.md`, correct the violation when relevant rather than copying it into new code.
- When nearby implementations differ, use `spec.md` and the most recent established pattern to determine the intended approach.
- Follow the method-ordering convention in `spec.md`; do not reorganise unrelated methods unnecessarily.

## Testing and validation

Use the smallest useful feedback loop first:

```bash
dotnet test src/AstroLab.Tests --filter "FullyQualifiedName~<relevant test>"
```

Then:

```bash
dotnet build AstroLab.slnx
dotnet test src/AstroLab.Tests
```

Run the API locally with:

```bash
dotnet run --project src/AstroLab.Api
```

The React SPA in `web/` consumes the API through its OpenAPI document, via a Vite dev proxy locally or nginx in Docker (`web/Dockerfile`, `web/nginx.conf.template`). When changing it, run `npm run build` in `web/` (type-check and bundle). When changing API contracts, remember that its generated endpoint forms depend on accurate OpenAPI metadata. `docker compose up -d --build` builds and starts both the API (`:8080`) and the web UI (`:3000`).

When deployment or native dependencies are affected, also validate the container:

```bash
docker compose up -d
```

Equivalently, without Compose:

```bash
docker build -t astrolab-api .
docker run -p 8080:8080 -v ./storage:/app/storage astrolab-api
```

The test project uses xUnit v3 and Microsoft Testing Platform. Preserve the repository's existing xUnit/MTP configuration.

For behavioural changes, prefer:

1. focused tests,
2. build,
3. full test suite,
4. additional integration/container validation when relevant.

Do not consider compilation alone sufficient validation.

## Final review

Before finishing:

- Inspect the diff for unintended changes.
- Re-read the relevant `spec.md` requirements.
- Confirm the implementation follows the existing architecture and patterns.
- Run the appropriate tests and broader validation where practical.
- Check for unnecessary allocations, abstractions, dependencies, and complexity.
- Confirm no placeholder or fake scientific behaviour was introduced.
- Update `spec.md` and/or `CLAUDE.md` only when the change genuinely affects information owned by that file.
