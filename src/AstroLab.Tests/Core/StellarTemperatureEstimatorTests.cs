using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class StellarTemperatureEstimatorTests
{
    [Fact]
    public void EstimateKelvin_MatchesBallesterosFormula()
    {
        const double colourIndex = 0.65;

        var result = StellarTemperatureEstimator.EstimateKelvin(colourIndex);

        Assert.True(result.IsSuccess);

        var expected = 4600.0 * ((1.0 / ((0.92 * colourIndex) + 1.7)) + (1.0 / ((0.92 * colourIndex) + 0.62)));

        Assert.Equal(expected, result.Value, precision: 6);
    }

    [Fact]
    public void EstimateKelvin_BluerColourIndex_ProducesHigherTemperature()
    {
        var blue = StellarTemperatureEstimator.EstimateKelvin(-0.2).Value;

        var red = StellarTemperatureEstimator.EstimateKelvin(1.5).Value;

        Assert.True(blue > red);
    }

    [Fact]
    public void EstimateKelvin_RejectsNonFiniteColourIndex()
    {
        var result = StellarTemperatureEstimator.EstimateKelvin(double.NaN);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.stellartemperature.invalid_colour_index", result.Error.Code);
    }

    [Fact]
    public void EstimateKelvin_RejectsColourIndexAtSingularity()
    {
        var result = StellarTemperatureEstimator.EstimateKelvin(-1.7 / 0.92);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.stellartemperature.singular_relation", result.Error.Code);
    }

    [Theory]
    [InlineData(-0.3)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void EstimateKelvin_ProducesPositiveTemperatureForTypicalColourIndices(double colourIndex)
    {
        var result = StellarTemperatureEstimator.EstimateKelvin(colourIndex);

        Assert.True(result.IsSuccess);

        Assert.True(result.Value > 0.0);
    }
}
