using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure comparison of two paired flux series (assumed already aligned bin-for-bin, e.g. two spectra
/// extracted on the same instrument/dispersion setup): the mean flux ratio and the root-mean-square
/// flux difference between them.
/// </summary>
public static class SpectrumComparer
{
    public static Result<(double MeanFluxRatio, double RmsFluxDifference)> Compare(ReadOnlySpan<double> fluxA, ReadOnlySpan<double> fluxB)
    {
        if (fluxA.Length != fluxB.Length)
        {
            return Error.Validation(
                "spectroscopy.compare.flux_length_mismatch", $"fluxA length ({fluxA.Length}) must equal fluxB length ({fluxB.Length}).");
        }

        if (fluxA.IsEmpty)
        {
            return Error.Validation("spectroscopy.compare.empty_spectrum", "The spectra contain no bins to compare.");
        }

        var meanA = Mean(fluxA);

        var meanB = Mean(fluxB);

        if (meanB == 0.0)
        {
            return Error.Validation(
                "spectroscopy.compare.zero_mean_flux", "The comparison spectrum has zero mean flux, so a flux ratio is undefined.");
        }

        var sumSquaredDifference = 0.0;

        for (var i = 0; i < fluxA.Length; i++)
        {
            var difference = fluxA[i] - fluxB[i];

            sumSquaredDifference += difference * difference;
        }

        var rmsDifference = Math.Sqrt(sumSquaredDifference / fluxA.Length);

        return (meanA / meanB, rmsDifference);
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
