using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure polynomial continuum fitting for a 1D spectrum: least-squares fits a polynomial of a given
/// degree against the flux, optionally excluding caller-supplied wavelength ranges (e.g. known
/// emission/absorption features) from the fit and/or iteratively sigma-clipping outliers, then
/// evaluates the fitted polynomial back across every original sample to produce a full continuum
/// array the same length as the input. The fit is carried out in the normalized variable
/// t = (x - centre) / half-range, which maps the wavelength range onto [-1, 1]: raw powers of optical
/// wavelengths (x^k with x ~ 5000) make the normal equations catastrophically ill-conditioned for
/// anything beyond a low degree. Returned coefficients are converted back to the raw power basis.
/// Does not mutate the input spectrum — see <see cref="SpectrumExtractor.SubtractBackground"/> for
/// continuum subtraction.
/// </summary>
public static class ContinuumFitter
{
    public const int MaxPolynomialDegree = 15;

    public static Result<(double[] Continuum, double[] Coefficients)> Fit(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> flux,
        int polynomialDegree,
        ReadOnlySpan<(double Min, double Max)> excludedRanges,
        double? sigmaClipThreshold,
        int? sigmaClipIterations)
    {
        if (x.Length != flux.Length)
        {
            return Error.Validation(
                "spectroscopy.continuum.length_mismatch", $"x length ({x.Length}) must equal flux length ({flux.Length}).");
        }

        if (x.IsEmpty)
        {
            return Error.Validation("spectroscopy.continuum.empty_spectrum", "The spectrum contains no points to fit a continuum to.");
        }

        if (polynomialDegree is < 0 or > MaxPolynomialDegree)
        {
            return Error.Validation(
                "spectroscopy.continuum.invalid_degree", $"polynomialDegree must be between 0 and {MaxPolynomialDegree}.");
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (!double.IsFinite(x[i]) || !double.IsFinite(flux[i]))
            {
                return Error.Validation("spectroscopy.continuum.non_finite_value", "x and flux values must be finite.");
            }
        }

        var minimumPoints = polynomialDegree + 1;

        var included = new bool[x.Length];

        for (var i = 0; i < x.Length; i++)
        {
            included[i] = !IsExcluded(x[i], excludedRanges);
        }

        var (center, halfRange) = ResolveNormalization(x);

        var t = new double[x.Length];

        for (var i = 0; i < x.Length; i++)
        {
            t[i] = (x[i] - center) / halfRange;
        }

        var fitResult = FitOnce(t, flux, included, polynomialDegree, minimumPoints);

        if (fitResult.IsFailure)
        {
            return Result<(double[], double[])>.Failure(fitResult.Error);
        }

        var coefficients = fitResult.Value;

        if (sigmaClipThreshold is { } threshold && sigmaClipIterations is { } iterations)
        {
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var sigma = ComputeResidualStandardDeviation(t, flux, included, coefficients);

                var newlyMasked = false;

                for (var i = 0; i < x.Length; i++)
                {
                    if (!included[i])
                    {
                        continue;
                    }

                    var residual = flux[i] - SpectrumExtractor.EvaluateWavelength(t[i], coefficients);

                    if (Math.Abs(residual) > threshold * sigma)
                    {
                        included[i] = false;

                        newlyMasked = true;
                    }
                }

                if (!newlyMasked)
                {
                    break;
                }

                var refitResult = FitOnce(t, flux, included, polynomialDegree, minimumPoints);

                if (refitResult.IsFailure)
                {
                    return Result<(double[], double[])>.Failure(refitResult.Error);
                }

                coefficients = refitResult.Value;
            }
        }

        var continuum = new double[x.Length];

        for (var i = 0; i < x.Length; i++)
        {
            continuum[i] = SpectrumExtractor.EvaluateWavelength(t[i], coefficients);
        }

        return (continuum, ToRawPowerBasis(coefficients, center, halfRange));
    }

    private static (double Center, double HalfRange) ResolveNormalization(ReadOnlySpan<double> x)
    {
        var min = double.PositiveInfinity;

        var max = double.NegativeInfinity;

        foreach (var value in x)
        {
            min = Math.Min(min, value);

            max = Math.Max(max, value);
        }

        var halfRange = (max - min) / 2.0;

        return ((min + max) / 2.0, halfRange > 0.0 ? halfRange : 1.0);
    }

    private static double[] ToRawPowerBasis(double[] normalizedCoefficients, double center, double halfRange)
    {
        var raw = new double[normalizedCoefficients.Length];

        for (var k = 0; k < normalizedCoefficients.Length; k++)
        {
            var scaled = normalizedCoefficients[k] / Math.Pow(halfRange, k);

            var binomial = 1.0;

            for (var j = 0; j <= k; j++)
            {
                raw[j] += scaled * binomial * Math.Pow(-center, k - j);

                binomial = binomial * (k - j) / (j + 1);
            }
        }

        return raw;
    }

    private static bool IsExcluded(double value, ReadOnlySpan<(double Min, double Max)> excludedRanges)
    {
        foreach (var (min, max) in excludedRanges)
        {
            if (value >= min && value <= max)
            {
                return true;
            }
        }

        return false;
    }

    private static Result<double[]> FitOnce(
        ReadOnlySpan<double> x, ReadOnlySpan<double> flux, bool[] included, int polynomialDegree, int minimumPoints)
    {
        var includedCount = 0;

        foreach (var isIncluded in included)
        {
            if (isIncluded)
            {
                includedCount++;
            }
        }

        if (includedCount < minimumPoints)
        {
            return Error.Validation(
                "spectroscopy.continuum.insufficient_points",
                $"At least {minimumPoints} unexcluded points are required to fit a degree-{polynomialDegree} continuum, but only {includedCount} remain.");
        }

        var coefficientCount = polynomialDegree + 1;

        var normalMatrix = new double[coefficientCount, coefficientCount];

        var rhs = new double[coefficientCount];

        var basis = new double[coefficientCount];

        for (var i = 0; i < x.Length; i++)
        {
            if (!included[i])
            {
                continue;
            }

            basis[0] = 1.0;

            for (var power = 1; power < coefficientCount; power++)
            {
                basis[power] = basis[power - 1] * x[i];
            }

            for (var row = 0; row < coefficientCount; row++)
            {
                rhs[row] += basis[row] * flux[i];

                for (var col = 0; col < coefficientCount; col++)
                {
                    normalMatrix[row, col] += basis[row] * basis[col];
                }
            }
        }

        return LinearSystemSolver.Solve(
            normalMatrix,
            rhs,
            "spectroscopy.continuum.singular_system",
            "The unexcluded points do not provide enough independent information to fit the requested continuum degree.");
    }

    private static double ComputeResidualStandardDeviation(
        ReadOnlySpan<double> x, ReadOnlySpan<double> flux, bool[] included, double[] coefficients)
    {
        var sumSquaredResiduals = 0.0;

        var count = 0;

        for (var i = 0; i < x.Length; i++)
        {
            if (!included[i])
            {
                continue;
            }

            var residual = flux[i] - SpectrumExtractor.EvaluateWavelength(x[i], coefficients);

            sumSquaredResiduals += residual * residual;

            count++;
        }

        return count > 0 ? Math.Sqrt(sumSquaredResiduals / count) : 0.0;
    }
}
