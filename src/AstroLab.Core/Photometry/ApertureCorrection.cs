using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure application of a multiplicative aperture correction (compensating for source flux that
/// falls outside a finite measurement aperture) to a measured flux and its optional uncertainty.
/// </summary>
public static class ApertureCorrection
{
    public static Result<(double CorrectedFlux, double? CorrectedFluxUncertainty)> Apply(
        double measuredFlux, double correctionFactor, double? measuredFluxUncertainty)
    {
        if (!double.IsFinite(measuredFlux))
        {
            return Error.Validation("photometry.aperturecorrection.invalid_flux", "measuredFlux must be finite.");
        }

        if (correctionFactor <= 0.0 || !double.IsFinite(correctionFactor))
        {
            return Error.Validation("photometry.aperturecorrection.invalid_factor", "correctionFactor must be a finite, positive value.");
        }

        if (measuredFluxUncertainty is { } uncertainty && (uncertainty < 0.0 || !double.IsFinite(uncertainty)))
        {
            return Error.Validation("photometry.aperturecorrection.invalid_uncertainty", "measuredFluxUncertainty must be finite and non-negative.");
        }

        var correctedFlux = measuredFlux * correctionFactor;

        var correctedFluxUncertainty = measuredFluxUncertainty * correctionFactor;

        if (!double.IsFinite(correctedFlux) || correctedFluxUncertainty is { } correctedUncertainty && !double.IsFinite(correctedUncertainty))
        {
            return Error.Validation(
                "photometry.aperturecorrection.overflow", "The aperture-corrected flux overflowed to a non-finite value.");
        }

        return (correctedFlux, correctedFluxUncertainty);
    }
}
