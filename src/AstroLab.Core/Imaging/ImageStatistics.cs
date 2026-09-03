using System.Buffers;
using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>Summary statistics for a pixel array, computed while ignoring non-finite (NaN/Infinity) pixels.</summary>
public readonly record struct ImageStatistics
{
    private const int DefaultHistogramBins = 65536;
    private const int MaxStackallocHistogramBins = 1024;
    private const double MinPercentile = 0.0;
    private const double MaxPercentile = 100.0;
    private const double IqrToSigmaFactor = 1.349;
    private const int SkyBackgroundHistogramBins = 65536;
    private const double SkyBackgroundLowerPercentile = 25.0;
    private const double SkyBackgroundUpperPercentile = 75.0;
    private const double PercentageScale = 100.0;
    private const double Epsilon = 1e-9;

    public const int DefaultDisplayHistogramBinCount = 256;

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

    public double DeadPixelPercentage => InvalidPixelCount / (double)TotalPixelCount * PercentageScale;

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
        ReadOnlySpan<float> pixels, double lowerPercentile, double upperPercentile, int histogramBins = DefaultHistogramBins)
    {
        if (lowerPercentile < MinPercentile || upperPercentile > MaxPercentile || lowerPercentile >= upperPercentile)
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

        var stats = statsResult.Value;

        if (Math.Abs(stats.Max - stats.Min) < Epsilon)
        {
            return (stats.Min, stats.Max);
        }

        Span<long> histogram = histogramBins <= MaxStackallocHistogramBins ? stackalloc long[histogramBins] : new long[histogramBins];

        var range = stats.Max - stats.Min;

        var scale = histogramBins / range;

        PopulateHistogram(pixels, stats.Min, scale, histogram);

        var lowerBound = FindPercentileValue(histogram, stats.ValidPixelCount, stats.Min, scale, lowerPercentile);

        var upperBound = FindPercentileValue(histogram, stats.ValidPixelCount, stats.Min, scale, upperPercentile);

        return (lowerBound, upperBound);
    }

    public static Result<Unit> ComputePercentiles(
        ReadOnlySpan<float> pixels, ImageStatistics stats, ReadOnlySpan<double> percentiles, Span<double> results, int histogramBins = DefaultHistogramBins)
    {
        if (percentiles.Length != results.Length)
        {
            return Error.Validation(
                "imaging.percentile_result_length_mismatch",
                $"results length ({results.Length}) must match percentiles length ({percentiles.Length}).");
        }

        foreach (var percentile in percentiles)
        {
            if (percentile is < MinPercentile or > MaxPercentile)
            {
                return Error.Validation("imaging.invalid_percentile_range", "Each percentile must be between 0 and 100 inclusive.");
            }
        }

        if (Math.Abs(stats.Max - stats.Min) < Epsilon)
        {
            results.Fill(stats.Min);

            return Result<Unit>.Success(Unit.Value);
        }

        Span<long> histogram = histogramBins <= MaxStackallocHistogramBins ? stackalloc long[histogramBins] : new long[histogramBins];

        var range = stats.Max - stats.Min;

        var scale = histogramBins / range;

        PopulateHistogram(pixels, stats.Min, scale, histogram);

        for (var i = 0; i < percentiles.Length; i++)
        {
            results[i] = FindPercentileValue(histogram, stats.ValidPixelCount, stats.Min, scale, percentiles[i]);
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public static Result<ImageHistogram> ComputeHistogram(
        ReadOnlySpan<float> pixels, ImageStatistics stats, int binCount = DefaultDisplayHistogramBinCount)
    {
        if (binCount <= 0)
        {
            return Error.Validation("imaging.invalid_histogram_bin_count", "binCount must be positive.");
        }

        var binEdges = new double[binCount + 1];

        var counts = new long[binCount];

        if (Math.Abs(stats.Max - stats.Min) < Epsilon)
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

    private static double FindPercentileValue(ReadOnlySpan<long> histogram, long validPixelCount, double min, double scale, double percentile)
    {
        var target = (long)(validPixelCount * (percentile / MaxPercentile));

        long cumulative = 0;

        for (var bin = 0; bin < histogram.Length; bin++)
        {
            cumulative += histogram[bin];

            if (cumulative >= target)
            {
                return min + (bin + 1) / scale;
            }
        }

        return min + histogram.Length / scale;
    }

    public static SkyBackgroundStatistics ComputeSkyBackground(ReadOnlySpan<float> pixels, ImageStatistics stats)
    {
        if (Math.Abs(stats.Max - stats.Min) < Epsilon)
        {
            return SkyBackgroundStatistics.Create(stats.Min, stats.Max, 0.0);
        }

        var histogram = ArrayPool<long>.Shared.Rent(SkyBackgroundHistogramBins);

        try
        {
            var histogramSpan = histogram.AsSpan(0, SkyBackgroundHistogramBins);

            histogramSpan.Clear();

            var range = stats.Max - stats.Min;

            var scale = SkyBackgroundHistogramBins / range;

            PopulateHistogram(pixels, stats.Min, scale, histogramSpan);

            var q1 = FindPercentileValue(histogramSpan, stats.ValidPixelCount, stats.Min, scale, SkyBackgroundLowerPercentile);

            var q3 = FindPercentileValue(histogramSpan, stats.ValidPixelCount, stats.Min, scale, SkyBackgroundUpperPercentile);

            return SkyBackgroundStatistics.Create(q1, q3, (q3 - q1) / IqrToSigmaFactor);
        }
        finally
        {
            ArrayPool<long>.Shared.Return(histogram);
        }
    }

    public static ImageStatistics Create(double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(totalPixelCount);

        return new ImageStatistics(min, max, mean, stdDev, validPixelCount, totalPixelCount);
    }
}
