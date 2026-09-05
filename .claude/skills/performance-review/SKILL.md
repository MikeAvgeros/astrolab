---
name: performance-review
description: Use when reviewing or optimising AstroLab performance, allocations, FITS pixel processing, numerical algorithms, streaming, or large-data handling.
---

# AstroLab Performance Review

## Purpose

Improve performance where it matters while preserving scientific correctness and maintainability.

The project is allocation-conscious, especially for Core hot data-processing paths, but performance rules must be applied to the actual workload rather than mechanically.

## First Rule: Measure

Before making a non-trivial optimisation:

1. Identify the suspected bottleneck.
2. Establish a baseline where practical.
3. Determine whether the issue is CPU, allocation, memory bandwidth, I/O, GC pressure, or contention.
4. Make the smallest targeted change.
5. Benchmark or measure again.
6. Verify scientific correctness.

Do not optimise code merely because it looks theoretically inefficient.

## Hot Numerical Paths

Pay particular attention to:

- per-pixel processing
- large contiguous arrays
- image statistics
- histogram construction
- image scaling
- source detection
- spectral extraction
- repeated coordinate transformations

Consider:

- `ReadOnlySpan<T>` / `Span<T>`
- direct indexing
- loop structure
- avoiding intermediate collections
- avoiding boxing
- reducing repeated calculations
- cache-friendly access patterns

## LINQ

Modern .NET LINQ can be highly optimised and is appropriate in many workloads.

Do not reject LINQ automatically.

Prefer explicit loops when a numerical/pixel-processing path is genuinely hot and direct iteration gives better control over:

- memory access
- branching
- allocations
- repeated enumeration

Benchmark when the distinction materially affects performance.

## Allocation Review

Look for:

- unnecessary arrays
- repeated temporary collections
- `ToArray()` / `ToList()` materialisation
- closures
- boxing
- string construction in hot paths
- iterator overhead where it matters
- repeated conversions/copies

Natural allocations are acceptable when they represent the actual result of an operation.

Do not force a collection-returning algorithm to avoid allocating its result merely to satisfy a generic "zero allocation" goal.

## Unsafe / Native Memory

Use unmanaged memory, `unsafe`, `NativeMemory`, pooling, `stackalloc`, or SIMD only when justified.

Every such optimisation should have:

- a clear performance reason
- correct ownership/lifetime
- tests
- no unnecessary architectural leakage into Core

CFITSIO and native resource management remain Infrastructure concerns.

## Streaming

Large FITS and network payloads should not be fully buffered into one managed `byte[]`.

Check:

- streaming behaviour
- cancellation propagation
- backpressure
- disposal
- accidental buffering
- retry semantics for large downloads

## Allocation Tests

When a hot Core algorithm has an allocation requirement:

- isolate test-harness/setup allocations
- measure the algorithm itself
- use `GC.GetAllocatedBytesForCurrentThread()` or an appropriate benchmark framework
- test realistic input sizes

Do not treat a legitimate result allocation as an unwanted internal allocation.

## Performance Review Output

Report:

- **Measured issue**
- **Likely cause**
- **Recommended change**
- **Expected trade-off**
- **How to verify**

If no meaningful performance issue is found, say so.

Do not recommend micro-optimisations with no plausible material benefit.
