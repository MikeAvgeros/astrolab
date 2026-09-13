using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Fits a Gaussian-plus-baseline profile <c>f(x) = baseline + amplitude * exp(-(x-center)^2 / (2 *
/// sigma^2))</c> to a spectral feature over a caller-supplied window, via Levenberg-Marquardt
/// damped nonlinear-least-squares iterations from a moment-based (or caller-supplied) initial guess.
/// Plain Gauss-Newton (no damping) is unreliable for this model on sparse, near-delta-function
/// features — the center/sigma Jacobian terms blow up as sigma shrinks towards a single-bin spike —
/// so each step is only accepted when it actually reduces the sum of squared residuals; otherwise the
/// damping factor grows and the step is retried, which is the standard fix for exactly this failure
/// mode. Parameter uncertainties are the standard linearized-covariance approximation
/// (residual-variance times the diagonal of the inverse normal-equations matrix at the converged
/// solution) rather than a rigorous bootstrap/MCMC estimate.
/// </summary>
public static class SpectralLineFitter
{
    private const int ParameterCount = 4;
    private const int MaxIterations = 100;
    private const double ConvergenceTolerance = 1e-12;
    private const double SigmaFloor = 1e-9;
    private const double FwhmToSigma = 1.0 / 2.3548200450309493; // 2*sqrt(2*ln2)
    private const double TwoPi = 2.0 * Math.PI;
    private const double InitialDampingFactor = 1e-3;
    private const double DampingGrowthFactor = 10.0;
    private const double DampingShrinkFactor = 10.0;
    private const double MaxDampingFactor = 1e12;

    public static Result<GaussianLineFit> FitGaussian(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> flux,
        double? initialCenter,
        double? initialAmplitude,
        double? initialFwhm)
    {
        if (x.Length != flux.Length)
        {
            return Error.Validation("spectroscopy.line_fit.length_mismatch", $"x length ({x.Length}) must equal flux length ({flux.Length}).");
        }

        const int minimumPoints = ParameterCount + 1;

        if (x.Length < minimumPoints)
        {
            return Error.Validation(
                "spectroscopy.line_fit.insufficient_points",
                $"At least {minimumPoints} points are required to fit a 4-parameter Gaussian, but only {x.Length} were supplied.");
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (!double.IsFinite(x[i]) || !double.IsFinite(flux[i]))
            {
                return Error.Validation("spectroscopy.line_fit.non_finite_input", "x and flux values must all be finite.");
            }
        }

        var theta = EstimateInitialParameters(x, flux, initialCenter, initialAmplitude, initialFwhm);

        var currentSumSquaredResiduals = SumSquaredResiduals(x, flux, theta);

        var dampingFactor = InitialDampingFactor;

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var (jacobian, residuals) = BuildJacobianAndResiduals(x, flux, theta);

            BuildNormalEquations(jacobian, residuals, x.Length, out var normalMatrix, out var rhs);

            for (var p = 0; p < ParameterCount; p++)
            {
                normalMatrix[p, p] *= 1.0 + dampingFactor;
            }

            var solveResult = LinearSystemSolver.Solve(
                normalMatrix, rhs, "spectroscopy.line_fit.singular_system", "The fit window does not provide enough independent information to fit a Gaussian line profile.");

            if (solveResult.IsFailure)
            {
                dampingFactor *= DampingGrowthFactor;

                if (dampingFactor > MaxDampingFactor)
                {
                    return Result<GaussianLineFit>.Failure(solveResult.Error);
                }

                continue;
            }

            var delta = solveResult.Value;

            var candidate = (double[])theta.Clone();

            var stepNormSquared = 0.0;

            for (var p = 0; p < ParameterCount; p++)
            {
                candidate[p] += delta[p];

                stepNormSquared += delta[p] * delta[p];
            }

            candidate[3] = Math.Max(Math.Abs(candidate[3]), SigmaFloor);

            var candidateSumSquaredResiduals = SumSquaredResiduals(x, flux, candidate);

            if (double.IsFinite(candidateSumSquaredResiduals) && candidateSumSquaredResiduals < currentSumSquaredResiduals)
            {
                theta = candidate;

                var relativeImprovement = (currentSumSquaredResiduals - candidateSumSquaredResiduals)
                    / Math.Max(currentSumSquaredResiduals, double.Epsilon);

                currentSumSquaredResiduals = candidateSumSquaredResiduals;

                dampingFactor /= DampingShrinkFactor;

                if (stepNormSquared < ConvergenceTolerance * ConvergenceTolerance || relativeImprovement < ConvergenceTolerance)
                {
                    break;
                }
            }
            else
            {
                dampingFactor *= DampingGrowthFactor;

                if (dampingFactor > MaxDampingFactor)
                {
                    break;
                }
            }
        }

        var (finalJacobian, finalResiduals) = BuildJacobianAndResiduals(x, flux, theta);

        var degreesOfFreedom = x.Length - ParameterCount;

        var sumSquaredResiduals = 0.0;

        foreach (var residual in finalResiduals)
        {
            sumSquaredResiduals += residual * residual;
        }

        var reducedChiSquare = sumSquaredResiduals / degreesOfFreedom;

        BuildNormalEquations(finalJacobian, finalResiduals, x.Length, out var finalNormalMatrix, out _);

        var uncertaintiesResult = ComputeParameterUncertainties(finalNormalMatrix, reducedChiSquare);

        if (uncertaintiesResult.IsFailure)
        {
            return Result<GaussianLineFit>.Failure(uncertaintiesResult.Error);
        }

        var uncertainties = uncertaintiesResult.Value;

        var baseline = theta[0];

        var amplitude = theta[1];

        var center = theta[2];

        var sigma = theta[3];

        var fwhm = sigma / FwhmToSigma;

        var fwhmUncertainty = uncertainties[3] / FwhmToSigma;

        var integratedFlux = amplitude * sigma * Math.Sqrt(TwoPi);

        return GaussianLineFit.Create(
            baseline, uncertainties[0],
            amplitude, uncertainties[1],
            center, uncertainties[2],
            fwhm, fwhmUncertainty,
            integratedFlux, reducedChiSquare);
    }

    private static double[] EstimateInitialParameters(
        ReadOnlySpan<double> x, ReadOnlySpan<double> flux, double? initialCenter, double? initialAmplitude, double? initialFwhm)
    {
        var baseline = (flux[0] + flux[^1]) / 2.0;

        var weights = new double[x.Length];

        var sumWeights = 0.0;

        var peakIndex = 0;

        var peakDeviation = 0.0;

        for (var i = 0; i < x.Length; i++)
        {
            var deviation = Math.Abs(flux[i] - baseline);

            weights[i] = deviation;

            sumWeights += deviation;

            if (deviation > peakDeviation)
            {
                peakDeviation = deviation;

                peakIndex = i;
            }
        }

        var center = initialCenter ?? (sumWeights > 0.0 ? WeightedMean(x, weights, sumWeights) : x[peakIndex]);

        var amplitude = initialAmplitude ?? (flux[peakIndex] - baseline);

        double sigma;

        if (initialFwhm is { } fwhm)
        {
            sigma = fwhm * FwhmToSigma;
        }
        else if (sumWeights > 0.0)
        {
            var weightedVariance = 0.0;

            for (var i = 0; i < x.Length; i++)
            {
                var offset = x[i] - center;

                weightedVariance += weights[i] * offset * offset;
            }

            weightedVariance /= sumWeights;

            sigma = weightedVariance > 0.0 ? Math.Sqrt(weightedVariance) : (x[^1] - x[0]) / x.Length;
        }
        else
        {
            sigma = (x[^1] - x[0]) / x.Length;
        }

        sigma = Math.Max(Math.Abs(sigma), SigmaFloor);

        return [baseline, amplitude, center, sigma];
    }

    private static double SumSquaredResiduals(ReadOnlySpan<double> x, ReadOnlySpan<double> flux, double[] theta)
    {
        var baseline = theta[0];

        var amplitude = theta[1];

        var center = theta[2];

        var sigma = theta[3];

        var sigmaSquared = sigma * sigma;

        var sum = 0.0;

        for (var i = 0; i < x.Length; i++)
        {
            var offset = x[i] - center;

            var z = Math.Exp(-(offset * offset) / (2.0 * sigmaSquared));

            var residual = flux[i] - (baseline + (amplitude * z));

            sum += residual * residual;
        }

        return sum;
    }

    private static double WeightedMean(ReadOnlySpan<double> x, double[] weights, double sumWeights)
    {
        var sum = 0.0;

        for (var i = 0; i < x.Length; i++)
        {
            sum += x[i] * weights[i];
        }

        return sum / sumWeights;
    }

    private static (double[,] Jacobian, double[] Residuals) BuildJacobianAndResiduals(
        ReadOnlySpan<double> x, ReadOnlySpan<double> flux, double[] theta)
    {
        var baseline = theta[0];

        var amplitude = theta[1];

        var center = theta[2];

        var sigma = theta[3];

        var jacobian = new double[x.Length, ParameterCount];

        var residuals = new double[x.Length];

        var sigmaSquared = sigma * sigma;

        var sigmaCubed = sigmaSquared * sigma;

        for (var i = 0; i < x.Length; i++)
        {
            var offset = x[i] - center;

            var z = Math.Exp(-(offset * offset) / (2.0 * sigmaSquared));

            var model = baseline + amplitude * z;

            residuals[i] = flux[i] - model;

            jacobian[i, 0] = 1.0;

            jacobian[i, 1] = z;

            jacobian[i, 2] = amplitude * z * offset / sigmaSquared;

            jacobian[i, 3] = amplitude * z * offset * offset / sigmaCubed;
        }

        return (jacobian, residuals);
    }

    private static void BuildNormalEquations(
        double[,] jacobian, double[] residuals, int pointCount, out double[,] normalMatrix, out double[] rhs)
    {
        normalMatrix = new double[ParameterCount, ParameterCount];

        rhs = new double[ParameterCount];

        for (var i = 0; i < pointCount; i++)
        {
            for (var row = 0; row < ParameterCount; row++)
            {
                rhs[row] += jacobian[i, row] * residuals[i];

                for (var col = 0; col < ParameterCount; col++)
                {
                    normalMatrix[row, col] += jacobian[i, row] * jacobian[i, col];
                }
            }
        }
    }

    private static Result<double[]> ComputeParameterUncertainties(double[,] normalMatrix, double reducedChiSquare)
    {
        var uncertainties = new double[ParameterCount];

        for (var column = 0; column < ParameterCount; column++)
        {
            var identityColumn = new double[ParameterCount];

            identityColumn[column] = 1.0;

            var matrixCopy = (double[,])normalMatrix.Clone();

            var solveResult = LinearSystemSolver.Solve(
                matrixCopy, identityColumn, "spectroscopy.line_fit.singular_covariance",
                "The converged fit's normal-equations matrix is singular; parameter uncertainties cannot be estimated.");

            if (solveResult.IsFailure)
            {
                return Result<double[]>.Failure(solveResult.Error);
            }

            var variance = reducedChiSquare * solveResult.Value[column];

            uncertainties[column] = Math.Sqrt(Math.Max(variance, 0.0));
        }

        return uncertainties;
    }
}
