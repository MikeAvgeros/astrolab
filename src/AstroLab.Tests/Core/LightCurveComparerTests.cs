using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LightCurveComparerTests
{
    [Fact]
    public void Compare_PerfectlyCorrelatedFlux_ReturnsUnitCorrelationAndConstantMagnitudeOffset()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0];

        ReadOnlySpan<double> fluxA = [100.0, 200.0, 300.0, 400.0];

        ReadOnlySpan<double> fluxB = [50.0, 100.0, 150.0, 200.0];

        var result = LightCurveComparer.Compare(time, fluxA, time, fluxB);

        Assert.True(result.IsSuccess);

        Assert.Equal(1.0, result.Value.CorrelationCoefficient, precision: 9);

        var expectedMagnitudeDifference = -2.5 * Math.Log10(2.0);

        Assert.Equal(expectedMagnitudeDifference, result.Value.MeanMagnitudeDifference, precision: 9);

        Assert.Equal(2.0, result.Value.FluxRatio, precision: 9);

        Assert.Equal(2.0, result.Value.VariabilityRatio, precision: 9);
    }

    [Fact]
    public void Compare_RejectsZeroMeanComparisonFlux()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> fluxA = [1.0, 2.0, 3.0];

        ReadOnlySpan<double> fluxB = [-1.0, 0.0, 1.0];

        var result = LightCurveComparer.Compare(time, fluxA, time, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.zero_mean_flux", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> timeA = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> fluxA = [1.0, 2.0, 3.0];

        ReadOnlySpan<double> timeB = [0.0, 1.0];

        ReadOnlySpan<double> fluxB = [1.0, 2.0];

        var result = LightCurveComparer.Compare(timeA, fluxA, timeB, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsEmptySeries()
    {
        var result = LightCurveComparer.Compare([], [], [], []);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.empty_series", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsConstantSeries()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> fluxA = [10.0, 10.0, 10.0];

        ReadOnlySpan<double> fluxB = [1.0, 2.0, 3.0];

        var result = LightCurveComparer.Compare(time, fluxA, time, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.zero_variance", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsSeriesWithNoPositiveFluxPairs()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> fluxA = [-1.0, -2.0, -3.0];

        ReadOnlySpan<double> fluxB = [-4.0, -5.0, -6.0];

        var result = LightCurveComparer.Compare(time, fluxA, time, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.no_positive_flux_pairs", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsTimeMisalignedSeries()
    {
        ReadOnlySpan<double> timeA = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> timeB = [0.0, 1.5, 2.0];

        ReadOnlySpan<double> fluxA = [1.0, 2.0, 3.0];

        ReadOnlySpan<double> fluxB = [4.0, 5.0, 6.0];

        var result = LightCurveComparer.Compare(timeA, fluxA, timeB, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.time_misaligned", result.Error.Code);
    }

    [Fact]
    public void Compare_RejectsNonFiniteValues()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> fluxA = [1.0, double.NaN, 3.0];

        ReadOnlySpan<double> fluxB = [4.0, 5.0, 6.0];

        var result = LightCurveComparer.Compare(time, fluxA, time, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.compare.non_finite_value", result.Error.Code);
    }
}
