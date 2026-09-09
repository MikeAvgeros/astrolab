using System.Buffers;
using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Partitions an image into a grid of fixed-size mesh boxes and independently estimates the local
/// sky background level and noise (RMS) within each box, following the mesh-based approach used by
/// standard source-extraction background estimators (e.g. SExtractor's background mesh). Reporting
/// the median across all mesh boxes gives a background/noise-floor estimate that resists
/// contamination by bright, extended sources far better than a single whole-image statistic.
/// </summary>
public static class ImageBackgroundModeller
{
    private const double LowerQuartilePercentile = 25.0;
    private const double MedianPercentile = 50.0;
    private const double UpperQuartilePercentile = 75.0;
    private const int MaxMeshHistogramBins = 4096;

    public static Result<ImageBackgroundModel> Model(ReadOnlySpan<float> pixels, int width, int height, int meshSizePixels)
    {
        var boundsCheck = ValidateImageBounds(pixels.Length, width, height);

        if (boundsCheck.IsFailure)
        {
            return Result<ImageBackgroundModel>.Failure(boundsCheck.Error);
        }

        if (meshSizePixels <= 0)
        {
            return Error.Validation("imaging.background.invalid_mesh_size", "meshSizePixels must be positive.");
        }

        var meshCountX = (width + meshSizePixels - 1) / meshSizePixels;

        var meshCountY = (height + meshSizePixels - 1) / meshSizePixels;

        var meshBackgrounds = new List<double>(meshCountX * meshCountY);

        var meshRmsValues = new List<double>(meshCountX * meshCountY);

        var maxBoxWidth = Math.Min(meshSizePixels, width);

        var maxBoxHeight = Math.Min(meshSizePixels, height);

        var buffer = ArrayPool<float>.Shared.Rent(maxBoxWidth * maxBoxHeight);

        try
        {
            for (var meshY = 0; meshY < meshCountY; meshY++)
            {
                var yStart = meshY * meshSizePixels;

                var yEnd = Math.Min(yStart + meshSizePixels, height);

                for (var meshX = 0; meshX < meshCountX; meshX++)
                {
                    var xStart = meshX * meshSizePixels;

                    var xEnd = Math.Min(xStart + meshSizePixels, width);

                    var meshPixelCount = CopyMeshRows(pixels, width, xStart, xEnd, yStart, yEnd, buffer);

                    TryAddMeshStatistics(buffer.AsSpan(0, meshPixelCount), meshBackgrounds, meshRmsValues);
                }
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }

        if (meshBackgrounds.Count == 0)
        {
            return Error.Validation("imaging.background.no_valid_meshes", "No mesh box contained valid pixel data.");
        }

        return ImageBackgroundModel.Create(meshSizePixels, meshCountX, meshCountY, Median(meshBackgrounds), Median(meshRmsValues));
    }

    private static int CopyMeshRows(ReadOnlySpan<float> pixels, int width, int xStart, int xEnd, int yStart, int yEnd, float[] buffer)
    {
        var meshWidth = xEnd - xStart;

        var count = 0;

        for (var y = yStart; y < yEnd; y++)
        {
            var rowStart = y * width + xStart;

            pixels.Slice(rowStart, meshWidth).CopyTo(buffer.AsSpan(count, meshWidth));

            count += meshWidth;
        }

        return count;
    }

    private static void TryAddMeshStatistics(ReadOnlySpan<float> meshPixels, List<double> meshBackgrounds, List<double> meshRmsValues)
    {
        var statsResult = ImageStatistics.Compute(meshPixels);

        if (statsResult.IsFailure)
        {
            return;
        }

        var stats = statsResult.Value;

        var histogramBins = (int)Math.Min(stats.ValidPixelCount, MaxMeshHistogramBins);

        Span<double> percentileValues = stackalloc double[3];

        var percentilesResult = ImageStatistics.ComputePercentiles(
            meshPixels, stats, [LowerQuartilePercentile, MedianPercentile, UpperQuartilePercentile], percentileValues, histogramBins);

        if (percentilesResult.IsFailure)
        {
            return;
        }

        var q1 = percentileValues[0];

        var median = percentileValues[1];

        var q3 = percentileValues[2];

        meshBackgrounds.Add(median);

        meshRmsValues.Add((q3 - q1) / ImageStatistics.IqrToSigmaFactor);
    }

    private static double Median(List<double> values)
    {
        values.Sort();

        var midIndex = values.Count / 2;

        return values.Count % 2 == 1
            ? values[midIndex]
            : (values[midIndex - 1] + values[midIndex]) / 2.0;
    }

    private static Result<Unit> ValidateImageBounds(int pixelLength, int width, int height) =>
        width > 0 && height > 0 && pixelLength == width * height
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation(
                "imaging.background.invalid_image_bounds",
                $"Pixel span length ({pixelLength}) does not match width x height ({width}x{height}).");
}
