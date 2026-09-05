---
name: scientific-algorithm
description: Use when implementing or reviewing an astronomy/scientific algorithm in AstroLab.Core, including photometry, astrometry, spectroscopy, image analysis, time-series analysis, or derived measurements.
---

# AstroLab Scientific Algorithm

## Purpose

Ensure scientific algorithms are implemented for scientific correctness first and software elegance second.

The implementation must be traceable to a defined mathematical/scientific procedure. Do not invent scientific behaviour to make an endpoint appear complete.

## Before Implementation

1. Identify the scientific quantity being calculated.
2. Define the mathematical procedure in plain language.
3. Identify all inputs and their units.
4. Identify coordinate, indexing, sign, and convention assumptions.
5. Identify valid input domains.
6. Identify invalid/undefined cases.
7. Identify numerical precision considerations.
8. Inspect existing AstroLab algorithms for conventions that must remain consistent.
9. Consult an authoritative scientific reference when the algorithm depends on an established standard, convention, or published method.

If the required scientific behaviour is underspecified, do not silently choose an arbitrary interpretation.

## Core Design

Scientific calculations belong in `AstroLab.Core`.

They should be:

- pure
- deterministic
- side-effect free
- independently testable
- independent of HTTP, storage, rendering, CFITSIO, and archive APIs

Keep the algorithm separate from data acquisition and output formatting.

## Numerical Correctness

Explicitly consider:

- NaN
- positive/negative infinity
- empty input
- insufficient samples
- zero/negative denominators
- overflow/underflow
- floating-point precision
- cancellation error where relevant
- units
- indexing conventions
- boundary inclusion/exclusion

Do not silently replace invalid scientific values with arbitrary defaults.

If invalid values are intentionally ignored, document the rule through clear naming and tests.

## Testing

Tests should include:

### Known cases

Use analytically predictable or independently verified inputs.

### Boundary cases

Test the edges of the valid domain.

### Invalid cases

Verify expected failures through `Result<T>` where appropriate.

### Numerical cases

Test representative floating-point values and tolerances.

### Invariants

Where appropriate, test properties that should remain true, such as:

- symmetry
- conservation
- monotonicity
- identity behaviour
- coordinate round trips

Do not use excessively loose tolerances simply to make tests pass.

## Scientific References

When a standard or published algorithm is required:

- identify the source
- use the source to establish the mathematical convention
- implement only the required scope
- test the convention explicitly

Do not cite a source as justification for behaviour that the source does not actually define.

## Performance

Optimise only after correctness is established.

For large arrays:

- avoid unnecessary copies
- operate over spans/arrays where appropriate
- keep hot loops allocation-conscious
- benchmark meaningful alternatives

Do not compromise scientific correctness for micro-optimisation.

## Completion Criteria

A scientific algorithm is not complete until:

- its mathematical behaviour is understood
- assumptions and units are clear
- valid/invalid domains are handled
- tests establish correctness
- expected failures are represented appropriately
- performance is reasonable for the expected data volume
- the implementation remains independent of I/O and presentation
