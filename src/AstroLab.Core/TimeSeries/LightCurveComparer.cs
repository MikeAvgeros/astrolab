using AstroLab.Core.Result;

namespace AstroLab.Core.TimeSeries;

/// <summary>
/// Pure comparison of two paired flux series (assumed already time-aligned sample-for-sample):
/// their Pearson correlation coefficient, the mean instrumental-magnitude offset between them
/// (using <see cref="Photometry.InstrumentalPhotometry.DefaultZeroPoint"/>, which cancels out in
/// the pairwise difference regardless of its value), the ratio of their mean flux levels, and the
/// ratio of their flux standard deviations (a simple variability comparison).
/// </summary>
public static class LightCurveComparer
{
    private const double MagnitudeScaleFactor = 2.5;

    public static Result<(double CorrelationCoefficient, double MeanMagnitudeDifference, double FluxRatio, double VariabilityRatio)> Compare(
        ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB)
    {
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

        if (meanB == 0.0)
        {
            return Error.Validation(
                "timeseries.compare.zero_mean_flux", "The comparison light curve has zero mean flux, so a flux ratio is undefined.");
        }

        var correlation = covariance / Math.Sqrt(varianceA * varianceB);

        var fluxRatio = meanA / meanB;

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
