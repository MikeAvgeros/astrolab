using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure light-curve variability analysis: summarizes a flux series with the standard
/// dispersion/variability statistics (mean, median, standard deviation, amplitude, RMS, and
/// median absolute deviation) used to characterize how much a source's brightness varies.
/// </summary>
public static class LightCurveVariabilityAnalyzer
{
    public static Result<LightCurveVariabilityStatistics> Analyze(ReadOnlySpan<double> flux)
    {
        if (flux.IsEmpty)
        {
            return Error.Validation("timeseries.variability.empty_series", "The light curve contains no points to analyze.");
        }

        var mean = Mean(flux);

        var sumSquaredDeviation = 0.0;

        var sumSquared = 0.0;

        var min = flux[0];

        var max = flux[0];

        foreach (var value in flux)
        {
            var deviation = value - mean;

            sumSquaredDeviation += deviation * deviation;

            sumSquared += value * value;

            min = Math.Min(min, value);

            max = Math.Max(max, value);
        }

        var standardDeviation = Math.Sqrt(sumSquaredDeviation / flux.Length);

        var rms = Math.Sqrt(sumSquared / flux.Length);

        var amplitude = max - min;

        var sorted = flux.ToArray();

        Array.Sort(sorted);

        var median = Median(sorted);

        var absoluteDeviations = new double[sorted.Length];

        for (var i = 0; i < sorted.Length; i++)
        {
            absoluteDeviations[i] = Math.Abs(flux[i] - median);
        }

        Array.Sort(absoluteDeviations);

        var mad = Median(absoluteDeviations);

        return LightCurveVariabilityStatistics.Create(mean, median, standardDeviation, amplitude, rms, mad);
    }

    private static double Mean(ReadOnlySpan<double> values)
    {
        var sum = 0.0;

        foreach (var value in values)
        {
            sum += value;
        }

        return sum / values.Length;
    }

    private static double Median(double[] sorted) =>
        sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2.0;
}
