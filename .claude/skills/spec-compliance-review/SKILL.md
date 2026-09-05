---
name: spec-compliance-review
description: Use when reviewing AstroLab changes for compliance with spec.md and CLAUDE.md. Audits architecture, coding standards, Result usage, FITS capabilities, performance, streaming, API boundaries, and tests.
---

# AstroLab Specification Compliance Review

## Purpose

Determine whether the current implementation or diff complies with the authoritative `spec.md` and the operational rules in `CLAUDE.md`.

This is a compliance audit, not a general refactoring exercise.

## Review Order

1. Read `CLAUDE.md`.
2. Identify the changed files.
3. Read the relevant sections of `spec.md`.
4. Inspect the complete affected implementations, not just the diff.
5. Compare implementation against explicit MUST/MUST NOT requirements.
6. Check established repository patterns.
7. Report findings by severity.

## Severity

Use:

- **FAIL** — violates an explicit MUST/MUST NOT requirement or creates an architectural violation.
- **WARN** — does not violate a hard requirement but is inconsistent with a SHOULD/preference or creates a meaningful maintainability/performance concern.
- **PASS** — compliant and appropriate.

Do not label stylistic disagreement as FAIL.

## Audit Areas

### Architecture

Check:

- Core does not reference Infrastructure or ASP.NET Core.
- Core contains no I/O, native interop, or mutable global state.
- Infrastructure owns external side effects.
- API endpoints orchestrate rather than implement scientific calculations.
- dependency direction is preserved.

### Core

Check:

- scientific algorithms are pure and deterministic
- expected failures use `Result<T>`
- no exceptions are used for normal domain control flow
- algorithms operate on appropriate representations
- unnecessary allocations are avoided on hot paths

Do not demand zero allocations for naturally allocated result objects unless the specification explicitly applies the zero-allocation requirement to that exact path.

### FITS

Check:

- FITS reasoning is based on HDUs and capabilities
- multiple capabilities can coexist
- a `TIME` column alone is not treated as proof that the whole dataset is a time series
- image/spectral/time-series analysis verifies the required capability
- unknown or irrelevant HDUs are not discarded merely because they do not match a preferred type

### API

Check:

- feature slices remain cohesive
- request/response DTOs are API-owned
- internal Core/Infrastructure models are not leaked
- endpoints remain thin
- HTTP behaviour follows established mappings

### Infrastructure

Check:

- archive-specific wire models remain in Infrastructure
- native/CFITSIO details remain in Infrastructure
- large files are streamed
- cancellation is propagated
- large downloads do not inherit inappropriate automatic retry behaviour

### Coding Standards

Check applicable rules including:

- file-scoped namespaces
- namespace/path alignment
- one primary type per file
- constructor/record conventions
- method visibility and ordering
- private helper ordering
- pattern matching preferences
- early returns
- LINQ usage appropriate to workload
- comment/documentation rules
- numeric constants
- async naming and cancellation

### Tests

Check that changed behaviour has appropriate tests and that performance/allocation tests exist where genuinely required.

## Output

Use this structure:

```text
## Compliance Review

### FAIL
- [file:line] Finding
  - Requirement:
  - Why it violates the requirement:
  - Recommended correction:

### WARN
- [file:line] Finding
  - Concern:
  - Recommendation:

### PASS
- Area — brief confirmation

### Overall
PASS / PASS WITH WARNINGS / FAIL
```

Do not invent line numbers. If a line cannot be established, cite the file and relevant symbol instead.

Do not propose unrelated refactors.
