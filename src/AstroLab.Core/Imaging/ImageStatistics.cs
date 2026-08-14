using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>Summary statistics for a pixel array, computed while ignoring non-finite (NaN/Infinity) pixels.</summary>
public readonly record struct ImageStatistics(
    double Min,
    double Max,
    double Mean,
    double StdDev,
    long ValidPixelCount,
    long TotalPixelCount)
{
    public long InvalidPixelCount => TotalPixelCount - ValidPixelCount;

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
        return new ImageStatistics(min, max, mean, stdDev, validCount, pixels.Length);
    }

    /// <summary>
    /// Estimates lower/upper percentile clipping bounds via a fixed-size histogram (bounded
    /// allocation, independent of image size), suitable for choosing black/white points for
    /// display without sorting the full — potentially gigapixel — pixel array.
    /// </summary>
    public static Result<(double Lower, double Upper)> ComputePercentileBounds(
        ReadOnlySpan<float> pixels, double lowerPercentile, double upperPercentile, int histogramBins = 65536)
    {
        if (lowerPercentile < 0 || upperPercentile > 100 || lowerPercentile >= upperPercentile)
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

        Span<long> histogram = histogramBins <= 1024 ? stackalloc long[histogramBins] : new long[histogramBins];
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

        var lowerTarget = (long)(stats.ValidPixelCount * (lowerPercentile / 100.0));
        var upperTarget = (long)(stats.ValidPixelCount * (upperPercentile / 100.0));

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
}
