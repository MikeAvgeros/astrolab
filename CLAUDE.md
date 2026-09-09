# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`spec.md` is the authoritative project specification that defines the required architecture, engineering requirements, coding standards, domain rules, API contracts, and implementation constraints. **Do not duplicate the specification here.** Use this file to explain how Claude should approach work in the repository.

Project-specific skills are located under `.claude/skills/`. Use the relevant skill when its purpose applies to the current task. Do not invoke unrelated skills merely because they are available.

## How to use the specification

- Read the relevant section of `spec.md` before implementing a new feature or changing architecture.
- Identify which requirements in `spec.md` apply to the task.
- Treat `MUST` and `MUST NOT` requirements in `spec.md` as hard constraints.
- Prefer the most specific rule when the specification contains an explicit exception.
- Inspect the existing implementation before introducing a new pattern, abstraction, dependency, or project.
- Follow established repository patterns unless the task or specification requires a change.
- If the requested implementation conflicts with `spec.md`, follow the specification and explain the conflict.
- Before finishing, review the diff against the applicable parts of `spec.md`.

## Project context

AstroLab is a .NET 10 / C# 14 RESTful API for downloading, storing, parsing, analysing, and visualising FITS (Flexible Image Transport System) astronomical datasets from ESO/MAST archives and direct uploads.

The architecture uses:

- **Functional Core, Imperative Shell (FCIS)**
- **Vertical Slice Architecture** in the API
- **REPR (Request–Endpoint–Response)** endpoint structure
- `Result<T>` for expected failures

Think of the layers as:

- **Core** — pure and deterministic scientific/domain logic.
- **Infrastructure** — I/O, native interop, storage, archive integration, and concrete rendering.
- **API** — thin vertical slices that coordinate the other layers and expose HTTP contracts.
- **Tests** — verification of Core, Infrastructure, and API behaviour.

Use `spec.md` for the authoritative dependency rules and detailed architecture.

## Before implementing a task

1. **Understand the existing code.**
   - Locate the relevant feature, Core algorithm, Infrastructure service, tests, and registrations.
   - Read nearby implementations before designing something new.
   - Look for an existing abstraction or pattern that already solves most of the problem.
   - Do not change public API contracts unnecessarily.

2. **Identify the architectural path.**
   - Determine whether the change belongs in Core, Infrastructure, API, or more than one.
   - Put scientific/domain decisions in Core.
   - Keep external concerns at the Infrastructure boundary.
   - Keep endpoint handlers focused on orchestration and HTTP concerns.

3. **Check the FITS capability model.**
   - Do not assume a FITS file has one exclusive scientific type.
   - Determine which capability the operation actually requires.
   - Reuse existing capability detection and validation rather than adding endpoint-specific heuristics.

4. **Choose the simplest implementation that fits.**
   - Prefer existing types and patterns over new abstractions.
   - Avoid speculative interfaces, projects, namespaces, dependencies, or extension points.
   - Do not optimise prematurely.
   - For performance-sensitive work, measure before introducing complexity.

5. **Trace failure handling.**
   - Decide whether a failure is expected and caller-handleable or genuinely exceptional.
   - Follow the existing `Result<T>` and HTTP mapping path for expected failures.
   - Do not introduce a new exception-handling mechanism for a single feature.

6. **Consider data size and ownership.**
   - FITS files and pixel buffers can be very large.
   - Avoid whole-file buffering and unnecessary copies.
   - Make ownership and disposal explicit when working with native or unmanaged memory.
   - Keep Core algorithms independent of how their input data was obtained.

7. **Check dependencies before changing them.**
   - If a NuGet package is needed, verify the current stable version on nuget.org before modifying the project file.
   - Prefer the BCL or an already-used dependency when it provides the required functionality adequately.
   - Treat third-party libraries as implementation details unless they define an intentional architectural boundary.

## Implementing new scientific functionality

Use this general sequence:

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

Do not start by putting the calculation in an endpoint and plan to extract it later.

For an algorithm:

- First identify its inputs, outputs, invariants, and required FITS capability.
- Keep the algorithm deterministic and independently testable.
- Reuse existing Core value types and result/error patterns.
- For large numerical or pixel workloads, consider allocation behaviour as part of the design, but only introduce spans, unmanaged memory, vectorisation, `stackalloc`, unsafe code, or similar techniques when they have a concrete benefit.
- Keep rendering/wire-format concerns outside the scientific algorithm.

If the underlying scientific implementation does not exist yet, use the repository's existing roadmap/501 mechanism rather than inventing placeholder results.

## Implementing archive integrations

Treat archive APIs as external contracts, not as predictable URL schemes.

When working with ESO, MAST, or a future archive:

