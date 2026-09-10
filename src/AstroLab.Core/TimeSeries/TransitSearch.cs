using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure Box Least Squares (BLS; Kovacs, Zucker &amp; Mazeh 2002) transit search over a
/// median-normalized flux series: phase-folds the series at each trial period into a fixed number of
/// phase bins, then evaluates every contiguous, non phase-wrapping bin range as a candidate transit
/// "box" using cumulative bin sums, selecting the period/box combination with the strongest
/// depth-significance (depth weighted by the square root of the in-transit sample count). Reports the
/// transit's period, fractional depth, duration, and the mid-transit epoch of its first occurrence at
/// or after the start of the series. Transits that straddle the phase-zero wrap point are not
/// considered, and the significance statistic is a simplified ranking score rather than the full BLS
/// signal-to-pink-noise detection statistic.
/// </summary>
public static class TransitSearch
{
    private const int PhaseBins = 50;
    private const int PeriodGridSize = 500;
    private const double MaxDurationFraction = 0.3;
    private const int MinimumPoints = 10;

    public static Result<(double Period, double Depth, double Duration, double Epoch)> Search(
        ReadOnlySpan<double> time, ReadOnlySpan<double> flux, double minPeriod, double maxPeriod, double minTransitDepth)
    {
        if (time.Length != flux.Length)
        {
            return Error.Validation(
                "timeseries.transit.length_mismatch", $"time length ({time.Length}) must equal flux length ({flux.Length}).");
        }

        if (time.Length < MinimumPoints)
        {
            return Error.Validation(
                "timeseries.transit.series_too_short", $"At least {MinimumPoints} points are required to search for transits.");
        }

        if (minPeriod <= 0.0 || !double.IsFinite(minPeriod))
        {
            return Error.Validation("timeseries.transit.invalid_min_period", "minPeriod must be a finite, positive value.");
        }

        if (maxPeriod <= minPeriod || !double.IsFinite(maxPeriod))
        {
            return Error.Validation("timeseries.transit.invalid_max_period", "maxPeriod must be finite and greater than minPeriod.");
        }

        var median = Median(flux);

        if (median <= 0.0)
        {
            return Error.Validation(
                "timeseries.transit.non_positive_median_flux",
                "The flux series has a non-positive median and cannot be normalized for a box search.");
        }

        var minTime = time[0];

        foreach (var t in time)
        {
            minTime = Math.Min(minTime, t);
        }

        var normalized = new double[flux.Length];

        var totalSum = 0.0;

        for (var i = 0; i < flux.Length; i++)
        {
            normalized[i] = flux[i] / median;

            totalSum += normalized[i];
        }

        var totalCount = normalized.Length;

        var maxDurationBins = Math.Max(1, (int)(PhaseBins * MaxDurationFraction));

        Span<double> binSum = stackalloc double[PhaseBins];

        Span<int> binCount = stackalloc int[PhaseBins];

        var bestScore = -1.0;

        var bestPeriod = 0.0;

        var bestDepth = 0.0;

        var bestStartBin = 0;

        var bestEndBin = 0;

        var periodStep = (maxPeriod - minPeriod) / (PeriodGridSize - 1);

        for (var periodIndex = 0; periodIndex < PeriodGridSize; periodIndex++)
        {
            var period = minPeriod + periodIndex * periodStep;

            binSum.Clear();

            binCount.Clear();

            for (var i = 0; i < time.Length; i++)
            {
                var phase = (time[i] - minTime) / period;

                phase -= Math.Floor(phase);

                var bin = Math.Clamp((int)(phase * PhaseBins), 0, PhaseBins - 1);

                binSum[bin] += normalized[i];

                binCount[bin]++;
            }

            for (var start = 0; start < PhaseBins; start++)
            {
                var runningSum = 0.0;

                var runningCount = 0;

                var lastEnd = Math.Min(PhaseBins - 1, start + maxDurationBins - 1);

                for (var end = start; end <= lastEnd; end++)
                {
                    runningSum += binSum[end];

                    runningCount += binCount[end];

                    if (runningCount == 0)
                    {
                        continue;
                    }

                    var outCount = totalCount - runningCount;

                    var outSum = totalSum - runningSum;

                    if (outCount == 0)
                    {
                        continue;
                    }

                    var outMean = outSum / outCount;

                    if (outMean <= 0.0)
                    {
                        continue;
                    }

                    var inMean = runningSum / runningCount;

                    var depth = (outMean - inMean) / outMean;

                    if (depth <= 0.0)
                    {
                        continue;
                    }

                    var score = depth * Math.Sqrt(runningCount);

                    if (score > bestScore)
                    {
                        bestScore = score;

                        bestPeriod = period;

                        bestDepth = depth;

                        bestStartBin = start;

                        bestEndBin = end;
                    }
                }
            }
        }

        if (bestScore < 0.0)
        {
            return Error.NotFound(
                "timeseries.transit.no_signal_detected", "No box-shaped dip was found across the requested period range.");
        }

        if (bestDepth < minTransitDepth)
        {
            return Error.NotFound(
                "timeseries.transit.below_depth_threshold",
                $"The strongest candidate transit depth ({bestDepth:G6}) is below the requested minimum ({minTransitDepth:G6}).");
        }

        var duration = (bestEndBin - bestStartBin + 1) / (double)PhaseBins * bestPeriod;

        var midPhase = (bestStartBin + bestEndBin + 1) / 2.0 / PhaseBins;

        var epoch = minTime + (midPhase * bestPeriod);

        return (bestPeriod, bestDepth, duration, epoch);
    }

    private static double Median(ReadOnlySpan<double> values)
    {
        var sorted = values.ToArray();

        Array.Sort(sorted);

        var mid = sorted.Length / 2;

        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
