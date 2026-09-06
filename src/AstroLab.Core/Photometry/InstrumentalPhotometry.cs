using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure conversions between net aperture flux and instrumental magnitude, and their propagated
/// uncertainties. An instrumental magnitude has no absolute physical zero point; callers that
/// don't carry one of their own (e.g. differential photometry, where any common zero point cancels
/// out in the subtraction) use <see cref="DefaultZeroPoint"/>.
/// </summary>
public static class InstrumentalPhotometry
{
    public const double DefaultZeroPoint = 0.0;

    private const double MagnitudeScaleFactor = 2.5;

    public static double EstimateFluxUncertainty(double skyBackgroundSigma, double apertureArea) =>
        skyBackgroundSigma * Math.Sqrt(apertureArea);

    public static Result<(double Magnitude, double MagnitudeUncertainty)> ComputeMagnitude(
        double netFlux, double fluxUncertainty, double zeroPoint)
    {
        if (netFlux <= 0.0 || !double.IsFinite(netFlux))
        {
            return Error.Validation(
                "photometry.non_positive_net_flux", "Instrumental magnitude requires a positive net flux.");
        }

        var magnitude = zeroPoint - (MagnitudeScaleFactor * Math.Log10(netFlux));

        var magnitudeUncertainty = (MagnitudeScaleFactor / Math.Log(10.0)) * (fluxUncertainty / netFlux);

        return (magnitude, magnitudeUncertainty);
    }

    public static (double DifferentialMagnitude, double Uncertainty) ComputeDifferentialMagnitude(
        double targetMagnitude, double targetMagnitudeUncertainty, double comparisonMagnitude, double comparisonMagnitudeUncertainty)
    {
        var differential = targetMagnitude - comparisonMagnitude;

        var uncertainty = Math.Sqrt(
            (targetMagnitudeUncertainty * targetMagnitudeUncertainty) + (comparisonMagnitudeUncertainty * comparisonMagnitudeUncertainty));

        return (differential, uncertainty);
    }
}
