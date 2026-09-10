using System.Buffers;
using System.Collections.Immutable;
using AstroLab.Core.Imaging;
using AstroLab.Core.Result;

namespace AstroLab.Core.Sources;

/// <summary>
/// Segments an image into per-source pixel regions: thresholds against a mesh-based 2D background
/// model (<see cref="ImageBackgroundModeller"/>), flood-fills 8-connected pixels above threshold
/// into candidate blobs, then deblends any blob containing more than one strict local-intensity
/// maximum (separated by at least <see cref="MinimumPeakSeparationPixels"/>) by assigning each of
/// its pixels to its nearest peak. This is a single-pass, deterministic approximation of the
/// mesh-background-plus-multi-peak-deblending approach used by standard source-extraction
/// segmentation (e.g. SExtractor's segmentation map) — not full multi-threshold deblending.
/// </summary>
public static class ImageSegmenter
{
    public const int DefaultMeshSizePixels = 64;
    private const int Unassigned = -1;
    private const double PixelCenterOffset = 0.5;
    private const double MinimumPeakSeparationPixels = 2.0;
    private const double MinimumPeakSeparationPixelsSquared = MinimumPeakSeparationPixels * MinimumPeakSeparationPixels;

    public static Result<ImmutableArray<ImageSegment>> Segment(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea)
    {
        var boundsCheck = ValidateImageBounds(pixels.Length, width, height);

        if (boundsCheck.IsFailure)
        {
            return Result<ImmutableArray<ImageSegment>>.Failure(boundsCheck.Error);
        }

        if (thresholdSigma <= 0.0 || !double.IsFinite(thresholdSigma))
        {
            return Error.Validation("sources.segmentation.invalid_threshold", "thresholdSigma must be a finite, positive value.");
        }

        if (minimumArea < 1)
        {
            return Error.Validation("sources.segmentation.invalid_minimum_area", "minimumArea must be at least 1.");
        }

        var modelResult = ImageBackgroundModeller.Model(pixels, width, height, DefaultMeshSizePixels);

        if (modelResult.IsFailure)
        {
            return Result<ImmutableArray<ImageSegment>>.Failure(modelResult.Error);
        }

        var model = modelResult.Value;

        var background = model.MedianBackground;

        var sigma = model.BackgroundRms;

        if (sigma <= 0.0)
        {
            return ImmutableArray<ImageSegment>.Empty;
        }

        var thresholdValue = background + (thresholdSigma * sigma);

        return BuildSegments(pixels, width, height, background, thresholdValue, minimumArea);
    }

