using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LombScarglePeriodogramTests
{
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

        Assert.True(result.Value.Power > 0.45, $"Expected a strong periodogram peak, got power {result.Value.Power}.");
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

        Assert.True(search.Power > 0.45, $"Expected a strong periodogram peak, got power {search.Power}.");

        Assert.Equal(4000, search.Periods.Length);

        Assert.Equal(4000, search.Powers.Length);

        Assert.Equal(1.0, search.Periods[0], precision: 9);

        Assert.Equal(20.0, search.Periods[^1], precision: 9);

        Assert.True(search.FalseAlarmProbability is >= 0.0 and < 0.01, $"Expected a low FAP for a strong peak, got {search.FalseAlarmProbability}.");
    }

    [Fact]
    public void SearchFull_FalseAlarmProbability_MatchesHorneBaliunasFormulaForGridSize()
    {
        const double truePeriod = 5.0;

        var time = new double[200];

        var flux = new double[200];

        for (var i = 0; i < time.Length; i++)
        {
            time[i] = i * 0.25;

            flux[i] = Math.Sin(2.0 * Math.PI * time[i] / truePeriod);
        }

        const int gridSize = 1000;

        var result = LombScarglePeriodogram.SearchFull(time, flux, minPeriod: 1.0, maxPeriod: 20.0, gridSize);

        Assert.True(result.IsSuccess);

        var normalizedPower = result.Value.Power * time.Length;

        var expectedFalseAlarmProbability = Math.Clamp(1.0 - Math.Pow(1.0 - Math.Exp(-normalizedPower), gridSize), 0.0, 1.0);

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
