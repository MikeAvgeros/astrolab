using System.Buffers;
using System.Collections.Immutable;
using AstroLab.Core.Imaging;
using AstroLab.Core.Result;

namespace AstroLab.Core.Sources;

/// <summary>
/// Deterministic, threshold-based source detection: estimates the image background and noise,
/// flags pixels significantly above it, groups neighbouring flagged pixels into candidate sources
/// via 8-connected flood fill, and reports each region's flux-weighted centroid, peak, integrated
/// flux, and signal-to-noise ratio. This is basic detection — favouring a simple, deterministic
/// algorithm — not production-grade photometry or scientific catalogue generation.
/// </summary>
public static class SourceDetector
{
    public const double DefaultThresholdSigma = 5.0;
    public const int DefaultMinimumArea = 5;
    public const int DefaultMaxSources = 100;
    private const double MedianPercentile = 50.0;
    private const double PixelCenterOffset = 0.5;
    private const int Unassigned = -1;

    /// <summary>
    /// Detects candidate sources in <paramref name="pixels"/>. The background level (image median)
    /// and noise (robust IQR-based sigma) are estimated globally from the whole image — see
    /// <see cref="ImageStatistics.ComputeSkyBackground"/> — then every finite pixel more than
    /// <paramref name="thresholdSigma"/> sigma above the background is flagged and grouped into
    /// 8-connected regions. Regions smaller than <paramref name="minimumArea"/> pixels are
    /// discarded; the remainder are ranked by integrated flux (descending) and truncated to
    /// <paramref name="maxSources"/>. Deterministic for a given input and configuration.
    /// </summary>
    public static Result<ImmutableArray<DetectedSource>> Detect(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double thresholdSigma = DefaultThresholdSigma,
        int minimumArea = DefaultMinimumArea,
        int maxSources = DefaultMaxSources)
    {
        var boundsCheck = ValidateImageBounds(pixels.Length, width, height);

        if (boundsCheck.IsFailure)
        {
            return Result<ImmutableArray<DetectedSource>>.Failure(boundsCheck.Error);
        }

        if (thresholdSigma <= 0.0 || !double.IsFinite(thresholdSigma))
        {
            return Error.Validation("sources.invalid_threshold", "thresholdSigma must be a finite, positive value.");
        }

        if (minimumArea < 1)
        {
            return Error.Validation("sources.invalid_minimum_area", "minimumArea must be at least 1.");
        }

        if (maxSources < 1)
        {
            return Error.Validation("sources.invalid_max_sources", "maxSources must be at least 1.");
        }

        var statsResult = ImageStatistics.Compute(pixels);

        if (statsResult.IsFailure)
        {
            return Result<ImmutableArray<DetectedSource>>.Failure(statsResult.Error);
        }

        var stats = statsResult.Value;

        Span<double> medianSpan = stackalloc double[1];

        var percentileResult = ImageStatistics.ComputePercentiles(pixels, stats, [MedianPercentile], medianSpan);

        if (percentileResult.IsFailure)
        {
            return Result<ImmutableArray<DetectedSource>>.Failure(percentileResult.Error);
        }

        var background = medianSpan[0];

        var sigma = ImageStatistics.ComputeSkyBackground(pixels, stats).SkySigma;

        if (sigma <= 0.0)
        {
            return ImmutableArray<DetectedSource>.Empty;
        }

        var thresholdValue = background + (thresholdSigma * sigma);

        return DetectAboveThreshold(pixels, width, height, background, sigma, thresholdValue, minimumArea, maxSources);
    }

