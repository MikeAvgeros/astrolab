using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LombScarglePeriodogramTests
{
    [Fact]
    public void SearchFull_ShortPeriodOverLongBaselineWithSuggestedRange_RecoversPeriod()
    {
        // 27 days at 30-minute cadence (a TESS-sector-like light curve) with a 0.2317-day signal:
        // a grid uniform in period would step far past the ~1/T-wide frequency peak.
        const double truePeriod = 0.2317;

        const double cadenceDays = 30.0 / 1440.0;

        var sampleCount = (int)(27.0 / cadenceDays);

        var time = new double[sampleCount];

        var flux = new double[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            time[i] = i * cadenceDays;

            flux[i] = Math.Sin(2.0 * Math.PI * time[i] / truePeriod);
        }

        var range = LombScarglePeriodogram.SuggestPeriodRange(time).Value;

        var result = LombScarglePeriodogram.SearchFull(time, flux, range.MinPeriod, range.MaxPeriod);

        Assert.True(result.IsSuccess);

        Assert.InRange(result.Value.BestPeriod, truePeriod * 0.999, truePeriod * 1.001);

        Assert.True(result.Value.Power > 0.9 * (sampleCount - 1) / 2.0);

        Assert.Equal(range.MinPeriod, result.Value.Periods[0], precision: 9);

        Assert.Equal(range.MaxPeriod, result.Value.Periods[^1], precision: 9);
    }

    [Fact]
    public void Search_SinusoidalSignal_RecoversKnownPeriod()
    {
        const double truePeriod = 5.0;

        var time = new double[200];

        var flux = new double[200];

        for (var i = 0; i < time.Length; i++)
        {
            time[i] = i * 0.25;

            flux[i] = Math.Sin(2.0 * Math.PI * time[i] / truePeriod);
        }

        var result = LombScarglePeriodogram.Search(time, flux, minPeriod: 1.0, maxPeriod: 20.0, gridSize: 4000);

        Assert.True(result.IsSuccess);

        Assert.True(Math.Abs(result.Value.BestPeriod - truePeriod) < 0.05, $"Expected period near {truePeriod}, got {result.Value.BestPeriod}.");

        // For a clean, well-sampled N-point sinusoid, the standard normalized Lomb-Scargle power at
        // the true period approaches (N-1)/2 (here, (200-1)/2 = 99.5).
        Assert.True(result.Value.Power > 90.0, $"Expected power near (N-1)/2 = 99.5, got power {result.Value.Power}.");
    }

    [Fact]
    public void Search_RejectsConstantFlux()
    {
        double[] time = [0.0, 1.0, 2.0, 3.0];

        double[] flux = [5.0, 5.0, 5.0, 5.0];

        var result = LombScarglePeriodogram.Search(time, flux, minPeriod: 0.5, maxPeriod: 5.0);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.periodsearch.constant_flux", result.Error.Code);
    }

    [Fact]
    public void Search_RejectsInvertedPeriodRange()
    {
        double[] time = [0.0, 1.0, 2.0, 3.0];

        double[] flux = [1.0, 2.0, 1.0, 2.0];

        var result = LombScarglePeriodogram.Search(time, flux, minPeriod: 5.0, maxPeriod: 1.0);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.periodsearch.invalid_max_period", result.Error.Code);
    }

    [Fact]
    public void Search_RejectsTooFewPoints()
    {
        double[] time = [0.0, 1.0];

        double[] flux = [1.0, 2.0];

        var result = LombScarglePeriodogram.Search(time, flux, minPeriod: 0.5, maxPeriod: 5.0);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.periodsearch.series_too_short", result.Error.Code);
    }

    [Fact]
    public void SearchFull_SinusoidalSignal_RecoversKnownPeriodAndReturnsFullGrid()
    {
        const double truePeriod = 5.0;

        var time = new double[200];

        var flux = new double[200];

        for (var i = 0; i < time.Length; i++)
        {
            time[i] = i * 0.25;

            flux[i] = Math.Sin(2.0 * Math.PI * time[i] / truePeriod);
        }

        var result = LombScarglePeriodogram.SearchFull(time, flux, minPeriod: 1.0, maxPeriod: 20.0, gridSize: 4000);

        Assert.True(result.IsSuccess);

        var search = result.Value;

        Assert.True(Math.Abs(search.BestPeriod - truePeriod) < 0.05, $"Expected period near {truePeriod}, got {search.BestPeriod}.");

        Assert.True(search.Power > 90.0, $"Expected power near (N-1)/2 = 99.5, got power {search.Power}.");

        Assert.Equal(4000, search.Periods.Length);

        Assert.Equal(4000, search.Powers.Length);

        Assert.Equal(1.0, search.Periods[0], precision: 9);

        Assert.Equal(20.0, search.Periods[^1], precision: 9);

        Assert.True(search.FalseAlarmProbability is >= 0.0 and < 0.01, $"Expected a low FAP for a strong peak, got {search.FalseAlarmProbability}.");
    }

    [Fact]
    public void SearchFull_FalseAlarmProbability_UsesNormalizedPowerDirectlyWithoutExtraScaling()
    {
        // Deliberately small and sparsely sampled so Power stays in a numerically well-behaved
        // range (neither so small that the false-alarm probability trivially saturates at 1, nor so
        // large that exp(-Power) underflows to exactly 0 on both sides of the comparison below,
        // which would make the assertion pass vacuously regardless of the scaling this test targets.
        const double truePeriod = 10.0;

        var time = new double[10];

        var flux = new double[10];

        for (var i = 0; i < time.Length; i++)
        {
            time[i] = i;

            flux[i] = Math.Sin(2.0 * Math.PI * time[i] / truePeriod);
        }

        const int gridSize = 1000;

        var result = LombScarglePeriodogram.SearchFull(time, flux, minPeriod: 2.0, maxPeriod: 20.0, gridSize);

        Assert.True(result.IsSuccess);

        var power = result.Value.Power;

        // FalseAlarmProbability must be the Horne & Baliunas (1986) approximation applied directly
        // to the already-normalized Power, with no additional rescaling by sample count.
        var expectedFalseAlarmProbability = Math.Clamp(1.0 - Math.Pow(1.0 - Math.Exp(-power), gridSize), 0.0, 1.0);

        Assert.True(expectedFalseAlarmProbability is > 0.0 and < 1.0, $"Expected a non-degenerate FAP, got {expectedFalseAlarmProbability}.");

        Assert.Equal(expectedFalseAlarmProbability, result.Value.FalseAlarmProbability, precision: 9);
    }

    [Fact]
    public void SearchFull_RejectsConstantFlux()
    {
        double[] time = [0.0, 1.0, 2.0, 3.0];

        double[] flux = [5.0, 5.0, 5.0, 5.0];

        var result = LombScarglePeriodogram.SearchFull(time, flux, minPeriod: 0.5, maxPeriod: 5.0);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.periodsearch.constant_flux", result.Error.Code);
    }

    [Fact]
    public void SuggestPeriodRange_RegularCadence_ReturnsNyquistToHalfBaseline()
    {
        double[] time = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0];

        var result = LombScarglePeriodogram.SuggestPeriodRange(time);

        Assert.True(result.IsSuccess);

        Assert.Equal(2.0, result.Value.MinPeriod, precision: 9);

        Assert.Equal(4.5, result.Value.MaxPeriod, precision: 9);
    }
}
