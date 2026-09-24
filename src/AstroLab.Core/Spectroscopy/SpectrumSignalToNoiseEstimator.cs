using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Estimates a representative signal-to-noise ratio for a 1D flux spectrum with the DER_SNR
/// algorithm (Stoehr et al. 2008): the signal is the median flux and the noise is
/// 1.482602/√6 · median(|2f[i] − f[i−2] − f[i+2]|), a second-difference estimator that is insensitive
/// to the continuum's shape (a smooth slope or curvature contributes nothing) and robust to isolated
/// lines, unlike a spread-of-flux estimator that mistakes the continuum shape for noise. The per-sample
/// SNR is each bin's flux divided by that same noise sigma (a per-bin z-score against the noise floor,
/// signed the same way the flux is).
/// </summary>
public static class SpectrumSignalToNoiseEstimator
{
    private const int StencilHalfWidth = 2;
    private const int MinimumBins = 2 * StencilHalfWidth + 1;
    private const double DerSnrNoiseFactor = 1.482602;
    private const double DerSnrStencilVariance = 6.0;

    public static Result<(double NoiseSigma, double OverallSnr, double[] PerSampleSnr)> Estimate(ReadOnlySpan<double> spectrum)
    {
        if (spectrum.IsEmpty)
        {
            return Error.Validation("spectroscopy.snr.empty_spectrum", "The spectrum contains no bins to estimate a signal-to-noise ratio from.");
        }

        if (spectrum.Length < MinimumBins)
        {
            return Error.Validation(
                "spectroscopy.snr.insufficient_bins",
                $"At least {MinimumBins} bins are required to estimate the noise with the DER_SNR second-difference stencil.");
        }

        var sigma = EstimateDerSnrNoise(spectrum);

        if (!double.IsFinite(sigma) || sigma <= 0.0)
        {
            return Error.Validation(
                "spectroscopy.snr.indeterminate_noise",
                "The spectrum shows no measurable point-to-point scatter; a noise level (and therefore an SNR) cannot be estimated.");
        }

        var overallSnr = Math.Abs(Median(spectrum.ToArray())) / sigma;

        var perSampleSnr = new double[spectrum.Length];

        for (var i = 0; i < spectrum.Length; i++)
        {
            perSampleSnr[i] = spectrum[i] / sigma;
        }

        return (sigma, overallSnr, perSampleSnr);
    }

    private static double EstimateDerSnrNoise(ReadOnlySpan<double> spectrum)
    {
        var secondDifferences = new double[spectrum.Length - 2 * StencilHalfWidth];

        for (var i = StencilHalfWidth; i < spectrum.Length - StencilHalfWidth; i++)
        {
            secondDifferences[i - StencilHalfWidth] =
                Math.Abs(2.0 * spectrum[i] - spectrum[i - StencilHalfWidth] - spectrum[i + StencilHalfWidth]);
        }

        return DerSnrNoiseFactor / Math.Sqrt(DerSnrStencilVariance) * Median(secondDifferences);
    }

    private static double Median(double[] values)
    {
        Array.Sort(values);

        var mid = values.Length / 2;

        return values.Length % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
    }
}