1. Inspect the existing archive client and its tests.
2. Keep archive-specific wire DTOs inside Infrastructure.
3. Map external responses into the application's shared models.
4. Discover real products through the archive's documented product/DataLink mechanisms.
5. Keep large downloads separate from metadata/query operations.
6. Preserve optional metadata when supplied and do not invent missing values.
7. Add deterministic tests for response mapping and product selection.
8. Do not guess an endpoint or download URL simply because a URL pattern appears plausible.

If the upstream contract for a capability is genuinely unknown, follow the specification's `NotImplemented` approach instead of sending an unverified request.

## Implementing API features

Use the existing vertical-slice structure under `AstroLab.Api/Features`.

For a new endpoint:

1. Find the closest existing slice and follow its structure.
2. Keep request/response DTOs at the API boundary.
3. Validate request-bound input using the established request pattern.
4. Resolve Infrastructure dependencies through the existing registration/dependency-injection approach.
5. Load or resolve external data in Infrastructure.
6. Invoke Core for scientific/domain work.
7. Map the resulting `Result<T>` to the endpoint's API response.
8. Add the appropriate API integration coverage.

Keep the endpoint readable enough that its role is obvious at a glance. If substantial scientific or data-processing logic starts accumulating in the handler, stop and move that responsibility to the appropriate layer.

## FITS, native code, and performance

CFITSIO is deliberately isolated behind Infrastructure.

When changing FITS/native code:

- Inspect the existing native adapter before adding another P/Invoke surface.
- Keep native handles, status codes, marshaling details, and ownership inside Infrastructure.
- Be particularly careful with native integer types and platform-dependent C types; follow the existing bindings rather than assuming C# type equivalence.
- Ensure native resources have deterministic ownership and disposal.
- Keep pure FITS metadata interpretation in Core where it does not require I/O.
- Add or update native-library-dependent tests without making ordinary developer builds depend on a locally installed native library.

For performance work:

- Identify the actual hot path first.
- Prefer straightforward code until measurement demonstrates a problem.
- Benchmark meaningful alternatives rather than relying on assumptions such as "LINQ is always slow" or "loops are always faster".
- Distinguish necessary result allocations from accidental intermediate allocations.

## Working with existing code

Before refactoring:

- Understand why the current code is structured the way it is.
- Preserve behaviour unless the task explicitly changes it.
- Avoid unrelated cleanup in the same change.
- Prefer a small, coherent diff over a broad "improvement".
- If an existing implementation violates `spec.md`, fix the violation when it is relevant to the task rather than copying the violation into new code.
- When several nearby implementations differ, identify the intended/current pattern from the specification and the most recent established implementation before choosing one.

When adding a method to an existing class, follow the method-ordering convention defined by `spec.md` rather than reorganising unrelated methods unnecessarily.

## Skills

Project-specific Claude skills live under `.claude/skills/`.

Before implementing a task:

- Check whether a skill clearly applies.
- Use the relevant skill when it provides task-specific instructions or workflow.
- Do not invoke unrelated skills merely because they exist.
- If a skill conflicts with `spec.md`, the specification wins.

## Keeping project guidance current

Update `CLAUDE.md` when a change affects **how Claude should work in the repository**, such as:

- build/test commands,
- development workflow,
- important repository-specific tooling,
- where to find project skills,
- a recurring implementation workflow that is not already obvious from `spec.md`.

Update `spec.md` when a change affects **what the system is required to be or how code must conform**, such as:

- architecture,
- dependency rules,
- coding standards,
- domain invariants,
- API contracts,
- implementation constraints.

If a change affects both, update both — but keep each piece of information in the file where it belongs. Do not mirror the same rule in both files.

## Testing workflow

Use the smallest useful feedback loop first:

```bash
dotnet test src/AstroLab.Tests --filter "FullyQualifiedName~<relevant test>"
```

Then run the broader suite:

```bash
dotnet test src/AstroLab.Tests
```

For normal validation:

```bash
dotnet build AstroLab.slnx
```

Run the API locally with:

```bash
dotnet run --project src/AstroLab.Api
```

Build/run the container when the change affects deployment or native dependencies:

```bash
docker build -t astrolab-api .
docker run -p 8080:8080 -v astrolab-storage:/app/storage astrolab-api
```

The test project uses xUnit v3 and Microsoft Testing Platform. Do not remove the repository's xUnit/MTP configuration merely to make tests behave like a conventional xUnit v2 test project.

When a change affects behaviour, prefer:

1. focused tests,
2. build,
3. full test suite,
4. additional integration/container validation when relevant.

Do not claim a task is complete merely because the changed project compiles.

## Final review

Before finishing a task:

- Check the diff for unintended changes.
- Re-read the relevant `spec.md` requirements.
- Confirm the implementation follows the existing architectural direction.
- Run the most relevant tests and the full suite where practical.
- Check for unnecessary allocations, abstractions, dependencies, and complexity.
- Confirm no placeholder/fake scientific behaviour was introduced.
- Update `spec.md` and/or `CLAUDE.md` only if the change genuinely affects the information those files own.
