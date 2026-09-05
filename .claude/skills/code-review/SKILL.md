---
name: code-review
description: Use when reviewing AstroLab code for design quality, maintainability, correctness, idiomatic C#, and architectural fitness beyond strict spec compliance.
---

# AstroLab Code Review

## Purpose

Review code as an experienced .NET engineer working within the AstroLab architecture.

A solution can satisfy the specification and still be unnecessarily complex, difficult to maintain, or poorly designed. This review looks for those problems without replacing the specification.

## Review Principles

Prefer:

- simple designs
- explicit responsibilities
- strong encapsulation
- readable code
- existing repository conventions
- narrow interfaces where abstraction is justified
- pure functions for scientific logic
- deterministic behaviour
- measured optimisation

Avoid:

- speculative abstractions
- abstraction for abstraction's sake
- excessive indirection
- duplicated conventions
- clever code that obscures scientific intent
- premature generalisation
- exposing implementation details

## Review Areas

### Responsibility

Ask:

- Does each type have a clear responsibility?
- Is logic in the correct layer?
- Is the endpoint merely orchestrating?
- Is Infrastructure doing I/O rather than Core?
- Is any class becoming a "god object"?

### API Design

Check:

- names communicate intent
- public surface is minimal
- inputs and outputs are coherent
- invalid states are difficult to represent
- DTOs do not expose internal implementation details

### C#

Check for:

- unnecessary allocations
- unnecessary async state machines
- incorrect cancellation propagation
- disposal/lifetime problems
- nullable reference type issues
- unnecessary casts
- boxing
- accidental enumeration
- unnecessary materialisation
- inappropriate mutable state

Do not recommend changes merely because an alternative is fashionable.

### LINQ

Do not assume LINQ is slow.

Use LINQ where it improves collection-oriented readability without a meaningful measured cost.

Prefer explicit loops for tight numerical/pixel-processing loops where direct indexing and memory access are clearer or measurably better.

### Error Handling

Check that:

- expected failures use `Result<T>`
- exceptions are not used as normal control flow
- errors are meaningful
- API clients do not receive raw exception details

### Scientific Code

Check that mathematical intent is obvious.

Flag:

- unexplained scientific assumptions
- unclear units
- coordinate convention ambiguity
- silently discarded invalid values
- questionable NaN/infinity handling
- algorithms that appear plausible but lack tests establishing correctness

Do not invent scientific requirements. If correctness depends on an unstated scientific convention, identify it as a question or recommendation.

## Output

Separate findings into:

- **Critical** — correctness, data integrity, security, or architecture
- **Important** — maintainability, reliability, or meaningful performance
- **Minor** — readability or polish
- **Good** — notable decisions worth retaining

For each finding give the smallest practical correction.

Do not turn a code review into a wholesale rewrite.
