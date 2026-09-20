using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure comparison of two paired flux series that MUST already be time-aligned sample-for-sample
/// (e.g. simultaneous target/comparison-star photometry from the same exposures): their Pearson
/// correlation coefficient, the mean instrumental-magnitude offset between them
/// (using <see cref="Photometry.InstrumentalPhotometry.DefaultZeroPoint"/>, which cancels out in
/// the pairwise difference regardless of its value), the ratio of their mean flux levels, and the
/// ratio of their flux standard deviations (a simple variability comparison). The corresponding
/// time arrays are required and checked for exact alignment so that two unrelated light curves
/// that merely share a sample count are not silently compared index-for-index.
/// </summary>
public static class LightCurveComparer
{
    private const double MagnitudeScaleFactor = 2.5;

    public static Result<(double CorrelationCoefficient, double MeanMagnitudeDifference, double FluxRatio, double VariabilityRatio)> Compare(
        ReadOnlySpan<double> timeA, ReadOnlySpan<double> fluxA, ReadOnlySpan<double> timeB, ReadOnlySpan<double> fluxB)
    {
        if (timeA.Length != fluxA.Length)
        {
            return Error.Validation(
                "timeseries.compare.primary_length_mismatch",
                $"The primary light curve's time length ({timeA.Length}) must equal its flux length ({fluxA.Length}).");
        }

        if (timeB.Length != fluxB.Length)
        {
            return Error.Validation(
                "timeseries.compare.comparison_length_mismatch",
                $"The comparison light curve's time length ({timeB.Length}) must equal its flux length ({fluxB.Length}).");
        }

        if (fluxA.Length != fluxB.Length)
        {
            return Error.Validation(
                "timeseries.compare.length_mismatch",
                $"The two light curves have different sample counts ({fluxA.Length} vs {fluxB.Length}) and cannot be compared sample-for-sample.");
        }

        if (fluxA.IsEmpty)
        {
            return Error.Validation("timeseries.compare.empty_series", "The light curves contain no points to compare.");
        }

        for (var i = 0; i < timeA.Length; i++)
        {
            if (!double.IsFinite(timeA[i]) || !double.IsFinite(fluxA[i]) || !double.IsFinite(timeB[i]) || !double.IsFinite(fluxB[i]))
            {
                return Error.Validation("timeseries.compare.non_finite_value", "Time and flux values must be finite.");
            }

            if (timeA[i] != timeB[i])
            {
                return Error.Validation(
                    "timeseries.compare.time_misaligned",
                    $"The light curves are not time-aligned sample-for-sample: primary time {timeA[i]} does not match comparison time {timeB[i]} at index {i}.");
            }
        }

        var momentsResult = ComputeMoments(fluxA, fluxB);

        if (momentsResult.IsFailure)
        {
            return Result<(double, double, double, double)>.Failure(momentsResult.Error);
        }

        var (correlation, fluxRatio, variabilityRatio) = momentsResult.Value;

        var magnitudeDifferenceResult = ComputeMeanMagnitudeDifference(fluxA, fluxB);

        if (magnitudeDifferenceResult.IsFailure)
        {
            return Result<(double, double, double, double)>.Failure(magnitudeDifferenceResult.Error);
        }

        return (correlation, magnitudeDifferenceResult.Value, fluxRatio, variabilityRatio);
    }

    private static Result<(double Correlation, double FluxRatio, double VariabilityRatio)> ComputeMoments(
        ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB)
    {
        var meanA = Mean(fluxA);

        var meanB = Mean(fluxB);

        var covariance = 0.0;

        var varianceA = 0.0;

        var varianceB = 0.0;

        for (var i = 0; i < fluxA.Length; i++)
        {
            var deviationA = fluxA[i] - meanA;

            var deviationB = fluxB[i] - meanB;

            covariance += deviationA * deviationB;

            varianceA += deviationA * deviationA;

            varianceB += deviationB * deviationB;
        }

        if (varianceA <= 0.0 || varianceB <= 0.0)
        {
            return Error.Validation(
                "timeseries.compare.zero_variance", "One of the light curves is constant, so a correlation coefficient is undefined.");
        }

        var correlation = covariance / Math.Sqrt(varianceA * varianceB);

        var fluxRatio = meanA / meanB;

        if (!double.IsFinite(fluxRatio))
        {
            return Error.Validation(
                "timeseries.compare.zero_mean_flux",
                "The comparison light curve has a mean flux too close to zero, so a flux ratio is undefined.");
        }

        var variabilityRatio = Math.Sqrt(varianceA / varianceB);

        return (correlation, fluxRatio, variabilityRatio);
    }

    private static Result<double> ComputeMeanMagnitudeDifference(ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB)
    {
        var sum = 0.0;

        var count = 0;

        for (var i = 0; i < fluxA.Length; i++)
        {
            var a = fluxA[i];

            var b = fluxB[i];

            if (a <= 0.0 || b <= 0.0 || !double.IsFinite(a) || !double.IsFinite(b))
            {
                continue;
            }

            sum += -MagnitudeScaleFactor * Math.Log10(a / b);

            count++;
        }

        if (count == 0)
        {
            return Error.Validation(
                "timeseries.compare.no_positive_flux_pairs",
                "No paired samples had positive flux in both light curves; a magnitude difference is undefined.");
        }

        return sum / count;
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
