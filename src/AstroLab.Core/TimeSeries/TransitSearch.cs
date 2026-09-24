using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure Box Least Squares (BLS; Kovacs, Zucker &amp; Mazeh 2002) transit search over a
/// median-normalized flux series: phase-folds the series at each trial period into phase bins, then
/// evaluates every contiguous bin range (including ranges that wrap through phase zero) as a candidate
/// transit "box", selecting the period/box combination with the strongest depth-significance (depth
/// weighted by the square root of the in-transit sample count). Reports the transit's period,
/// fractional depth, duration, and the mid-transit epoch of its first occurrence at or after the start
/// of the series. The significance statistic is a simplified ranking score rather than the full BLS
/// signal-to-pink-noise detection statistic.
/// <para>
/// Sampling follows the data rather than fixed constants: each period uses phase bins about two
/// cadences wide (so short transits on long periods are resolved rather than smeared across a P/50
/// bin), and each step between trial frequencies is chosen so that the phase drift accumulated across
/// the whole baseline stays below half a phase bin at that period (a near-logarithmic grid, bounded to
/// 500-100,000 trials); otherwise individual transits in a long series fall out of alignment and the
/// folded dip is diluted or missed.
/// </para>
/// </summary>
public static class TransitSearch
{
    private const int MinPhaseBins = 50;
    private const int MaxPhaseBins = 500;
    private const int MinPeriodGridSize = 500;
    private const int MaxPeriodGridSize = 100_000;
    private const double MaxPhaseDriftBins = 0.5;
    private const double PhaseBinWidthCadences = 1.0;
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

        for (var i = 0; i < time.Length; i++)
        {
            if (!double.IsFinite(time[i]) || !double.IsFinite(flux[i]))
            {
                return Error.Validation("timeseries.transit.non_finite_value", "Time and flux values must be finite.");
            }
        }

        var median = Median(flux);

        if (median <= 0.0)
        {
            return Error.Validation(
                "timeseries.transit.non_positive_median_flux",
                "The flux series has a non-positive median and cannot be normalized for a box search.");
        }

        var (minTime, baseline, cadence) = DescribeSampling(time);

        if (baseline <= 0.0 || cadence <= 0.0)
        {
            return Error.Validation(
                "timeseries.transit.zero_baseline", "The time series spans no duration; a transit search cannot be performed.");
        }

        var normalized = new double[flux.Length];

        var totalSum = 0.0;

        for (var i = 0; i < flux.Length; i++)
        {
            normalized[i] = flux[i] / median;

            totalSum += normalized[i];
        }

        var minFrequency = 1.0 / maxPeriod;

        var frequencyRange = 1.0 / minPeriod - minFrequency;

        var largestFrequencyStep = frequencyRange / MinPeriodGridSize;

        var smallestFrequencyStep = frequencyRange / MaxPeriodGridSize;

        Span<double> binSum = stackalloc double[MaxPhaseBins];

        Span<int> binCount = stackalloc int[MaxPhaseBins];

        (double Score, double Depth, double Period, int PhaseBins, int StartBin, int WidthBins) best = (-1.0, 0.0, 0.0, 0, 0, 0);

        for (var frequency = 1.0 / minPeriod; frequency >= minFrequency; )
        {
            var period = 1.0 / frequency;

            var phaseBins = PhaseBinCount(period, cadence);

            frequency -= Math.Clamp(MaxPhaseDriftBins / (phaseBins * baseline), smallestFrequencyStep, largestFrequencyStep);

            FoldIntoBins(time, normalized, minTime, period, binSum[..phaseBins], binCount[..phaseBins]);

            var (score, depth, startBin, widthBins) = FindBestBox(binSum[..phaseBins], binCount[..phaseBins], totalSum, normalized.Length);

            if (score > best.Score)
            {
                best = (score, depth, period, phaseBins, startBin, widthBins);
            }
        }

