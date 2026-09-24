using System.Buffers;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Computes exact, linearly interpolated percentiles (the definition used by NumPy and Astropy by
/// default) of the finite values in a pixel span without sorting or copying the whole span. Each
/// required order statistic is located by histogram refinement: a histogram over the value range
/// identifies the bin holding the requested rank, only that bin's values are gathered into a pooled
/// buffer, and the gathered values are refined the same way until few enough remain to sort. Unlike
/// a fixed-width histogram, the result stays exact when a single outlier (a cosmic ray or saturated
/// star) stretches the value range far beyond the bulk of the data.
/// </summary>
internal static class PercentileSelector
{
    private const int HistogramBins = 4096;
    private const int SortThreshold = 64;
    private const double MaxPercentile = 100.0;

    public static void Compute(
        ReadOnlySpan<float> pixels, ImageStatistics stats, ReadOnlySpan<double> percentiles, Span<double> results)
    {
        if (stats.Max <= stats.Min)
        {
            results.Fill(stats.Min);

            return;
        }

        var scale = HistogramBins / (stats.Max - stats.Min);

        var histogram = ArrayPool<long>.Shared.Rent(HistogramBins);

        var scratchHistogram = ArrayPool<long>.Shared.Rent(HistogramBins);

        try
        {
            var histogramSpan = histogram.AsSpan(0, HistogramBins);

            histogramSpan.Clear();

            foreach (var value in pixels)
            {
                if (float.IsFinite(value))
                {
                    histogramSpan[BinIndex(value, stats.Min, scale)]++;
                }
            }

            for (var i = 0; i < percentiles.Length; i++)
            {
                results[i] = InterpolatePercentile(
                    pixels, stats, scale, histogramSpan, scratchHistogram.AsSpan(0, HistogramBins), percentiles[i]);
            }
        }
        finally
        {
            ArrayPool<long>.Shared.Return(histogram);

            ArrayPool<long>.Shared.Return(scratchHistogram);
        }
    }

    private static double InterpolatePercentile(
        ReadOnlySpan<float> pixels, ImageStatistics stats, double scale, ReadOnlySpan<long> histogram, Span<long> scratchHistogram, double percentile)
    {
        var position = percentile / MaxPercentile * (stats.ValidPixelCount - 1);

        var lowerRank = (long)Math.Floor(position);

        var fraction = position - lowerRank;

        var lowerValue = SelectOrderStatistic(pixels, stats.Min, scale, histogram, scratchHistogram, lowerRank);

        if (fraction <= 0.0)
        {
            return lowerValue;
        }

        var upperValue = SelectOrderStatistic(pixels, stats.Min, scale, histogram, scratchHistogram, lowerRank + 1);

        return lowerValue + fraction * (upperValue - lowerValue);
    }

    private static double SelectOrderStatistic(
        ReadOnlySpan<float> pixels, double min, double scale, ReadOnlySpan<long> histogram, Span<long> scratchHistogram, long rank)
    {
        var (bin, rankInBin) = FindBin(histogram, rank);

        var binCount = (int)histogram[bin];

        var gathered = ArrayPool<float>.Shared.Rent(binCount);

        try
        {
            var count = 0;

            foreach (var value in pixels)
            {
                if (float.IsFinite(value) && BinIndex(value, min, scale) == bin)
                {
                    gathered[count++] = value;
                }
            }

            return SelectFromBuffer(gathered.AsSpan(0, count), scratchHistogram, (int)rankInBin);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(gathered);
        }
    }

    private static (int Bin, long RankInBin) FindBin(ReadOnlySpan<long> histogram, long rank)
    {
        var remaining = rank;

        for (var bin = 0; bin < histogram.Length; bin++)
        {
            if (remaining < histogram[bin])
            {
                return (bin, remaining);
            }

            remaining -= histogram[bin];
        }

        throw new ArgumentOutOfRangeException(nameof(rank), "Rank exceeds the number of histogrammed values.");
    }

    private static double SelectFromBuffer(Span<float> values, Span<long> histogram, int rank)
    {
        while (values.Length > SortThreshold)
        {
            var (min, max) = FindRange(values);

            if (max <= min)
            {
                return min;
            }

            var scale = HistogramBins / (max - min);

            histogram.Clear();

            foreach (var value in values)
            {
                histogram[BinIndex(value, min, scale)]++;
            }

            var (bin, rankInBin) = FindBin(histogram, rank);

            var count = 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (BinIndex(values[i], min, scale) == bin)
                {
                    values[count++] = values[i];
                }
            }

            values = values[..count];

            rank = (int)rankInBin;
        }

        values.Sort();

        return values[rank];
    }

    private static (double Min, double Max) FindRange(ReadOnlySpan<float> values)
    {
        var min = double.PositiveInfinity;

        var max = double.NegativeInfinity;

        foreach (var value in values)
        {
            min = Math.Min(min, value);

            max = Math.Max(max, value);
        }

        return (min, max);
    }

    private static int BinIndex(double value, double min, double scale) =>
        Math.Clamp((int)((value - min) * scale), 0, HistogramBins - 1);
}
