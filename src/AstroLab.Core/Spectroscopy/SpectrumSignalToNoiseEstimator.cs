using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Estimates a representative signal-to-noise ratio for a 1D flux spectrum, reusing the same robust
/// median/inter-quartile-range noise estimate <see cref="SpectralLineDetector"/> uses for continuum
/// detection. The overall SNR is the robust median flux divided by the noise sigma (a single
/// representative value for the whole spectrum); the per-sample SNR is each bin's flux divided by
/// that same noise sigma (a per-bin z-score against the noise floor, signed the same way the flux is).
/// </summary>
public static class SpectrumSignalToNoiseEstimator
{
    public static Result<(double NoiseSigma, double OverallSnr, double[] PerSampleSnr)> Estimate(ReadOnlySpan<double> spectrum)
    {
        if (spectrum.IsEmpty)
        {
            return Error.Validation("spectroscopy.snr.empty_spectrum", "The spectrum contains no bins to estimate a signal-to-noise ratio from.");
        }

        var (median, sigma) = RobustSpectrumStatistics.Compute(spectrum);

        if (!double.IsFinite(sigma) || sigma <= 0.0)
        {
            return Error.Validation(
                "spectroscopy.snr.indeterminate_noise",
                "The spectrum shows no measurable inter-quartile spread; a noise level (and therefore an SNR) cannot be estimated.");
        }

        var overallSnr = Math.Abs(median) / sigma;

        var perSampleSnr = new double[spectrum.Length];

        for (var i = 0; i < spectrum.Length; i++)
        {
            perSampleSnr[i] = spectrum[i] / sigma;
        }

        return (sigma, overallSnr, perSampleSnr);
    }
}