    private static ImmutableArray<ImageSegment> BuildSegments(
        ReadOnlySpan<float> pixels, int width, int height, double background, double thresholdValue, int minimumArea)
    {
        var pixelCount = pixels.Length;

        var regionId = ArrayPool<int>.Shared.Rent(pixelCount);

        var stack = ArrayPool<int>.Shared.Rent(pixelCount);

        try
        {
            regionId.AsSpan(0, pixelCount).Fill(Unassigned);

            var accumulations = new List<SegmentAccumulation>();

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

                var members = FloodFillMembers(pixels, width, height, startIndex, thresholdValue, regionId, stack);

                if (members.Count < minimumArea)
                {
                    continue;
                }

                var peaks = FindPeaks(pixels, width, height, members);

                if (peaks.Count <= 1)
                {
                    accumulations.Add(Accumulate(pixels, width, background, members));

                    continue;
                }

                foreach (var partition in PartitionByNearestPeak(width, members, peaks))
                {
                    if (partition.Count >= minimumArea)
                    {
                        accumulations.Add(Accumulate(pixels, width, background, partition));
                    }
                }
            }

            accumulations.Sort((left, right) =>
            {
                var pixelCountComparison = right.PixelCount.CompareTo(left.PixelCount);

                return pixelCountComparison != 0 ? pixelCountComparison : left.FirstPixelIndex.CompareTo(right.FirstPixelIndex);
            });

            var builder = ImmutableArray.CreateBuilder<ImageSegment>(accumulations.Count);

            for (var i = 0; i < accumulations.Count; i++)
            {
                var accumulation = accumulations[i];

                builder.Add(ImageSegment.Create(
                    i + 1, accumulation.PixelCount, accumulation.CentroidX, accumulation.CentroidY,
                    accumulation.MinX, accumulation.MinY, accumulation.MaxX, accumulation.MaxY));
            }

            return builder.MoveToImmutable();
        }
        finally
        {
            ArrayPool<int>.Shared.Return(regionId);

            ArrayPool<int>.Shared.Return(stack);
        }
    }

    private static List<int> FloodFillMembers(
        ReadOnlySpan<float> pixels, int width, int height, int startIndex, double thresholdValue, int[] regionId, int[] stack)
    {
        var stackTop = 0;

        stack[stackTop++] = startIndex;

        regionId[startIndex] = startIndex;

        var members = new List<int>();

        while (stackTop > 0)
        {
            var index = stack[--stackTop];

            members.Add(index);

            var x = index % width;

            var y = index / width;

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

                    var neighborIndex = neighborY * width + neighborX;

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

        return members;
    }
    
    private static List<(int X, int Y)> FindPeaks(ReadOnlySpan<float> pixels, int width, int height, List<int> members)
    {
        var candidates = new List<(int Index, int X, int Y, float Value)>();

        foreach (var index in members)
        {
            var x = index % width;

            var y = index / width;

            var value = pixels[index];

            var isPeak = true;

            for (var dy = -1; dy <= 1 && isPeak; dy++)
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

                    var neighborValue = pixels[neighborY * width + neighborX];

                    if (float.IsFinite(neighborValue) && neighborValue >= value)
                    {
                        isPeak = false;

                        break;
                    }
                }
            }

            if (isPeak)
            {
                candidates.Add((index, x, y, value));
            }
        }

        candidates.Sort((left, right) =>
        {
            var valueComparison = right.Value.CompareTo(left.Value);

            return valueComparison != 0 ? valueComparison : left.Index.CompareTo(right.Index);
        });

        var peaks = new List<(int X, int Y)>();

        foreach (var candidate in candidates)
        {
            var tooClose = false;

            foreach (var peak in peaks)
            {
                var dx = peak.X - candidate.X;

                var dy = peak.Y - candidate.Y;

                if (dx * dx + dy * dy < MinimumPeakSeparationPixelsSquared)
                {
                    tooClose = true;

                    break;
                }
            }

            if (!tooClose)
            {
                peaks.Add((candidate.X, candidate.Y));
            }
        }

        return peaks;
    }

    private static List<List<int>> PartitionByNearestPeak(int width, List<int> members, List<(int X, int Y)> peaks)
    {
        var partitions = new List<List<int>>(peaks.Count);

        for (var i = 0; i < peaks.Count; i++)
        {
            partitions.Add([]);
        }

        foreach (var index in members)
        {
            var x = index % width;

            var y = index / width;

            var bestPeak = 0;

            var bestDistanceSquared = double.PositiveInfinity;

            for (var i = 0; i < peaks.Count; i++)
            {
                var dx = peaks[i].X - x;

                var dy = peaks[i].Y - y;

                var distanceSquared = (double)(dx * dx + (dy * dy));

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;

                    bestPeak = i;
                }
            }

            partitions[bestPeak].Add(index);
        }

        return partitions;
    }

    private static SegmentAccumulation Accumulate(ReadOnlySpan<float> pixels, int width, double background, List<int> members)
    {
        var minX = int.MaxValue;

        var minY = int.MaxValue;

        var maxX = int.MinValue;

        var maxY = int.MinValue;

        var firstPixelIndex = int.MaxValue;

        double weightedXSum = 0.0;

        double weightedYSum = 0.0;

        double weightSum = 0.0;

        foreach (var index in members)
        {
            var x = index % width;

            var y = index / width;

            var weight = pixels[index] - background;

            weightedXSum += (x + PixelCenterOffset) * weight;

            weightedYSum += (y + PixelCenterOffset) * weight;

            weightSum += weight;

            if (x < minX)
            {
                minX = x;
            }

            if (x > maxX)
            {
                maxX = x;
            }

            if (y < minY)
            {
                minY = y;
            }

            if (y > maxY)
            {
                maxY = y;
            }

            if (index < firstPixelIndex)
            {
                firstPixelIndex = index;
            }
        }

        return new SegmentAccumulation(
            firstPixelIndex, members.Count, weightedXSum / weightSum, weightedYSum / weightSum, minX, minY, maxX, maxY);
    }

    private static Result<Unit> ValidateImageBounds(int pixelLength, int width, int height) =>
        width > 0 && height > 0 && pixelLength == width * height
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation(
                "sources.segmentation.invalid_image_bounds",
                $"Pixel span length ({pixelLength}) does not match width x height ({width}x{height}).");

    private readonly record struct SegmentAccumulation(
        int FirstPixelIndex, int PixelCount, double CentroidX, double CentroidY, int MinX, int MinY, int MaxX, int MaxY);
}
