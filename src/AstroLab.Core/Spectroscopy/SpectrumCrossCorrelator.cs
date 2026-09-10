using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure spectral cross-correlation: aligning two flux series either by an integer pixel lag (for two
/// spectra extracted on the same instrument/dispersion setup) or by a redshift applied to a rest-frame
/// template's wavelength axis (for comparing an observed spectrum against a reference template). Both
/// searches maximize the Pearson correlation coefficient over their respective trial grids.
/// </summary>
public static class SpectrumCrossCorrelator
{
    public const int DefaultRedshiftGridSize = 500;

    private const int MinimumPoints = 5;
    private const int MinimumOverlapPoints = 5;
    private const double MaxLagFraction = 0.25;
    private const double Epsilon = 1e-12;
    
    public static Result<(double PeakCorrelation, double LagBins)> CorrelatePixelLag(
        ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB)
    {
        if (fluxA.Length != fluxB.Length)
        {
            return Error.Validation(
                "spectroscopy.compare.length_mismatch", $"fluxA length ({fluxA.Length}) must equal fluxB length ({fluxB.Length}).");
        }

        if (fluxA.Length < MinimumPoints)
        {
            return Error.Validation(
                "spectroscopy.compare.series_too_short", $"At least {MinimumPoints} points are required to cross-correlate.");
        }

        var maxLag = Math.Max(1, (int)(fluxA.Length * MaxLagFraction));

        var bestLag = 0;

        var bestCorrelation = double.NegativeInfinity;

        Span<double> correlationByLag = stackalloc double[2 * maxLag + 1];

        for (var lag = -maxLag; lag <= maxLag; lag++)
        {
            var correlation = CorrelateAtLag(fluxA, fluxB, lag);

            correlationByLag[lag + maxLag] = correlation;

            if (double.IsFinite(correlation) && correlation > bestCorrelation)
            {
                bestCorrelation = correlation;

                bestLag = lag;
            }
        }

        if (!double.IsFinite(bestCorrelation))
        {
            return Error.Validation(
                "spectroscopy.compare.no_overlap", "No lag produced enough overlapping, non-constant samples to correlate.");
        }

        var refinedLag = RefineLag(correlationByLag, bestLag, maxLag);

        return (bestCorrelation, refinedLag);
    }
    
    public static Result<(double Redshift, double PeakCorrelation)> CorrelateAgainstTemplate(
        ReadOnlySpan<double> observedWavelengths,
        ReadOnlySpan<double> observedFlux,
        ReadOnlySpan<double> templateWavelengths,
        ReadOnlySpan<double> templateFlux,
        double minRedshift,
        double maxRedshift,
        int gridSize = DefaultRedshiftGridSize)
    {
        if (observedWavelengths.Length != observedFlux.Length)
        {
            return Error.Validation(
                "spectroscopy.redshift.observed_length_mismatch",
                $"observedWavelengths length ({observedWavelengths.Length}) must equal observedFlux length ({observedFlux.Length}).");
        }

        if (templateWavelengths.Length != templateFlux.Length)
        {
            return Error.Validation(
                "spectroscopy.redshift.template_length_mismatch",
                $"templateWavelengths length ({templateWavelengths.Length}) must equal templateFlux length ({templateFlux.Length}).");
        }

        if (observedWavelengths.Length < MinimumPoints || templateWavelengths.Length < MinimumPoints)
        {
            return Error.Validation(
                "spectroscopy.redshift.series_too_short", $"At least {MinimumPoints} points are required in both spectra.");
        }

        if (!IsStrictlyIncreasing(observedWavelengths) || !IsStrictlyIncreasing(templateWavelengths))
        {
            return Error.Validation(
                "spectroscopy.redshift.wavelengths_not_increasing", "Wavelength arrays must be strictly increasing.");
        }

        if (maxRedshift <= minRedshift || !double.IsFinite(minRedshift) || !double.IsFinite(maxRedshift))
        {
            return Error.Validation(
                "spectroscopy.redshift.invalid_range", "maxRedshift must be finite and greater than a finite minRedshift.");
        }

        if (gridSize < 2)
        {
            return Error.Validation("spectroscopy.redshift.invalid_grid_size", "gridSize must be at least 2.");
        }

        var bestRedshift = minRedshift;

        var bestCorrelation = double.NegativeInfinity;

        var step = (maxRedshift - minRedshift) / (gridSize - 1);

        var shiftedTemplateWavelengths = new double[templateWavelengths.Length];

        var interpolatedTemplateFlux = new double[observedFlux.Length];

        var overlapObservedFlux = new double[observedFlux.Length];

        for (var k = 0; k < gridSize; k++)
        {
            var redshift = minRedshift + k * step;

            for (var i = 0; i < templateWavelengths.Length; i++)
            {
                shiftedTemplateWavelengths[i] = templateWavelengths[i] * (1.0 + redshift);
            }

            var overlapCount = InterpolateOverlap(
                observedWavelengths, observedFlux, shiftedTemplateWavelengths, templateFlux, overlapObservedFlux, interpolatedTemplateFlux);

            if (overlapCount < MinimumOverlapPoints)
            {
                continue;
            }

            var correlation = PearsonCorrelation(overlapObservedFlux.AsSpan(0, overlapCount), interpolatedTemplateFlux.AsSpan(0, overlapCount));

            if (double.IsFinite(correlation) && correlation > bestCorrelation)
            {
                bestCorrelation = correlation;

                bestRedshift = redshift;
            }
        }

        if (!double.IsFinite(bestCorrelation))
        {
            return Error.Validation(
                "spectroscopy.redshift.no_overlap",
                "No trial redshift produced enough overlap between the observed and template wavelength ranges.");
        }

        return (bestRedshift, bestCorrelation);
    }

    private static double CorrelateAtLag(ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB, int lag)
    {
        var start = Math.Max(0, -lag);

        var end = Math.Min(fluxA.Length, fluxA.Length - lag);

        var overlap = end - start;

        if (overlap < MinimumOverlapPoints)
        {
            return double.NaN;
        }

        Span<double> a = stackalloc double[overlap];

        Span<double> b = stackalloc double[overlap];

        for (var i = 0; i < overlap; i++)
        {
            a[i] = fluxA[start + i];

            b[i] = fluxB[start + i + lag];
        }

        return PearsonCorrelation(a, b);
    }

    private static double RefineLag(ReadOnlySpan<double> correlationByLag, int bestLag, int maxLag)
    {
        if (bestLag == -maxLag || bestLag == maxLag)
        {
            return bestLag;
        }

        var index = bestLag + maxLag;

        var left = correlationByLag[index - 1];

        var center = correlationByLag[index];

        var right = correlationByLag[index + 1];

        if (!double.IsFinite(left) || !double.IsFinite(right))
        {
            return bestLag;
        }

        var denominator = left - 2.0 * center + right;

        if (Math.Abs(denominator) < Epsilon)
        {
            return bestLag;
        }

        var offset = 0.5 * (left - right) / denominator;

        return bestLag + Math.Clamp(offset, -1.0, 1.0);
    }

    private static int InterpolateOverlap(
        ReadOnlySpan<double> observedWavelengths,
        ReadOnlySpan<double> observedFlux,
        ReadOnlySpan<double> shiftedTemplateWavelengths,
        ReadOnlySpan<double> templateFlux,
        Span<double> overlapObservedFlux,
        Span<double> interpolatedTemplateFlux)
    {
        var templateMin = shiftedTemplateWavelengths[0];

        var templateMax = shiftedTemplateWavelengths[^1];

        var searchStart = 0;

        var count = 0;

        for (var i = 0; i < observedWavelengths.Length; i++)
        {
            var wavelength = observedWavelengths[i];

            if (wavelength < templateMin || wavelength > templateMax)
            {
                continue;
            }

            while (searchStart < shiftedTemplateWavelengths.Length - 2 && shiftedTemplateWavelengths[searchStart + 1] < wavelength)
            {
                searchStart++;
            }

            var x0 = shiftedTemplateWavelengths[searchStart];

            var x1 = shiftedTemplateWavelengths[searchStart + 1];

            var y0 = templateFlux[searchStart];

            var y1 = templateFlux[searchStart + 1];

            var fraction = Math.Abs(x1 - x0) < Epsilon ? 0.0 : (wavelength - x0) / (x1 - x0);

            interpolatedTemplateFlux[count] = y0 + (fraction * (y1 - y0));

            overlapObservedFlux[count] = observedFlux[i];

            count++;
        }

        return count;
    }

    private static bool IsStrictlyIncreasing(ReadOnlySpan<double> values)
    {
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] <= values[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static double PearsonCorrelation(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
    {
        var meanA = Mean(a);

        var meanB = Mean(b);

        var covariance = 0.0;

        var varianceA = 0.0;

        var varianceB = 0.0;

        for (var i = 0; i < a.Length; i++)
        {
            var deviationA = a[i] - meanA;

            var deviationB = b[i] - meanB;

            covariance += deviationA * deviationB;

            varianceA += deviationA * deviationA;

            varianceB += deviationB * deviationB;
        }

        if (varianceA <= 0.0 || varianceB <= 0.0)
        {
            return double.NaN;
        }

        return covariance / Math.Sqrt(varianceA * varianceB);
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