    private static Result<ImmutableArray<DetectedSource>> DetectAboveThreshold(
        ReadOnlySpan<float> pixels, int width, int height,
        double background, double sigma, double thresholdValue, int minimumArea, int maxSources)
    {
        var pixelCount = pixels.Length;

        var regionId = ArrayPool<int>.Shared.Rent(pixelCount);

        var stack = ArrayPool<int>.Shared.Rent(pixelCount);

        try
        {
            regionId.AsSpan(0, pixelCount).Fill(Unassigned);

            var candidates = new List<SourceCandidate>();

            for (var startIndex = 0; startIndex < pixelCount; startIndex++)
            {
                if (regionId[startIndex] != Unassigned)
                {
                    continue;
                }

                var value = pixels[startIndex];

                if (!float.IsFinite(value) || value <= thresholdValue)
                {
                    continue;
                }

                var candidate = FloodFillRegion(pixels, width, height, startIndex, thresholdValue, background, regionId, stack);

                if (candidate.PixelCount >= minimumArea)
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort((left, right) =>
            {
                var fluxComparison = right.TotalFlux.CompareTo(left.TotalFlux);

                return fluxComparison != 0 ? fluxComparison : left.FirstPixelIndex.CompareTo(right.FirstPixelIndex);
            });

            var resultCount = Math.Min(candidates.Count, maxSources);

            var builder = ImmutableArray.CreateBuilder<DetectedSource>(resultCount);

            for (var i = 0; i < resultCount; i++)
            {
                var candidate = candidates[i];

                builder.Add(DetectedSource.Create(
                    id: i + 1,
                    pixelX: candidate.WeightedXSum / candidate.WeightSum,
                    pixelY: candidate.WeightedYSum / candidate.WeightSum,
                    pixelCount: candidate.PixelCount,
                    peakValue: candidate.PeakValue,
                    totalFlux: candidate.TotalFlux,
                    background: background,
                    signalToNoiseRatio: candidate.TotalFlux / (sigma * Math.Sqrt(candidate.PixelCount))));
            }

            return builder.MoveToImmutable();
        }
        finally
        {
            ArrayPool<int>.Shared.Return(regionId);

            ArrayPool<int>.Shared.Return(stack);
        }
    }

    /// <summary>
    /// Iteratively (not recursively, to avoid stack overflow on large blobs) flood-fills the
    /// 8-connected region of above-threshold pixels reachable from <paramref name="startIndex"/>,
    /// accumulating flux-weighted centroid, peak, and total-flux statistics as it visits each pixel
    /// exactly once. <paramref name="stack"/> is a caller-owned, pixel-count-sized scratch buffer
    /// reused across every region in the image, avoiding a per-region allocation.
    /// </summary>
    private static SourceCandidate FloodFillRegion(
        ReadOnlySpan<float> pixels, int width, int height, int startIndex, double thresholdValue, double background,
        int[] regionId, int[] stack)
    {
        var stackTop = 0;

        stack[stackTop++] = startIndex;

        regionId[startIndex] = startIndex;

        var pixelCount = 0;

        var peakValue = double.NegativeInfinity;

        double totalFlux = 0.0;

        double weightedXSum = 0.0;

        double weightedYSum = 0.0;

        double weightSum = 0.0;

        while (stackTop > 0)
        {
            var index = stack[--stackTop];

            var x = index % width;

            var y = index / width;

            double value = pixels[index];

            var weight = value - background;

            pixelCount++;

            totalFlux += weight;

            weightedXSum += (x + PixelCenterOffset) * weight;

            weightedYSum += (y + PixelCenterOffset) * weight;

            weightSum += weight;

            if (value > peakValue)
            {
                peakValue = value;
            }

            for (var dy = -1; dy <= 1; dy++)
            {
                var neighborY = y + dy;

                if (neighborY < 0 || neighborY >= height)
                {
                    continue;
                }

                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var neighborX = x + dx;

                    if (neighborX < 0 || neighborX >= width)
                    {
                        continue;
                    }

                    var neighborIndex = (neighborY * width) + neighborX;

                    if (regionId[neighborIndex] != Unassigned)
                    {
                        continue;
                    }

                    var neighborValue = pixels[neighborIndex];

                    if (!float.IsFinite(neighborValue) || neighborValue <= thresholdValue)
                    {
                        continue;
                    }

                    regionId[neighborIndex] = startIndex;

                    stack[stackTop++] = neighborIndex;
                }
            }
        }

        return SourceCandidate.Create(startIndex, pixelCount, peakValue, totalFlux, weightedXSum, weightedYSum, weightSum);
    }

    private static Result<Unit> ValidateImageBounds(int pixelLength, int width, int height) =>
        width > 0 && height > 0 && pixelLength == width * height
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation("sources.invalid_image_bounds", $"Pixel span length ({pixelLength}) does not match width x height ({width}x{height}).");
}
