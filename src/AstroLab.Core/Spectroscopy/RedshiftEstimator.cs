using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure redshift estimation from paired observed/rest-frame spectral line wavelengths: the mean
/// fractional wavelength shift z = (observed - rest) / rest across all supplied line pairs, with
/// the standard error of that mean as an uncertainty estimate.
/// </summary>
public static class RedshiftEstimator
{
    private const int MinimumSampleSizeForUncertainty = 2;

    public static Result<(double Redshift, double Uncertainty)> Estimate(
        ReadOnlySpan<double> observedWavelengths, ReadOnlySpan<double> restWavelengths)
    {
        if (observedWavelengths.Length != restWavelengths.Length)
        {
            return Error.Validation(
                "spectroscopy.redshift.length_mismatch",
                $"observedWavelengths length ({observedWavelengths.Length}) must equal restWavelengths length ({restWavelengths.Length}).");
        }

        if (observedWavelengths.IsEmpty)
        {
            return Error.Validation(
                "spectroscopy.redshift.no_line_pairs", "At least one observed/rest wavelength pair is required.");
        }

        var shifts = new double[observedWavelengths.Length];

        for (var i = 0; i < observedWavelengths.Length; i++)
        {
            var observed = observedWavelengths[i];

            var rest = restWavelengths[i];

            if (!double.IsFinite(observed) || !double.IsFinite(rest))
            {
                return Error.Validation(
                    "spectroscopy.redshift.non_finite_wavelength", "Observed and rest wavelengths must be finite.");
            }

            if (rest <= 0.0)
            {
                return Error.Validation("spectroscopy.redshift.invalid_rest_wavelength", "Rest wavelengths must be positive.");
            }

            shifts[i] = (observed - rest) / rest;
        }

        var mean = Mean(shifts);

        var uncertainty = shifts.Length < MinimumSampleSizeForUncertainty ? 0.0 : StandardErrorOfMean(shifts, mean);

        return (mean, uncertainty);
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

    private static double StandardErrorOfMean(ReadOnlySpan<double> values, double mean)
    {
        var sumSquaredDeviations = 0.0;

        foreach (var value in values)
        {
            var deviation = value - mean;

            sumSquaredDeviations += deviation * deviation;
        }

        var sampleVariance = sumSquaredDeviations / (values.Length - 1);

        return Math.Sqrt(sampleVariance / values.Length);
    }
}
