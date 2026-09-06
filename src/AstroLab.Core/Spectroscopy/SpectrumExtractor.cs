using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure one-dimensional spectral extraction algorithms for long-slit/fiber spectroscopy: boxcar
/// aperture extraction along a (possibly curved) trace, background subtraction, and polynomial
/// wavelength calibration. Operates directly over spans with no I/O and no large intermediate
/// allocations.
/// </summary>
public static class SpectrumExtractor
{
    private const int MaxDispersionDegree = 3;
    private const int MinimumCalibrationPoints = 2;
    private const double SingularSystemTolerance = 1e-10;
    private const string DispersionAxisHeaderKeyword = "DISPAXIS";
    private const long DefaultDispersionAxisValue = 1;
    private const long VerticalDispersionAxisValue = 2;
    
    public static DispersionAxis ResolveDispersionAxis(FitsHeader header) =>
        header.GetInteger(DispersionAxisHeaderKeyword).GetValueOrDefault(DefaultDispersionAxisValue) == VerticalDispersionAxisValue
            ? DispersionAxis.Vertical
            : DispersionAxis.Horizontal;

    public static Result<Unit> ExtractBoxcar(
        ReadOnlySpan<float> image,
        int width,
        int height,
        DispersionAxis axis,
        ReadOnlySpan<double> traceCenters,
        double apertureHalfWidth,
        Span<double> spectrum)
    {
        if (width <= 0 || height <= 0 || image.Length != width * height)
        {
            return Error.Validation(
                "spectroscopy.invalid_image_bounds",
                $"Image span length ({image.Length}) does not match width x height ({width}x{height}).");
        }

        var dispersionBins = axis == DispersionAxis.Horizontal ? width : height;

        var spatialExtent = axis == DispersionAxis.Horizontal ? height : width;

        if (traceCenters.Length != dispersionBins)
        {
            return Error.Validation(
                "spectroscopy.trace_length_mismatch",
                $"traceCenters length ({traceCenters.Length}) must equal the dispersion axis extent ({dispersionBins}).");
        }

        if (spectrum.Length != dispersionBins)
        {
            return Error.Validation(
                "spectroscopy.output_length_mismatch",
                $"spectrum length ({spectrum.Length}) must equal the dispersion axis extent ({dispersionBins}).");
        }

        if (apertureHalfWidth <= 0 || !double.IsFinite(apertureHalfWidth))
        {
            return Error.Validation("spectroscopy.invalid_aperture",
                "apertureHalfWidth must be a finite, positive value.");
        }

        for (var d = 0; d < dispersionBins; d++)
        {
            var center = traceCenters[d];

            var lowerEdge = center - apertureHalfWidth;

            var upperEdge = center + apertureHalfWidth;

            var sMin = Math.Max(0, (int)Math.Floor(lowerEdge));

            var sMax = Math.Min(spatialExtent - 1, (int)Math.Ceiling(upperEdge) - 1);

            double flux = 0.0;

            for (var s = sMin; s <= sMax; s++)
            {
                var overlap = Math.Min(s + 1, upperEdge) - Math.Max(s, lowerEdge);

                if (overlap <= 0)
                {
                    continue;
                }

                var value = image[axis == DispersionAxis.Horizontal ? (s * width) + d : (d * width) + s];

                if (!float.IsFinite(value))
                {
                    continue;
                }

                flux += value * overlap;
            }

            spectrum[d] = flux;
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public static Result<Unit> SubtractBackground(Span<double> spectrum, ReadOnlySpan<double> background)
    {
        if (spectrum.Length != background.Length)
        {
            return Error.Validation(
                "spectroscopy.background_length_mismatch",
                $"background length ({background.Length}) must equal spectrum length ({spectrum.Length}).");
        }

        for (var i = 0; i < spectrum.Length; i++)
        {
            spectrum[i] -= background[i];
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public static double EvaluateWavelength(double pixelIndex, ReadOnlySpan<double> dispersionCoefficients)
    {
        var result = 0.0;

        for (var i = dispersionCoefficients.Length - 1; i >= 0; i--)
        {
            result = result * pixelIndex + dispersionCoefficients[i];
        }

        return result;
    }

    public static Result<Unit> ComputeWavelengths(
        ReadOnlySpan<double> pixelIndices, ReadOnlySpan<double> dispersionCoefficients, Span<double> wavelengths)
    {
        if (pixelIndices.Length != wavelengths.Length)
        {
            return Error.Validation(
                "spectroscopy.wavelength_length_mismatch",
                $"wavelengths length ({wavelengths.Length}) must equal pixelIndices length ({pixelIndices.Length}).");
        }

        if (dispersionCoefficients.IsEmpty)
        {
            return Error.Validation("spectroscopy.empty_dispersion_solution",
                "dispersionCoefficients must contain at least one coefficient.");
        }

        for (var i = 0; i < pixelIndices.Length; i++)
        {
            wavelengths[i] = EvaluateWavelength(pixelIndices[i], dispersionCoefficients);
        }

        return Result<Unit>.Success(Unit.Value);
    }
    
    public static Result<(double[] Coefficients, double ResidualRms)> FitDispersionSolution(
        ReadOnlySpan<double> pixelPositions, ReadOnlySpan<double> knownWavelengths)
    {
        if (pixelPositions.Length != knownWavelengths.Length)
        {
            return Error.Validation(
                "spectroscopy.calibration_length_mismatch",
                $"pixelPositions length ({pixelPositions.Length}) must equal knownWavelengths length ({knownWavelengths.Length}).");
        }

        if (pixelPositions.Length < MinimumCalibrationPoints)
        {
            return Error.Validation(
                "spectroscopy.calibration_insufficient_points",
                $"At least {MinimumCalibrationPoints} pixel/wavelength pairs are required to fit a dispersion solution.");
        }

        for (var i = 0; i < pixelPositions.Length; i++)
        {
            if (!double.IsFinite(pixelPositions[i]) || !double.IsFinite(knownWavelengths[i]))
            {
                return Error.Validation(
                    "spectroscopy.calibration_non_finite_value", "Pixel positions and known wavelengths must be finite.");
            }
        }

        var degree = Math.Min(MaxDispersionDegree, pixelPositions.Length - 1);

        var coefficientCount = degree + 1;

        var normalMatrix = new double[coefficientCount, coefficientCount];

        var rhs = new double[coefficientCount];

        var basis = new double[coefficientCount];

        for (var i = 0; i < pixelPositions.Length; i++)
        {
            basis[0] = 1.0;

            for (var power = 1; power < coefficientCount; power++)
            {
                basis[power] = basis[power - 1] * pixelPositions[i];
            }

            for (var row = 0; row < coefficientCount; row++)
            {
                rhs[row] += basis[row] * knownWavelengths[i];

                for (var col = 0; col < coefficientCount; col++)
                {
                    normalMatrix[row, col] += basis[row] * basis[col];
                }
            }
        }

        var solveResult = SolveLinearSystem(normalMatrix, rhs);

        if (solveResult.IsFailure)
        {
            return Result<(double[], double)>.Failure(solveResult.Error);
        }

        var coefficients = solveResult.Value;

        var sumSquaredResiduals = 0.0;

        for (var i = 0; i < pixelPositions.Length; i++)
        {
            var residual = EvaluateWavelength(pixelPositions[i], coefficients) - knownWavelengths[i];

            sumSquaredResiduals += residual * residual;
        }

        var residualRms = Math.Sqrt(sumSquaredResiduals / pixelPositions.Length);

        return (coefficients, residualRms);
    }

    private static Result<double[]> SolveLinearSystem(double[,] matrix, double[] rhs)
    {
        var n = rhs.Length;

        for (var pivotColumn = 0; pivotColumn < n; pivotColumn++)
        {
            var pivotRow = pivotColumn;

            var largestPivotMagnitude = Math.Abs(matrix[pivotColumn, pivotColumn]);

            for (var row = pivotColumn + 1; row < n; row++)
            {
                var candidateMagnitude = Math.Abs(matrix[row, pivotColumn]);

                if (candidateMagnitude > largestPivotMagnitude)
                {
                    largestPivotMagnitude = candidateMagnitude;

                    pivotRow = row;
                }
            }

            if (largestPivotMagnitude < SingularSystemTolerance)
            {
                return Error.Validation(
                    "spectroscopy.calibration_singular_system",
                    "The pixel positions do not provide enough independent information to fit a dispersion solution.");
            }

            if (pivotRow != pivotColumn)
            {
                SwapRows(matrix, rhs, pivotColumn, pivotRow, n);
            }

            for (var row = pivotColumn + 1; row < n; row++)
            {
                var factor = matrix[row, pivotColumn] / matrix[pivotColumn, pivotColumn];

                for (var col = pivotColumn; col < n; col++)
                {
                    matrix[row, col] -= factor * matrix[pivotColumn, col];
                }

                rhs[row] -= factor * rhs[pivotColumn];
            }
        }

        var solution = new double[n];

        for (var row = n - 1; row >= 0; row--)
        {
            var sum = rhs[row];

            for (var col = row + 1; col < n; col++)
            {
                sum -= matrix[row, col] * solution[col];
            }

            solution[row] = sum / matrix[row, row];
        }

        return solution;
    }

    private static void SwapRows(double[,] matrix, double[] rhs, int rowA, int rowB, int columnCount)
    {
        for (var col = 0; col < columnCount; col++)
        {
            (matrix[rowA, col], matrix[rowB, col]) = (matrix[rowB, col], matrix[rowA, col]);
        }

        (rhs[rowA], rhs[rowB]) = (rhs[rowB], rhs[rowA]);
    }
}