        if (best.Score < 0.0)
        {
            return Error.NotFound(
                "timeseries.transit.no_signal_detected", "No box-shaped dip was found across the requested period range.");
        }

        if (best.Depth < minTransitDepth)
        {
            return Error.NotFound(
                "timeseries.transit.below_depth_threshold",
                $"The strongest candidate transit depth ({best.Depth:G6}) is below the requested minimum ({minTransitDepth:G6}).");
        }

        var duration = best.WidthBins / (double)best.PhaseBins * best.Period;

        var midPhase = (best.StartBin + best.WidthBins / 2.0) / best.PhaseBins;

        var epoch = minTime + (midPhase - Math.Floor(midPhase)) * best.Period;

        return (best.Period, best.Depth, duration, epoch);
    }

    private static (double MinTime, double Baseline, double Cadence) DescribeSampling(ReadOnlySpan<double> time)
    {
        var sorted = time.ToArray();

        Array.Sort(sorted);

        var gaps = new List<double>(sorted.Length - 1);

        for (var i = 1; i < sorted.Length; i++)
        {
            var gap = sorted[i] - sorted[i - 1];

            if (gap > 0.0)
            {
                gaps.Add(gap);
            }
        }

        if (gaps.Count == 0)
        {
            return (sorted[0], 0.0, 0.0);
        }

        gaps.Sort();

        var mid = gaps.Count / 2;

        var cadence = gaps.Count % 2 == 1 ? gaps[mid] : (gaps[mid - 1] + gaps[mid]) / 2.0;

        return (sorted[0], sorted[^1] - sorted[0], cadence);
    }

    private static int PhaseBinCount(double period, double cadence) =>
        (int)Math.Clamp(Math.Ceiling(period / (PhaseBinWidthCadences * cadence)), MinPhaseBins, MaxPhaseBins);

    private static void FoldIntoBins(
        ReadOnlySpan<double> time, ReadOnlySpan<double> normalized, double minTime, double period, Span<double> binSum, Span<int> binCount)
    {
        binSum.Clear();

        binCount.Clear();

        for (var i = 0; i < time.Length; i++)
        {
            var phase = (time[i] - minTime) / period;

            phase -= Math.Floor(phase);

            var bin = Math.Clamp((int)(phase * binSum.Length), 0, binSum.Length - 1);

            binSum[bin] += normalized[i];

            binCount[bin]++;
        }
    }

    private static (double Score, double Depth, int StartBin, int WidthBins) FindBestBox(
        ReadOnlySpan<double> binSum, ReadOnlySpan<int> binCount, double totalSum, int totalCount)
    {
        var phaseBins = binSum.Length;

        var maxWidthBins = Math.Max(1, (int)(phaseBins * MaxDurationFraction));

        (double Score, double Depth, int StartBin, int WidthBins) best = (-1.0, 0.0, 0, 0);

        for (var start = 0; start < phaseBins; start++)
        {
            var runningSum = 0.0;

            var runningCount = 0;

            for (var width = 1; width <= maxWidthBins; width++)
            {
                var bin = (start + width - 1) % phaseBins;

                runningSum += binSum[bin];

                runningCount += binCount[bin];

                var outCount = totalCount - runningCount;

                if (runningCount == 0 || outCount == 0)
                {
                    continue;
                }

                var outMean = (totalSum - runningSum) / outCount;

                if (outMean <= 0.0)
                {
                    continue;
                }

                var depth = (outMean - runningSum / runningCount) / outMean;

                if (depth <= 0.0)
                {
                    continue;
                }

                var score = depth * Math.Sqrt(runningCount);

                if (score > best.Score)
                {
                    best = (score, depth, start, width);
                }
            }
        }

        return best;
    }

    private static double Median(ReadOnlySpan<double> values)
    {
        var sorted = values.ToArray();

        Array.Sort(sorted);

        var mid = sorted.Length / 2;

        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
