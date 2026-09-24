using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure light-curve detrending: removes a slowly-varying baseline from a flux series so that
/// shorter-timescale variability (transits, pulsations, flares) stands out. Supports a "linear"
/// method (subtracts the least-squares best-fit line) and a "median" method (subtracts a moving
/// median over a caller-supplied time window centred on each sample, edge-truncated at the series
/// boundaries). The median window is a duration in the light curve's own time units rather than a
/// sample count, so it means the same thing across cadences and data gaps; it must be several times
/// longer than any feature to be preserved, since a feature longer than about half the window is
/// absorbed into the running median and removed along with the trend.
/// </summary>
public static class LightCurveDetrender
{
    private const int MinimumPointsForMovingMedian = 3;

    public static Result<double[]> Detrend(ReadOnlySpan<double> time, ReadOnlySpan<double> flux, string method, double? windowDuration = null)
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

        for (var i = 0; i < time.Length; i++)
        {
            if (!double.IsFinite(time[i]) || !double.IsFinite(flux[i]))
            {
                return Error.Validation("timeseries.detrend.non_finite_value", "Time and flux values must be finite.");
            }
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            return Error.Validation("timeseries.detrend.missing_method", "A detrend method must be specified.");
        }

        return method.Trim().ToLowerInvariant() switch
        {
            "linear" => DetrendLinear(time, flux),
            "median" => DetrendMovingMedian(time, flux, windowDuration),
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

    private static Result<double[]> DetrendMovingMedian(ReadOnlySpan<double> time, ReadOnlySpan<double> flux, double? windowDuration)
    {
        if (windowDuration is not { } window || window <= 0.0 || !double.IsFinite(window))
        {
            return Error.Validation(
                "timeseries.detrend.invalid_window_duration",
                "Moving-median detrending requires a finite, positive windowDuration in the light curve's time units.");
        }

        if (flux.Length < MinimumPointsForMovingMedian)
        {
            return Error.Validation(
                "timeseries.detrend.series_too_short",
                $"At least {MinimumPointsForMovingMedian} points are required for moving-median detrending.");
        }
        
        for (var i = 1; i < time.Length; i++)
        {
            if (time[i] < time[i - 1])
            {
                return Error.Validation(
                    "timeseries.detrend.unsorted_time",
                    "Moving-median detrending requires the time values to be sorted in non-decreasing order.");
            }
        }

        var halfWindow = window / 2.0;

        var detrended = new double[flux.Length];

        var windowBuffer = new double[flux.Length];

        var windowStart = 0;

        var windowEnd = 0;

        for (var i = 0; i < flux.Length; i++)
        {
            while (time[windowStart] < time[i] - halfWindow)
            {
                windowStart++;
            }

            windowEnd = Math.Max(windowEnd, i);

            while (windowEnd + 1 < flux.Length && time[windowEnd + 1] <= time[i] + halfWindow)
            {
                windowEnd++;
            }

            var windowLength = windowEnd - windowStart + 1;

            var activeWindow = windowBuffer.AsSpan(0, windowLength);

            flux[windowStart..(windowEnd + 1)].CopyTo(activeWindow);

            activeWindow.Sort();

            var median = windowLength % 2 == 1
                ? activeWindow[windowLength / 2]
                : (activeWindow[windowLength / 2 - 1] + activeWindow[windowLength / 2]) / 2.0;

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
