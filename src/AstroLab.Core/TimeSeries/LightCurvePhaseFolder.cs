using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure light-curve phase folding: maps each observation time onto a repeating phase cycle
/// [0, 1) for a supplied period and reference epoch, so that periodic behavior (transits,
/// pulsations, eclipses) collapses onto a single cycle when flux is plotted against phase
/// instead of time.
/// </summary>
public static class LightCurvePhaseFolder
{
    public static Result<Unit> Fold(ReadOnlySpan<double> time, ReadOnlySpan<double> flux, double period, double referenceEpoch, Span<double> phase)
    {
        if (time.Length != flux.Length)
        {
            return Error.Validation(
                "timeseries.phasefold.length_mismatch",
                $"time length ({time.Length}) must equal flux length ({flux.Length}).");
        }

        if (time.IsEmpty)
        {
            return Error.Validation("timeseries.phasefold.empty_series", "The light curve contains no points to phase-fold.");
        }

        if (phase.Length != time.Length)
        {
            return Error.Validation(
                "timeseries.phasefold.output_length_mismatch",
                $"phase length ({phase.Length}) must equal time length ({time.Length}).");
        }

        if (period <= 0.0 || !double.IsFinite(period))
        {
            return Error.Validation("timeseries.phasefold.invalid_period", "period must be a finite, positive value.");
        }

        if (!double.IsFinite(referenceEpoch))
        {
            return Error.Validation("timeseries.phasefold.invalid_reference_epoch", "referenceEpoch must be a finite value.");
        }

        for (var i = 0; i < time.Length; i++)
        {
            var cycles = (time[i] - referenceEpoch) / period;

            var fractional = cycles - Math.Floor(cycles);

            phase[i] = fractional;
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
