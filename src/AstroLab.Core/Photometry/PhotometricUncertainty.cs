using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure propagation of aperture-photometry flux uncertainty and signal-to-noise ratio. When a
/// detector gain is supplied, uncertainty follows the CCD equation (source shot noise + background
/// noise, all in ADU). Without a gain, only the background noise term is available.
/// <paramref name="skyBackgroundSigma"/> is expected to be measured directly from the image's
/// background pixels (e.g. <see cref="Imaging.ImageStatistics.ComputeSkyBackground"/>), so it
/// already reflects every noise source physically present there, including read noise. An optional
/// <paramref name="readNoiseElectrons"/> is therefore subtracted from the measured background
/// variance (in quadrature) before being added back as its own term, so a caller who also knows the
/// detector's read noise does not have it counted twice. When the number of pixels the local
/// background was estimated from is known, the per-pixel background variance is inflated by
/// (1 + n_pix/n_B) to include the error of that background estimate itself (Merline &amp; Howell
/// 1995; Howell 2006, §4.4), which is otherwise omitted and understates the uncertainty whenever the
/// background annulus is not much larger than the aperture.
/// </summary>
public static class PhotometricUncertainty
{
    private const string GainKeyword = "GAIN";

    public static double? ReadDetectorGain(FitsHeader header)
    {
        var gainResult = header.GetReal(GainKeyword);

        return gainResult.IsSuccess && gainResult.Value > 0.0 && double.IsFinite(gainResult.Value) ? gainResult.Value : null;
    }

    public static Result<double> EstimateFluxUncertainty(
        NetFluxMeasurement measurement, double skyBackgroundSigma, double? detectorGain = null, double? readNoiseElectrons = null) =>
        EstimateFluxUncertainty(
            measurement.NetFlux, measurement.ApertureArea, skyBackgroundSigma, detectorGain, readNoiseElectrons, measurement.BackgroundPixelCount);

    public static Result<double> EstimateFluxUncertainty(
        double netFlux,
        double apertureArea,
        double skyBackgroundSigma,
        double? detectorGain = null,
        double? readNoiseElectrons = null,
        int? backgroundPixelCount = null)
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

        if (backgroundPixelCount is <= 0)
        {
            return Error.Validation(
                "photometry.uncertainty.invalid_background_pixel_count", "backgroundPixelCount must be positive when supplied.");
        }

        var backgroundEstimateFactor = backgroundPixelCount is { } backgroundPixels ? 1.0 + apertureArea / backgroundPixels : 1.0;

        var measuredBackgroundVarianceAdu = apertureArea * skyBackgroundSigma * skyBackgroundSigma;

        if (detectorGain is not { } gain)
        {
            return Math.Sqrt(measuredBackgroundVarianceAdu * backgroundEstimateFactor);
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

        var readNoiseVarianceAdu = readNoiseElectrons is { } rn ? apertureArea * (rn * rn) / (gain * gain) : 0.0;

        var skyOnlyVarianceAdu = Math.Max(measuredBackgroundVarianceAdu - readNoiseVarianceAdu, 0.0);

        return Math.Sqrt(sourceShotVarianceAdu + (skyOnlyVarianceAdu + readNoiseVarianceAdu) * backgroundEstimateFactor);
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
