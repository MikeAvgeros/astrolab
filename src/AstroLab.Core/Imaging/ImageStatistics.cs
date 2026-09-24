using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

public readonly record struct ImageStatistics
{
    private const double MinPercentile = 0.0;
    private const double MaxPercentile = 100.0;
    internal const double IqrToSigmaFactor = 1.349;
    private const double PercentageScale = 100.0;

    public const int DefaultDisplayHistogramBinCount = 256;
    public const int MaxDisplayHistogramBinCount = 65536;

    private static readonly double[] SkyBackgroundQuartilePercentiles = [25.0, 75.0];

    private ImageStatistics(double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount)
    {
        Min = min;
        Max = max;
        Mean = mean;
        StdDev = stdDev;
        ValidPixelCount = validPixelCount;
        TotalPixelCount = totalPixelCount;
    }

    public double Min { get; }

    public double Max { get; }

    public double Mean { get; }

    public double StdDev { get; }

    public long ValidPixelCount { get; }

    public long TotalPixelCount { get; }

    public long InvalidPixelCount => TotalPixelCount - ValidPixelCount;

    public double DeadPixelPercentage => TotalPixelCount > 0 ? InvalidPixelCount / (double)TotalPixelCount * PercentageScale : 0.0;

    public static Result<ImageStatistics> Compute(ReadOnlySpan<float> pixels)
    {
        if (pixels.Length == 0)
        {
            return Error.Validation("imaging.empty_pixel_array", "Cannot compute statistics over an empty pixel array.");
        }

        var min = double.PositiveInfinity;

        var max = double.NegativeInfinity;

        double sum = 0.0;

        long validCount = 0;

        foreach (var value in pixels)
        {
            if (!float.IsFinite(value))
            {
                continue;
            }

            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }

            sum += value;

            validCount++;
        }

        if (validCount == 0)
        {
            return Error.Validation("imaging.no_valid_pixels", "Pixel array contains no finite values.");
        }

        var mean = sum / validCount;

        double sumSquaredDeviation = 0.0;

        foreach (var value in pixels)
        {
            if (!float.IsFinite(value))
            {
                continue;
            }

            var deviation = value - mean;

            sumSquaredDeviation += deviation * deviation;
        }

        var stdDev = Math.Sqrt(sumSquaredDeviation / validCount);

        return Create(min, max, mean, stdDev, validCount, pixels.Length);
    }

    public static Result<(double Lower, double Upper)> ComputePercentileBounds(
        ReadOnlySpan<float> pixels, double lowerPercentile, double upperPercentile)
    {
        if (!(lowerPercentile >= MinPercentile && upperPercentile <= MaxPercentile && lowerPercentile < upperPercentile))
        {
            return Error.Validation(
                "imaging.invalid_percentile_range",
                "Require 0 <= lowerPercentile < upperPercentile <= 100.");
        }

        var statsResult = Compute(pixels);

        if (statsResult.IsFailure)
        {
            return Result<(double, double)>.Failure(statsResult.Error);
        }

        Span<double> bounds = stackalloc double[2];

        PercentileSelector.Compute(pixels, statsResult.Value, [lowerPercentile, upperPercentile], bounds);

        return (bounds[0], bounds[1]);
    }

    public static Result<Unit> ComputePercentiles(
        ReadOnlySpan<float> pixels, ImageStatistics stats, ReadOnlySpan<double> percentiles, Span<double> results)
    {
        if (percentiles.Length != results.Length)
        {
            return Error.Validation(
                "imaging.percentile_result_length_mismatch",
                $"results length ({results.Length}) must match percentiles length ({percentiles.Length}).");
        }

        foreach (var percentile in percentiles)
        {
            if (percentile is not (>= MinPercentile and <= MaxPercentile))
            {
                return Error.Validation("imaging.invalid_percentile_range", "Each percentile must be between 0 and 100 inclusive.");
            }
        }

        PercentileSelector.Compute(pixels, stats, percentiles, results);

        return Result<Unit>.Success(Unit.Value);
    }

    public static Result<ImageHistogram> ComputeHistogram(
        ReadOnlySpan<float> pixels, ImageStatistics stats, int binCount = DefaultDisplayHistogramBinCount)
    {
        if (binCount is <= 0 or > MaxDisplayHistogramBinCount)
        {
            return Error.Validation(
                "imaging.invalid_histogram_bin_count", $"binCount must be between 1 and {MaxDisplayHistogramBinCount}.");
        }

        var binEdges = new double[binCount + 1];

        var counts = new long[binCount];

        if (stats.Max <= stats.Min)
        {
            Array.Fill(binEdges, stats.Min);

            counts[0] = stats.ValidPixelCount;

            return ImageHistogram.Create(ImmutableArray.Create(binEdges), ImmutableArray.Create(counts), stats.ValidPixelCount);
        }

        var range = stats.Max - stats.Min;

        var scale = binCount / range;

        PopulateHistogram(pixels, stats.Min, scale, counts);

        for (var i = 0; i <= binCount; i++)
        {
            binEdges[i] = stats.Min + i / scale;
        }

        return ImageHistogram.Create([.. binEdges], [.. counts], stats.ValidPixelCount);
    }
    
    public static SkyBackgroundStatistics ComputeSkyBackground(ReadOnlySpan<float> pixels, ImageStatistics stats)
    {
        Span<double> quartiles = stackalloc double[2];

        PercentileSelector.Compute(pixels, stats, SkyBackgroundQuartilePercentiles, quartiles);

        return SkyBackgroundStatistics.Create(quartiles[0], quartiles[1], (quartiles[1] - quartiles[0]) / IqrToSigmaFactor);
    }

    private static void PopulateHistogram(ReadOnlySpan<float> pixels, double min, double scale, Span<long> histogram)
    {
        foreach (var value in pixels)
        {
            if (!float.IsFinite(value))
            {
                continue;
            }

            var bin = (int)((value - min) * scale);

            bin = Math.Clamp(bin, 0, histogram.Length - 1);

            histogram[bin]++;
        }
    }

    private static ImageStatistics Create(double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(totalPixelCount);

        return new ImageStatistics(min, max, mean, stdDev, validPixelCount, totalPixelCount);
    }
}
