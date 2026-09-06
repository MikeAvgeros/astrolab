using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure light-curve detrending: removes a slowly-varying baseline from a flux series so that
/// shorter-timescale variability (transits, pulsations, flares) stands out. Supports a "linear"
/// method (subtracts the least-squares best-fit line) and a "median" method (subtracts a
/// fixed-width moving median, edge-truncated at the series boundaries).
/// </summary>
public static class LightCurveDetrender
{
    private const int MovingMedianWindowPoints = 5;
    private const int MinimumPointsForMovingMedian = 3;

    public static Result<double[]> Detrend(ReadOnlySpan<double> time, ReadOnlySpan<double> flux, string method)
    {
        if (time.Length != flux.Length)
        {
            return Error.Validation(
                "timeseries.detrend.length_mismatch",
                $"time length ({time.Length}) must equal flux length ({flux.Length}).");
        }

        if (time.IsEmpty)
        {
            return Error.Validation("timeseries.detrend.empty_series", "The light curve contains no points to detrend.");
        }

        return method.Trim().ToLowerInvariant() switch
        {
            "linear" => DetrendLinear(time, flux),
            "median" => DetrendMovingMedian(flux),
            _ => Error.Validation(
                "timeseries.detrend.unknown_method",
                $"Unknown detrend method '{method}'. Supported methods: 'linear', 'median'."),
        };
    }

    private static Result<double[]> DetrendLinear(ReadOnlySpan<double> time, ReadOnlySpan<double> flux)
    {
        var meanTime = Mean(time);

        var meanFlux = Mean(flux);

        var sumTimeSquaredDeviation = 0.0;

        var sumTimeFluxDeviation = 0.0;

        for (var i = 0; i < time.Length; i++)
        {
            var timeDeviation = time[i] - meanTime;

            sumTimeSquaredDeviation += timeDeviation * timeDeviation;

            sumTimeFluxDeviation += timeDeviation * (flux[i] - meanFlux);
        }

        if (sumTimeSquaredDeviation <= 0.0)
        {
            return Error.Validation(
                "timeseries.detrend.constant_time_values", "All time values are identical; a linear trend cannot be fit.");
        }

        var slope = sumTimeFluxDeviation / sumTimeSquaredDeviation;

        var intercept = meanFlux - (slope * meanTime);

        var detrended = new double[flux.Length];

        for (var i = 0; i < flux.Length; i++)
        {
            detrended[i] = flux[i] - ((slope * time[i]) + intercept);
        }

        return detrended;
    }

    private static Result<double[]> DetrendMovingMedian(ReadOnlySpan<double> flux)
    {
        if (flux.Length < MinimumPointsForMovingMedian)
        {
            return Error.Validation(
                "timeseries.detrend.series_too_short",
                $"At least {MinimumPointsForMovingMedian} points are required for moving-median detrending.");
        }

        var halfWindow = MovingMedianWindowPoints / 2;

        var detrended = new double[flux.Length];

        for (var i = 0; i < flux.Length; i++)
        {
            var windowStart = Math.Max(0, i - halfWindow);

            var windowEnd = Math.Min(flux.Length - 1, i + halfWindow);

            var window = flux[windowStart..(windowEnd + 1)].ToArray();

            Array.Sort(window);

            var median = window.Length % 2 == 1
                ? window[window.Length / 2]
                : (window[(window.Length / 2) - 1] + window[window.Length / 2]) / 2.0;

            detrended[i] = flux[i] - median;
        }

        return detrended;
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
}
