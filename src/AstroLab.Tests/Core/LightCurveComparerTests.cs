using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LightCurveComparerTests
{
    [Fact]
    public void Compare_PerfectlyCorrelatedFlux_ReturnsUnitCorrelationAndConstantMagnitudeOffset()
    {
        ReadOnlySpan<double> fluxA = [100.0, 200.0, 300.0, 400.0];

        ReadOnlySpan<double> fluxB = [50.0, 100.0, 150.0, 200.0];

        var result = LightCurveComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsSuccess);

        Assert.Equal(1.0, result.Value.CorrelationCoefficient, precision: 9);

        var expectedMagnitudeDifference = -2.5 * Math.Log10(2.0);

        Assert.Equal(expectedMagnitudeDifference, result.Value.MeanMagnitudeDifference, precision: 9);
    }

    [Fact]
    public void Compare_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> fluxA = [1.0, 2.0, 3.0];

        ReadOnlySpan<double> fluxB = [1.0, 2.0];

        var result = LightCurveComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsEmptySeries()
    {
        var result = LightCurveComparer.Compare([], []);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.empty_series", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsConstantSeries()
    {
        ReadOnlySpan<double> fluxA = [10.0, 10.0, 10.0];

        ReadOnlySpan<double> fluxB = [1.0, 2.0, 3.0];

        var result = LightCurveComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.zero_variance", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsSeriesWithNoPositiveFluxPairs()
    {
        ReadOnlySpan<double> fluxA = [-1.0, -2.0, -3.0];

        ReadOnlySpan<double> fluxB = [-4.0, -5.0, -6.0];

        var result = LightCurveComparer.Compare(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.no_positive_flux_pairs", result.Error.Code);
    }
}
