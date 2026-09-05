---
name: fits-analysis
description: Use when inspecting, parsing, classifying, analysing, or extending FITS handling in AstroLab.
---

# AstroLab FITS Analysis

## Purpose

Provide a disciplined approach to FITS structure and scientific capability detection.

## Fundamental Model

Treat a FITS file as a dataset composed of HDUs.

Conceptually:

```text
FITS Dataset
├── Primary HDU
├── Image HDUs
├── Table HDUs
├── Binary Table HDUs
├── WCS information
└── other recognised structures
```

A dataset may expose multiple scientific capabilities simultaneously.

Do not force the entire file into one mutually exclusive scientific category merely because one HDU matches a pattern.

## Capability Detection

Reason about capabilities independently.

Examples:

- image capability
- WCS capability
- spectral capability
- time-series capability
- catalogue/table capability

A `TIME` column alone does not prove that the entire FITS dataset is a time series.

Likewise, a pixel-bearing HDU does not automatically establish that it is the scientifically relevant image for every analysis.

Inspect the relevant HDU structure and metadata before deciding whether a capability exists.

## HDU-Level Reasoning

When modifying FITS interpretation:

1. Inspect all relevant HDUs.
2. Determine what each HDU contains.
3. Preserve per-HDU context.
4. Identify capabilities from sufficient evidence.
5. Separate structural facts from scientific interpretation.
6. Avoid discarding useful HDUs because another HDU was found first.

Do not assume:

- the primary HDU is always the useful image
- the first image HDU is always the scientifically relevant image
- a table with `TIME` is necessarily a time-series dataset
- a FITS file can have only one useful scientific interpretation

## WCS

When handling WCS:

- respect FITS coordinate conventions
- preserve axis semantics
- do not assume axis order without inspecting the relevant metadata
- distinguish absent WCS from invalid WCS
- distinguish unsupported valid projections from malformed data
- maintain consistency with the project's pixel-coordinate convention

Follow the existing `Wcs` implementation and specification rather than introducing a competing convention.

## FITS Parsing

Keep responsibilities separated:

- raw I/O belongs in Infrastructure
- deterministic parsing/interpretation of already-loaded data belongs in Core
- native/CFITSIO details belong in Infrastructure
- API DTOs must not expose FITS implementation details

Do not reinterpret FITS binary structure ad hoc in unrelated layers.

## Analysis Gating

An analysis operation should verify the capability it actually requires.

Examples:

```text
Photometry       -> Image
Astrometry       -> Image + WCS
Spectroscopy     -> Spectral data
Time-series      -> Time-series data
```

If the required capability is absent, return the appropriate expected failure rather than guessing.

## Testing

Test:

- multiple HDUs
- mixed image/table datasets
- irrelevant tables
- missing metadata
- malformed metadata
- multiple simultaneous capabilities
- WCS presence/absence
- representative real-world structures where fixtures are available

Include regression tests whenever a classification/capability bug is discovered.

## Refactoring Existing Classification

If existing code returns a single `FitsDatasetKind`:

1. Characterise current behaviour with tests.
2. Identify which callers depend on mutually exclusive classification.
3. Introduce capability representation without breaking unrelated behaviour unnecessarily.
4. Update analysis gating to require capabilities.
5. Remove simplistic dataset-wide inference.
6. Run the complete test suite.
7. Review the public API for accidental breaking changes.

Do not blindly replace a classifier without understanding its consumers.
