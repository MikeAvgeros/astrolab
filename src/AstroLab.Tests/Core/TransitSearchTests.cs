using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class TransitSearchTests
{
    private const double TruePeriod = 10.0;
    private const double TrueDepth = 0.05;
    private const double TrueDurationFraction = 0.05;

    [Fact]
    public void Search_BoxShapedDips_RecoversPeriodAndApproximateDepth()
    {
        var (time, flux) = BuildSyntheticTransitSeries();

        var result = TransitSearch.Search(time, flux, minPeriod: 5.0, maxPeriod: 15.0, minTransitDepth: 0.01);

        Assert.True(result.IsSuccess);

        Assert.True(Math.Abs(result.Value.Period - TruePeriod) < 0.1, $"Expected period near {TruePeriod}, got {result.Value.Period}.");

        Assert.True(result.Value.Depth is > 0.02 and < 0.08, $"Expected depth near {TrueDepth}, got {result.Value.Depth}.");

        Assert.True(result.Value.Duration is > 0.1 and < 1.0, $"Expected duration near 0.5, got {result.Value.Duration}.");
    }

    [Fact]
    public void Search_DepthBelowRequestedThreshold_ReturnsNotFound()
    {
        var (time, flux) = BuildSyntheticTransitSeries();

        var result = TransitSearch.Search(time, flux, minPeriod: 5.0, maxPeriod: 15.0, minTransitDepth: 0.5);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.transit.below_depth_threshold", result.Error.Code);
    }

    [Fact]
    public void Search_RejectsLengthMismatch()
    {
        double[] time = [0.0, 1.0, 2.0];

        double[] flux = [1.0, 1.0];

        var result = TransitSearch.Search(time, flux, minPeriod: 0.5, maxPeriod: 5.0, minTransitDepth: 0.01);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.transit.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Search_RejectsTooFewPoints()
    {
        double[] time = [0.0, 1.0, 2.0];

        double[] flux = [1.0, 1.0, 1.0];

        var result = TransitSearch.Search(time, flux, minPeriod: 0.5, maxPeriod: 5.0, minTransitDepth: 0.01);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.transit.series_too_short", result.Error.Code);
    }

    private static (double[] Time, double[] Flux) BuildSyntheticTransitSeries()
    {
        var time = new double[500];

        var flux = new double[500];

        for (var i = 0; i < time.Length; i++)
        {
            time[i] = i * 0.1;

            var phase = (time[i] / TruePeriod) % 1.0;

            flux[i] = phase < TrueDurationFraction ? 1.0 - TrueDepth : 1.0;
        }

        return (time, flux);
    }
}
