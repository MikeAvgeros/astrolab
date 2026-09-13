namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure application of a multiplicative aperture correction (compensating for source flux that
/// falls outside a finite measurement aperture) to a measured flux and its optional uncertainty.
/// Both inputs are already validated at the request boundary (a positive, finite correction
/// factor), so this has no failure mode of its own.
/// </summary>
public static class ApertureCorrection
{
    public static (double CorrectedFlux, double? CorrectedFluxUncertainty) Apply(
        double measuredFlux, double correctionFactor, double? measuredFluxUncertainty) =>
        (measuredFlux * correctionFactor, measuredFluxUncertainty * correctionFactor);
}
