using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LightCurveVariabilityAnalyzerTests
{
    [Fact]
    public void Analyze_ComputesExpectedStatistics()
    {
        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0, 4.0, 5.0];

        var result = LightCurveVariabilityAnalyzer.Analyze(flux);

        Assert.True(result.IsSuccess);

        var stats = result.Value;

        Assert.Equal(3.0, stats.Mean, precision: 9);

        Assert.Equal(3.0, stats.Median, precision: 9);

        Assert.Equal(Math.Sqrt(2.0), stats.StandardDeviation, precision: 9);

        Assert.Equal(4.0, stats.Amplitude, precision: 9);

        Assert.Equal(Math.Sqrt(55.0 / 5.0), stats.Rms, precision: 9);

        Assert.Equal(1.0, stats.MedianAbsoluteDeviation, precision: 9);
    }

    [Fact]
    public void Analyze_SinglePoint_ReturnsZeroDispersion()
    {
        ReadOnlySpan<double> flux = [7.0];

        var result = LightCurveVariabilityAnalyzer.Analyze(flux);

        Assert.True(result.IsSuccess);

        var stats = result.Value;

        Assert.Equal(7.0, stats.Mean, precision: 9);

        Assert.Equal(7.0, stats.Median, precision: 9);

        Assert.Equal(0.0, stats.StandardDeviation, precision: 9);

        Assert.Equal(0.0, stats.Amplitude, precision: 9);

        Assert.Equal(7.0, stats.Rms, precision: 9);

        Assert.Equal(0.0, stats.MedianAbsoluteDeviation, precision: 9);
    }

    [Fact]
    public void Analyze_RejectsEmptySeries()
    {
        var result = LightCurveVariabilityAnalyzer.Analyze([]);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.variability.empty_series", result.Error.Code);
    }
}
