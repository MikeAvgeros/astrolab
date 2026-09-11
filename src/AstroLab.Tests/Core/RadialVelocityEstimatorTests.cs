using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class RadialVelocityEstimatorTests
{
    [Fact]
    public void EstimateKilometersPerSecond_RedshiftedLine_ComputesPositiveVelocity()
    {
        var result = RadialVelocityEstimator.EstimateKilometersPerSecond(observedWavelengthNm: 505.0, restWavelengthNm: 500.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.01 * RadialVelocityEstimator.SpeedOfLightKmPerSecond, result.Value, precision: 6);
    }

    [Fact]
    public void EstimateKilometersPerSecond_BlueshiftedLine_ComputesNegativeVelocity()
    {
        var result = RadialVelocityEstimator.EstimateKilometersPerSecond(observedWavelengthNm: 495.0, restWavelengthNm: 500.0);

        Assert.True(result.IsSuccess);

        Assert.True(result.Value < 0.0);
    }

    [Fact]
    public void EstimateKilometersPerSecond_MatchingWavelengths_ComputesZeroVelocity()
    {
        var result = RadialVelocityEstimator.EstimateKilometersPerSecond(observedWavelengthNm: 500.0, restWavelengthNm: 500.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value, precision: 9);
    }

    [Fact]
    public void EstimateKilometersPerSecond_RejectsNonPositiveRestWavelength()
    {
        var result = RadialVelocityEstimator.EstimateKilometersPerSecond(observedWavelengthNm: 500.0, restWavelengthNm: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.invalid_rest_wavelength", result.Error.Code);
    }
}
