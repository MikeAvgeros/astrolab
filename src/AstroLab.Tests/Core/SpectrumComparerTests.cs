using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectrumComparerTests
{
    [Fact]
    public void Compare_ScaledSpectrum_ReturnsFluxRatioAndRmsDifference()
    {
        double[] fluxA = [10.0, 20.0, 30.0, 40.0];

        double[] fluxB = [5.0, 10.0, 15.0, 20.0];

        var result = SpectrumComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsSuccess);

        Assert.Equal(2.0, result.Value.MeanFluxRatio, precision: 9);

        var expectedRms = Math.Sqrt(((5.0 * 5.0) + (10.0 * 10.0) + (15.0 * 15.0) + (20.0 * 20.0)) / 4.0);

        Assert.Equal(expectedRms, result.Value.RmsFluxDifference, precision: 9);
    }

    [Fact]
    public void Compare_RejectsLengthMismatch()
    {
        double[] fluxA = [1.0, 2.0, 3.0];

        double[] fluxB = [1.0, 2.0];

        var result = SpectrumComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.compare.flux_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsZeroMeanComparisonFlux()
    {
        double[] fluxA = [1.0, 2.0, 3.0];

        double[] fluxB = [-1.0, 0.0, 1.0];

        var result = SpectrumComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.compare.zero_mean_flux", result.Error.Code);
    }
}
