using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure (non-generalized) Lomb-Scargle periodogram (Press &amp; Rybicki 1989 formulation) for
/// detecting periodic signals in an unevenly-sampled, already-detrended flux series. Searches a
/// linear grid of trial periods between <c>minPeriod</c> and <c>maxPeriod</c> and reports the period
/// whose normalized power is highest.
/// </summary>
public static class LombScarglePeriodogram
{
    public const int DefaultGridSize = 2000;

    private const int MinimumPoints = 3;
    private const double NyquistCadenceFactor = 2.0;
    private const double MaxPeriodBaselineFactor = 0.5;

    public static Result<(double BestPeriod, double Power)> Search(
        ReadOnlySpan<double> time, ReadOnlySpan<double> flux, double minPeriod, double maxPeriod, int gridSize = DefaultGridSize)
    {
        if (time.Length != flux.Length)
        {
            return Error.Validation(
                "timeseries.periodsearch.length_mismatch",
                $"time length ({time.Length}) must equal flux length ({flux.Length}).");
        }

        if (time.Length < MinimumPoints)
        {
            return Error.Validation(
                "timeseries.periodsearch.series_too_short",
                $"At least {MinimumPoints} points are required to search for periodicity.");
        }

        if (minPeriod <= 0.0 || !double.IsFinite(minPeriod))
        {
            return Error.Validation("timeseries.periodsearch.invalid_min_period", "minPeriod must be a finite, positive value.");
        }

        if (maxPeriod <= minPeriod || !double.IsFinite(maxPeriod))
        {
            return Error.Validation(
                "timeseries.periodsearch.invalid_max_period", "maxPeriod must be finite and greater than minPeriod.");
        }

        if (gridSize < 2)
        {
            return Error.Validation("timeseries.periodsearch.invalid_grid_size", "gridSize must be at least 2.");
        }

        var mean = Mean(flux);

        var variance = 0.0;

        foreach (var value in flux)
        {
            var deviation = value - mean;

            variance += deviation * deviation;
        }

        if (variance <= 0.0)
        {
            return Error.Validation(
                "timeseries.periodsearch.constant_flux", "The flux series is constant, so no periodic signal can be detected.");
        }

        var bestPeriod = minPeriod;

        var bestPower = -1.0;

        var periodStep = (maxPeriod - minPeriod) / (gridSize - 1);

        for (var k = 0; k < gridSize; k++)
        {
            var period = minPeriod + (k * periodStep);

            var power = ComputePower(time, flux, mean, variance, period);

            if (double.IsFinite(power) && power > bestPower)
            {
                bestPower = power;

                bestPeriod = period;
            }
        }

        return (bestPeriod, Math.Max(bestPower, 0.0));
    }
    
    public static Result<(double MinPeriod, double MaxPeriod)> SuggestPeriodRange(ReadOnlySpan<double> time)
    {
        if (time.Length < MinimumPoints)
        {
            return Error.Validation(
                "timeseries.periodsearch.series_too_short",
                $"At least {MinimumPoints} points are required to suggest a period search range.");
        }

        var sorted = time.ToArray();

        Array.Sort(sorted);

        var baseline = sorted[^1] - sorted[0];

        if (baseline <= 0.0)
        {
            return Error.Validation(
                "timeseries.periodsearch.zero_baseline", "The time series spans no duration; a period range cannot be suggested.");
        }

        var gaps = new double[sorted.Length - 1];

        for (var i = 1; i < sorted.Length; i++)
        {
            gaps[i - 1] = sorted[i] - sorted[i - 1];
        }

        Array.Sort(gaps);

        var medianCadence = gaps.Length % 2 == 1
            ? gaps[gaps.Length / 2]
            : (gaps[(gaps.Length / 2) - 1] + gaps[gaps.Length / 2]) / 2.0;

        if (medianCadence <= 0.0)
        {
            return Error.Validation(
                "timeseries.periodsearch.zero_cadence", "The time series has no positive cadence; a period range cannot be suggested.");
        }

        var minPeriod = NyquistCadenceFactor * medianCadence;

        var maxPeriod = MaxPeriodBaselineFactor * baseline;

        if (maxPeriod <= minPeriod)
        {
            return Error.Validation(
                "timeseries.periodsearch.range_too_narrow",
                "The time series is too sparsely or briefly sampled to suggest a usable period range.");
        }

        return (minPeriod, maxPeriod);
    }

    private static double ComputePower(ReadOnlySpan<double> time, ReadOnlySpan<double> flux, double mean, double variance, double period)
    {
        var angularFrequency = 2.0 * Math.PI / period;

        var sumSin2Wt = 0.0;

        var sumCos2Wt = 0.0;

        foreach (var t in time)
        {
            sumSin2Wt += Math.Sin(2.0 * angularFrequency * t);

            sumCos2Wt += Math.Cos(2.0 * angularFrequency * t);
        }

        var tau = Math.Atan2(sumSin2Wt, sumCos2Wt) / (2.0 * angularFrequency);

        var sumC = 0.0;

        var sumS = 0.0;

        var sumCc = 0.0;

        var sumSs = 0.0;

        for (var i = 0; i < time.Length; i++)
        {
            var phase = angularFrequency * (time[i] - tau);

            var c = Math.Cos(phase);

            var s = Math.Sin(phase);

            var deviation = flux[i] - mean;

            sumC += deviation * c;

            sumS += deviation * s;

            sumCc += c * c;

            sumSs += s * s;
        }

        if (sumCc <= 0.0 || sumSs <= 0.0)
        {
            return 0.0;
        }

        return 0.5 * (sumC * sumC / sumCc + sumS * sumS / sumSs) / variance;
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
