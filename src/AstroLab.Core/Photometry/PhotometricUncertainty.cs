using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure propagation of aperture-photometry flux uncertainty and signal-to-noise ratio. When a
/// detector gain (and optionally read noise) is supplied, uncertainty follows the standard CCD
/// equation (source shot noise + sky-background noise + read noise, all in ADU). Without a gain,
/// this falls back to the sky-noise-dominated estimate already used elsewhere in the codebase
/// (<see cref="InstrumentalPhotometry.EstimateFluxUncertainty"/>).
/// </summary>
public static class PhotometricUncertainty
{
    public static Result<double> EstimateFluxUncertainty(
        double netFlux, double apertureArea, double skyBackgroundSigma, double? detectorGain = null, double? readNoiseElectrons = null)
    {
        if (apertureArea <= 0.0 || !double.IsFinite(apertureArea))
        {
            return Error.Validation("photometry.uncertainty.invalid_aperture_area", "apertureArea must be a finite, positive value.");
        }

        if (skyBackgroundSigma < 0.0 || !double.IsFinite(skyBackgroundSigma))
        {
            return Error.Validation(
                "photometry.uncertainty.invalid_sky_background_sigma", "skyBackgroundSigma must be a finite, non-negative value.");
        }

        if (detectorGain is not { } gain)
        {
            return InstrumentalPhotometry.EstimateFluxUncertainty(skyBackgroundSigma, apertureArea);
        }

        if (gain <= 0.0 || !double.IsFinite(gain))
        {
            return Error.Validation("photometry.uncertainty.invalid_gain", "detectorGain must be a finite, positive value.");
        }

        if (readNoiseElectrons is { } readNoise && (readNoise < 0.0 || !double.IsFinite(readNoise)))
        {
            return Error.Validation(
                "photometry.uncertainty.invalid_read_noise", "readNoiseElectrons must be a finite, non-negative value.");
        }

        var sourceShotVarianceAdu = Math.Max(netFlux, 0.0) / gain;

        var skyVarianceAdu = apertureArea * skyBackgroundSigma * skyBackgroundSigma;

        var readNoiseVarianceAdu = readNoiseElectrons is { } rn ? apertureArea * (rn * rn) / (gain * gain) : 0.0;

        return Math.Sqrt(sourceShotVarianceAdu + skyVarianceAdu + readNoiseVarianceAdu);
    }

    public static Result<double> ComputeSignalToNoiseRatio(double netFlux, double fluxUncertainty)
    {
        if (fluxUncertainty <= 0.0 || !double.IsFinite(fluxUncertainty))
        {
            return Error.Validation(
                "photometry.snr.invalid_flux_uncertainty", "fluxUncertainty must be a finite, positive value.");
        }

        return netFlux / fluxUncertainty;
    }
}
