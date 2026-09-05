# Refactoring Skill

## Purpose

Use this skill when improving the structure, readability, maintainability, or design of existing AstroLab code without intentionally changing its externally observable behaviour.

Refactoring is not an excuse to redesign the system, introduce speculative abstractions, or combine unrelated changes. The goal is to make the existing code better while preserving its behaviour and architectural constraints.

## Required workflow

### 1. Establish the baseline

Before changing code:

1. Read `CLAUDE.md`.
2. Read the relevant sections of `spec.md`.
3. Inspect the target implementation completely enough to understand its responsibilities.
4. Inspect directly related interfaces, models, callers, tests, and analogous implementations.
5. Identify the current behaviour that must be preserved.
6. Run the most relevant existing tests when practical.

Do not speculate about code that has not been inspected.

### 2. Define the refactoring boundary

State internally what the refactoring is intended to improve, for example:

- duplicated logic
- unclear responsibilities
- excessive method complexity
- poor naming
- inappropriate coupling
- unnecessary abstraction
- awkward control flow
- violation of established project structure
- obsolete implementation following a specification change

Keep the change focused on that boundary.

Do not mix unrelated cleanup, formatting, dependency upgrades, feature work, or architectural redesign unless they are necessary to complete the refactoring safely.

### 3. Preserve behaviour

Unless the task explicitly requests a behavioural change:

- Preserve public API behaviour.
- Preserve domain semantics.
- Preserve error semantics and expected `Result` failures.
- Preserve cancellation behaviour.
- Preserve disposal and resource ownership.
- Preserve numerical/scientific behaviour and conventions.
- Preserve FITS interpretation and capability semantics.
- Preserve streaming behaviour for large payloads.
- Preserve performance characteristics where they are intentional.

If the existing behaviour appears incorrect, distinguish the bug fix from the refactoring rather than silently changing behaviour.

### 4. Prefer the smallest sound design

Choose the simplest refactoring that materially improves the code.

Prefer:

- clearer names
- smaller cohesive methods
- appropriate method visibility
- removing duplication
- simplifying control flow
- extracting genuinely cohesive responsibilities
- reducing unnecessary coupling
- existing project patterns over new abstractions
- pattern matching where it improves clarity
- immutable data where it fits existing design
- composition over speculative frameworks or abstraction layers

Do not create an abstraction merely because two pieces of code look superficially similar. Abstract only when the shared concept is real and the abstraction improves the design.

Do not add interfaces, factories, services, helper classes, generic frameworks, or extension methods solely to make code appear more architecturally sophisticated.

### 5. Respect AstroLab architecture

Refactoring MUST continue to follow the architecture defined by `spec.md` and `CLAUDE.md`.

In particular:

- Keep scientific/domain logic in `AstroLab.Core`.
- Keep I/O, native interop, filesystem, HTTP, and other infrastructure concerns out of Core.
- Keep API concerns out of Core.
- Preserve the Functional Core / Imperative Shell boundary.
- Keep vertical-slice ownership clear in the API.
- Keep FITS capability detection and analysis capability-oriented rather than forcing unrelated HDUs into one dataset classification.
- Keep DTOs/contracts separate from domain models.
- Do not move code between projects merely for cosmetic reasons; move it when responsibility or dependency direction requires it.

### 6. Be careful with scientific and numerical code

For numerical or scientific refactoring:

- Establish what the existing code calculates before changing its structure.
- Preserve units, coordinate conventions, indexing conventions, tolerances, and edge-case behaviour.
- Do not replace a clear numerical loop with LINQ simply for style.
- Do not optimise or change numerical algorithms unless optimisation or algorithmic change is part of the request.
- Compare before/after results for representative and boundary inputs when the refactoring touches scientific calculations.

Correctness takes precedence over stylistic elegance.

### 7. Be careful with FITS code

When refactoring FITS functionality:

- Treat a FITS file as a dataset containing potentially different HDUs.
- Do not reintroduce a single mutually exclusive dataset classification where multiple capabilities can coexist.
- A `TIME` column alone MUST NOT be treated as proof that the entire FITS dataset is a time series.
- Preserve per-HDU reasoning and capability detection.
- Preserve WCS interpretation and coordinate conventions.
- Preserve native-memory ownership and disposal boundaries.
- Keep cfitsio/native concerns in Infrastructure.

### 8. Consider performance, but measure before changing it

Refactoring can accidentally introduce allocations, copying, unnecessary enumeration, or expensive conversions.

For performance-sensitive code:

1. Identify whether the code is actually on a hot path.
2. Prefer a clear implementation unless there is evidence that performance matters.
3. Avoid introducing LINQ, intermediate collections, boxing, repeated conversions, or unnecessary array copies into numerical/pixel hot paths.
4. Do not introduce `unsafe`, native buffers, pooling, spans, or other complexity solely as a theoretical optimisation.
5. If performance is a stated concern, measure before and after and record the relevant result.

Do not sacrifice maintainability for an unmeasured optimisation.

### 9. Refactor tests with the production code

Tests should be refactored when necessary to reflect the new structure, but tests must continue to verify behaviour rather than implementation details.

Prefer tests that establish:

- public behaviour
- domain invariants
- expected `Result` failures
- scientific correctness
- FITS capability detection
- resource ownership where relevant
- API contracts

Avoid rewriting tests merely to match changed private implementation details.

Do not weaken or delete tests simply because they make a refactoring inconvenient.

### 10. Validate incrementally

After each meaningful refactoring step:

- build the affected project
- run focused tests
- then run the broader relevant test suite

For larger changes, inspect the diff regularly rather than waiting until the end.

The final implementation should leave the repository buildable and tests passing unless a pre-existing failure or explicitly requested change prevents that.

### 11. Review the final diff

Before finishing, check:

- Did the refactoring achieve its stated purpose?
- Is behaviour preserved?
- Is the new design simpler than the old one?
- Did any new abstraction earn its place?
- Are responsibilities clearer?
- Is visibility no broader than necessary?
- Did dependency direction remain correct?
- Did the change accidentally alter error, cancellation, disposal, streaming, FITS, or scientific behaviour?
- Did it introduce unnecessary allocations or complexity?
- Are there unrelated changes that should be removed?
- Does the result comply with `CLAUDE.md` and `spec.md`?

## Anti-patterns

MUST NOT:

- Rewrite large areas of code without first understanding the existing implementation.
- Refactor unrelated files merely because they are nearby.
- Perform broad style cleanups during a focused refactoring.
- Introduce abstractions speculatively.
- Turn simple code into a framework of helpers and interfaces without a concrete benefit.
- Change public behaviour under the guise of refactoring.
- Change scientific calculations without explicitly treating that as a behavioural change.
- Replace performance-sensitive numerical code with less predictable allocation-heavy code for stylistic reasons.
- Delete tests because they constrain the desired refactoring.
- Assume that a more abstract design is automatically a better design.
- Claim a refactoring is complete without checking the final diff and relevant tests.

## Output expectations

When completing a refactoring task, briefly report:

1. **What changed** — the structural improvements made.
2. **Why** — the problem the refactoring addressed.
3. **Behaviour preserved** — any important behaviour deliberately kept unchanged.
4. **Validation** — builds/tests run and their outcome.
5. **Remaining concerns** — only if something relevant could not be verified.

Keep the final report concise. The code and tests are the primary deliverable.
