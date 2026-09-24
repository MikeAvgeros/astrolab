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

    [Theory]
    [InlineData(3.0, 0.1, 0.0)]
    [InlineData(10.0, 0.125, 0.0)]
    [InlineData(4.3, 0.15, -0.05)]
    public void Search_LongBaselineThirtyMinuteCadence_RecoversShortTransitPeriodDepthAndDuration(
        double truePeriod, double trueDuration, double firstMidTransitOffset)
    {
        // 45 days at 30-minute cadence, searched over 0.5-20 days. A negative offset puts the first
        // mid-transit before the first sample, so the series starts part-way through a transit.
        const double cadence = 30.0 / 1440.0;

        const double trueDepth = 0.01;

        var sampleCount = (int)(45.0 / cadence);

        var time = new double[sampleCount];

        var flux = new double[sampleCount];

        var epoch = trueDuration / 2.0 + firstMidTransitOffset;

        for (var i = 0; i < sampleCount; i++)
        {
            time[i] = i * cadence;

            var phaseTime = time[i] - epoch;

            var offsetFromMidTransit = phaseTime - truePeriod * Math.Round(phaseTime / truePeriod);

            flux[i] = Math.Abs(offsetFromMidTransit) < trueDuration / 2.0 ? 1.0 - trueDepth : 1.0;
        }

        var result = TransitSearch.Search(time, flux, minPeriod: 0.5, maxPeriod: 20.0, minTransitDepth: 0.005);

        Assert.True(result.IsSuccess);

        Assert.InRange(result.Value.Period, truePeriod * 0.999, truePeriod * 1.001);

        Assert.InRange(result.Value.Depth, 0.8 * trueDepth, 1.05 * trueDepth);

        Assert.InRange(result.Value.Duration, 0.5 * trueDuration, 1.5 * trueDuration);
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
