using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Shared Gaussian-elimination-with-partial-pivoting solver for the small, dense normal-equations
/// systems produced by this namespace's least-squares fitters (dispersion-solution fitting,
/// polynomial continuum fitting, and Gauss-Newton spectral line fitting). Extracted so those three
/// fitters share one tested implementation rather than duplicating the same elimination logic.
/// Every caller passes symmetric positive (semi-)definite normal equations, so the system is first
/// Jacobi-equilibrated (scaled by the inverse square root of its diagonal to a unit diagonal). This
/// makes the singularity test independent of each parameter's physical units: without it, a fixed
/// pivot tolerance declares a perfectly well-posed fit singular whenever the data are small in
/// absolute terms (e.g. flux-calibrated spectra in erg/s/cm²/Å, where JᵀJ scales with flux²).
/// </summary>
internal static class LinearSystemSolver
{
    private const double SingularPivotTolerance = 1e-12;

    public static Result<double[]> Solve(double[,] matrix, double[] rhs, string singularSystemErrorCode, string singularSystemErrorMessage)
    {
        var n = rhs.Length;

        var scale = new double[n];

        for (var i = 0; i < n; i++)
        {
            var diagonal = matrix[i, i];

            if (!(diagonal > 0.0) || !double.IsFinite(diagonal))
            {
                return Error.Validation(singularSystemErrorCode, singularSystemErrorMessage);
            }

            scale[i] = 1.0 / Math.Sqrt(diagonal);
        }

        for (var row = 0; row < n; row++)
        {
            for (var col = 0; col < n; col++)
            {
                matrix[row, col] *= scale[row] * scale[col];
            }

            rhs[row] *= scale[row];
        }

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

            if (largestPivotMagnitude < SingularPivotTolerance || double.IsNaN(largestPivotMagnitude))
            {
                return Error.Validation(singularSystemErrorCode, singularSystemErrorMessage);
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

        for (var i = 0; i < n; i++)
        {
            solution[i] *= scale[i];
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
