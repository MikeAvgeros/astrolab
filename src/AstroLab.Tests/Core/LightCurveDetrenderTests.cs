using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LightCurveDetrenderTests
{
    [Fact]
    public void Detrend_Linear_OnPerfectLinearTrend_RemovesTrendExactly()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0, 4.0];

        ReadOnlySpan<double> flux = [1.0, 3.0, 5.0, 7.0, 9.0];

        var result = LightCurveDetrender.Detrend(time, flux, "linear");

        Assert.True(result.IsSuccess);

        Assert.All(result.Value, detrended => Assert.Equal(0.0, detrended, precision: 9));
    }

    [Fact]
    public void Detrend_Linear_WithNoisyData_SubtractsLeastSquaresLine()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0, 4.0];

        ReadOnlySpan<double> flux = [1.0, 3.0, 4.0, 7.0, 10.0];

        var result = LightCurveDetrender.Detrend(time, flux, "linear");

        Assert.True(result.IsSuccess);

        double[] expected = [0.4, 0.2, -1.0, -0.2, 0.6];

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], result.Value[i], precision: 9);
        }
    }

    [Fact]
    public void Detrend_Linear_RejectsConstantTimeValues()
    {
        ReadOnlySpan<double> time = [5.0, 5.0, 5.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0];

        var result = LightCurveDetrender.Detrend(time, flux, "linear");

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.constant_time_values", result.Error.Code);
    }

    [Fact]
    public void Detrend_Median_SubtractsFixedWidthMovingMedian()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];

        ReadOnlySpan<double> flux = [10.0, 10.0, 10.0, 10.0, 50.0, 10.0, 10.0, 10.0, 10.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median");

        Assert.True(result.IsSuccess);

        double[] expected = [0.0, 0.0, 0.0, 0.0, 40.0, 0.0, 0.0, 0.0, 0.0];

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Detrend_Median_RejectsSeriesShorterThanThreePoints()
    {
        ReadOnlySpan<double> time = [0.0, 1.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median");

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.series_too_short", result.Error.Code);
    }

    [Fact]
    public void Detrend_IsCaseInsensitiveToMethodName()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0];

        var result = LightCurveDetrender.Detrend(time, flux, "LINEAR");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Detrend_RejectsUnknownMethod()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0];

        var result = LightCurveDetrender.Detrend(time, flux, "sinusoidal");

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.unknown_method", result.Error.Code);
    }

    [Fact]
    public void Detrend_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        var result = LightCurveDetrender.Detrend(time, flux, "linear");

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Detrend_RejectsEmptySeries()
    {
        var result = LightCurveDetrender.Detrend([], [], "linear");

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.empty_series", result.Error.Code);
    }
}
