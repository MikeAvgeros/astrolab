using System.Buffers;
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

    /// <summary>
    /// Computes min/max/mean/standard-deviation over <paramref name="pixels"/> in two zero-allocation
    /// passes (the second pass is required for a numerically stable variance computation).
    /// </summary>
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

    /// <summary>
    /// Estimates lower/upper percentile clipping bounds via a fixed-size histogram (bounded
    /// allocation, independent of image size), suitable for choosing black/white points for
    /// display without sorting the full — potentially gigapixel — pixel array.
    /// </summary>
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

        if (stats.Max == stats.Min)
        {
            return (stats.Min, stats.Max);
        }

        Span<long> histogram = histogramBins <= MaxStackallocHistogramBins ? stackalloc long[histogramBins] : new long[histogramBins];

        var range = stats.Max - stats.Min;

        var scale = histogramBins / range;

        foreach (var value in pixels)
        {
            if (!float.IsFinite(value))
            {
                continue;
            }

            var bin = (int)((value - stats.Min) * scale);

            bin = Math.Clamp(bin, 0, histogramBins - 1);

            histogram[bin]++;
        }

        var lowerTarget = (long)(stats.ValidPixelCount * (lowerPercentile / MaxPercentile));

        var upperTarget = (long)(stats.ValidPixelCount * (upperPercentile / MaxPercentile));

        var lowerBound = stats.Min;

        var upperBound = stats.Max;

        long cumulative = 0;

        var lowerFound = false;

        for (var bin = 0; bin < histogramBins; bin++)
        {
            cumulative += histogram[bin];

            if (!lowerFound && cumulative >= lowerTarget)
            {
                lowerBound = stats.Min + ((bin + 1) / scale);

                lowerFound = true;
            }

            if (cumulative >= upperTarget)
            {
                upperBound = stats.Min + ((bin + 1) / scale);

                break;
            }
        }

        return (lowerBound, upperBound);
    }

    /// <summary>
    /// Estimates a robust sky-background sigma via the interquartile range of <paramref name="pixels"/>
    /// (<c>(Q3 - Q1) / 1.349</c>, the standard IQR-to-Gaussian-sigma conversion), using a fixed-size
    /// pooled histogram so cost stays O(n) with zero managed-heap allocation regardless of image size —
    /// no sorting of the full pixel array. <paramref name="stats"/> must be the result of a prior
    /// successful <see cref="Compute"/> call over the same <paramref name="pixels"/> span.
    /// </summary>
    public static SkyBackgroundStatistics ComputeSkyBackground(ReadOnlySpan<float> pixels, ImageStatistics stats)
    {
        if (stats.Max == stats.Min)
        {
            return SkyBackgroundStatistics.Create(stats.Min, stats.Max, 0.0);
        }

        var histogram = ArrayPool<long>.Shared.Rent(SkyBackgroundHistogramBins);

        try
        {
            histogram.AsSpan(0, SkyBackgroundHistogramBins).Clear();

            var range = stats.Max - stats.Min;

            var scale = SkyBackgroundHistogramBins / range;

            foreach (var value in pixels)
            {
                if (!float.IsFinite(value))
                {
                    continue;
                }

                var bin = (int)((value - stats.Min) * scale);

                bin = Math.Clamp(bin, 0, SkyBackgroundHistogramBins - 1);

                histogram[bin]++;
            }

            var lowerTarget = (long)(stats.ValidPixelCount * (SkyBackgroundLowerPercentile / MaxPercentile));

            var upperTarget = (long)(stats.ValidPixelCount * (SkyBackgroundUpperPercentile / MaxPercentile));

            var q1 = stats.Min;

            var q3 = stats.Max;

            long cumulative = 0;

            var lowerFound = false;

            for (var bin = 0; bin < SkyBackgroundHistogramBins; bin++)
            {
                cumulative += histogram[bin];

                if (!lowerFound && cumulative >= lowerTarget)
                {
                    q1 = stats.Min + ((bin + 1) / scale);

                    lowerFound = true;
                }

                if (cumulative >= upperTarget)
                {
                    q3 = stats.Min + ((bin + 1) / scale);

                    break;
                }
            }

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
