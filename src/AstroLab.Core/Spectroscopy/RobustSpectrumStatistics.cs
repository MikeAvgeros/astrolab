namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Shared robust central-tendency and noise estimation for a 1D flux spectrum: the median (robust
/// against a small number of strong emission/absorption features) and an inter-quartile-range-based
/// estimate of the per-point noise sigma (assuming a normal noise distribution, for which
/// IQR/1.349 is the standard robust sigma estimator). Used by both <see cref="SpectralLineDetector"/>
/// (continuum/noise for line detection) and <see cref="SpectrumSignalToNoiseEstimator"/> (spectrum-wide SNR).
/// </summary>
internal static class RobustSpectrumStatistics
{
    private const double LowerPercentile = 25.0;
    private const double UpperPercentile = 75.0;
    private const double IqrToSigmaFactor = 1.349;

    public static (double Median, double Sigma) Compute(ReadOnlySpan<double> spectrum)
    {
        var sorted = spectrum.ToArray();

        Array.Sort(sorted);

        var median = Percentile(sorted, 50.0);

        var q1 = Percentile(sorted, LowerPercentile);

        var q3 = Percentile(sorted, UpperPercentile);

        return (median, (q3 - q1) / IqrToSigmaFactor);
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var rank = percentile / 100.0 * (sorted.Length - 1);

        var lowerIndex = (int)Math.Floor(rank);

        var upperIndex = (int)Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var fraction = rank - lowerIndex;

        return sorted[lowerIndex] + fraction * (sorted[upperIndex] - sorted[lowerIndex]);
    }
}
