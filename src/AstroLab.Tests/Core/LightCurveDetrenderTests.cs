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
    public void Detrend_Median_SubtractsTimeWindowedMovingMedian()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];

        ReadOnlySpan<double> flux = [10.0, 10.0, 10.0, 10.0, 50.0, 10.0, 10.0, 10.0, 10.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration: 4.0);

        Assert.True(result.IsSuccess);

        double[] expected = [0.0, 0.0, 0.0, 0.0, 40.0, 0.0, 0.0, 0.0, 0.0];

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Detrend_Median_WindowLongerThanTransit_PreservesMultiSampleTransitDepth()
    {
        // 30-minute cadence with a 3-hour (6-sample) transit on a sloping baseline, detrended with a
        // 1-day window: a fixed 5-sample window would sit entirely inside the transit and erase it.
        const double cadence = 30.0 / 1440.0;

        var time = Enumerable.Range(0, 480).Select(i => i * cadence).ToArray();

        var flux = time.Select(t => 1.0 + 0.001 * t - (t is >= 5.0 and < 5.125 ? 0.01 : 0.0)).ToArray();

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration: 1.0);

        Assert.True(result.IsSuccess);

        var inTransit = Enumerable.Range(0, time.Length).Where(i => time[i] is >= 5.0 and < 5.125).Select(i => result.Value[i]);

        Assert.All(inTransit, value => Assert.InRange(value, -0.0102, -0.0098));
    }

    [Fact]
    public void Detrend_Median_WindowDoesNotReachAcrossDataGaps()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 100.0, 101.0, 102.0];

        ReadOnlySpan<double> flux = [10.0, 10.0, 10.0, 90.0, 90.0, 90.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration: 4.0);

        Assert.True(result.IsSuccess);

        Assert.All(result.Value, value => Assert.Equal(0.0, value, precision: 9));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Detrend_Median_WithoutValidWindowDuration_ReturnsValidationError(double? windowDuration)
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 3.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0, 4.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.invalid_window_duration", result.Error.Code);
    }

    [Fact]
    public void Detrend_Median_RejectsUnsortedTime()
    {
        ReadOnlySpan<double> time = [0.0, 2.0, 1.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];

        ReadOnlySpan<double> flux = [10.0, 10.0, 10.0, 10.0, 50.0, 10.0, 10.0, 10.0, 10.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration: 4.0);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.unsorted_time", result.Error.Code);
    }

    [Fact]
    public void Detrend_Median_RejectsSeriesShorterThanThreePoints()
    {
        ReadOnlySpan<double> time = [0.0, 1.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        var result = LightCurveDetrender.Detrend(time, flux, "median", windowDuration: 4.0);

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

    [Fact]
    public void Detrend_RejectsNullMethod()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0];

        var result = LightCurveDetrender.Detrend(time, flux, null!);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.detrend.missing_method", result.Error.Code);
    }
}
