---
name: feature-development
description: Use when implementing or extending an AstroLab feature, especially scientific analysis, FITS processing, archive integration, or API vertical slices. Guides Claude through investigation, Core-first implementation, testing, integration, and final compliance review.
---

# AstroLab Feature Development

## Purpose

Implement features in a way that preserves the architecture and conventions defined by `spec.md` and operationalised by `CLAUDE.md`.

This is a workflow skill. It does not replace either document.

## Before Editing

1. Read `CLAUDE.md`.
2. Read only the relevant sections of `spec.md`; do not assume the specification from memory.
3. Inspect the existing implementation and tests for the closest analogous feature.
4. Trace the current data/request flow through Core, Infrastructure, and API.
5. Identify the smallest set of files that should change.
6. Identify whether the requested behaviour already exists in another form.
7. Do not create speculative abstractions, projects, interfaces, namespaces, or endpoints.

If the existing code conflicts with the specification, identify the conflict before deciding whether it is in scope to correct.

## Design Order

For a scientific capability, work from the inside out:

1. Define or refine the scientific/domain concept in `AstroLab.Core`.
2. Implement pure deterministic logic in Core.
3. Add Core tests for correctness and expected failures.
4. Add Infrastructure support for I/O, native interop, archive access, or rendering where required.
5. Add the API vertical slice last.
6. Add API integration tests.

Do not put scientific calculations in an API endpoint simply because it is convenient.

## Existing Patterns

Before introducing a new pattern, find an existing implementation that performs a similar job and follow its established conventions.

Examples to inspect when relevant:

- existing `Result<T>` operations
- existing Core algorithms
- existing FITS parsing/capability detection
- existing archive clients
- existing streaming code
- existing API vertical slices
- existing response mapping
- existing test fixtures

Prefer consistency with the repository over a theoretically cleaner pattern that would create a second convention.

## API Features

For an API feature:

1. Define request/response DTOs in the feature slice.
2. Keep endpoints thin.
3. Validate request-bound input at the API boundary.
4. Resolve Infrastructure dependencies.
5. Load or obtain required data.
6. Call Core algorithms.
7. Map `Result<T>` to the established HTTP response mechanism.
8. Never expose Core or Infrastructure models directly.

## Failure Handling

Use `Result<T>` for expected failures.

Do not use exceptions for normal domain/application control flow.

Follow the existing `Error` categories and HTTP mapping instead of inventing feature-specific mechanisms unless the specification requires one.

## FITS Features

Do not assume a FITS file has one mutually exclusive scientific type.

Reason about:

- individual HDUs
- available data
- recognised metadata
- WCS
- image capability
- spectral capability
- time-series capability
- other relevant capabilities

A keyword or column by itself does not necessarily establish a dataset-wide scientific interpretation.

## Performance

For large numerical datasets:

- avoid unnecessary intermediate allocations
- prefer spans/arrays when appropriate
- keep hot loops allocation-conscious
- do not introduce unsafe code, pooling, stack allocation, SIMD, or complex abstractions without a measurable reason
- benchmark genuinely performance-sensitive alternatives

Correctness and maintainability come before premature optimisation.

## Testing

Add tests at the layer where the behaviour belongs.

Core tests should cover:

- normal scientific cases
- boundaries
- invalid inputs
- expected `Result<T>` failures
- numerical edge cases such as NaN/infinity where applicable

Infrastructure tests should cover:

- parsing
- mapping
- I/O
- native resource ownership
- streaming
- cancellation

API tests should cover:

- request binding/validation
- HTTP status codes
- response mapping
- representative end-to-end behaviour

Add allocation/performance tests when the changed algorithm is genuinely performance-sensitive.

## Validation Before Completion

After implementation:

1. Format/check the changed code using repository conventions.
2. Run the smallest relevant tests.
3. Run the full test suite where practical.
4. Build the solution.
5. Inspect the final diff.
6. Check the diff against the applicable `spec.md` MUST requirements.
7. Check for accidental public API changes.
8. Check for unnecessary files, dependencies, abstractions, and comments.
9. Update `spec.md` and `CLAUDE.md` if the implementation changes documented architecture or conventions.

Do not declare the task complete merely because the code compiles.
