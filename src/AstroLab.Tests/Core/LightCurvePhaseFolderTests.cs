using AstroLab.Core.TimeSeries;

namespace AstroLab.Tests.Core;

public class LightCurvePhaseFolderTests
{
    [Fact]
    public void Fold_WithZeroReferenceEpoch_ComputesFractionalCycle()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0, 2.5, 3.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0, 4.0, 5.0];

        Span<double> phase = stackalloc double[5];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 2.0, referenceEpoch: 0.0, phase);

        Assert.True(result.IsSuccess);

        double[] expected = [0.0, 0.5, 0.0, 0.25, 0.5];

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], phase[i], precision: 9);
        }
    }

    [Fact]
    public void Fold_AlwaysWrapsIntoZeroToOneRange()
    {
        ReadOnlySpan<double> time = [-3.0, -1.0, 0.0, 10.0, 100.0];

        ReadOnlySpan<double> flux = [1.0, 2.0, 3.0, 4.0, 5.0];

        Span<double> phase = stackalloc double[5];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 3.0, referenceEpoch: 0.5, phase);

        Assert.True(result.IsSuccess);

        foreach (var value in phase)
        {
            Assert.True(value >= 0.0 && value < 1.0, $"Expected phase in [0, 1), got {value}.");
        }
    }

    [Fact]
    public void Fold_ShiftsWithReferenceEpoch()
    {
        ReadOnlySpan<double> time = [1.0];

        ReadOnlySpan<double> flux = [1.0];

        Span<double> phase = stackalloc double[1];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 4.0, referenceEpoch: 1.0, phase);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, phase[0], precision: 9);
    }

    [Fact]
    public void Fold_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> time = [0.0, 1.0, 2.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        Span<double> phase = stackalloc double[3];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 1.0, referenceEpoch: 0.0, phase);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.phasefold.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Fold_RejectsEmptySeries()
    {
        var result = LightCurvePhaseFolder.Fold([], [], period: 1.0, referenceEpoch: 0.0, []);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.phasefold.empty_series", result.Error.Code);
    }

    [Fact]
    public void Fold_RejectsOutputLengthMismatch()
    {
        ReadOnlySpan<double> time = [0.0, 1.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        Span<double> phase = stackalloc double[1];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 1.0, referenceEpoch: 0.0, phase);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.phasefold.output_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Fold_RejectsNonPositivePeriod()
    {
        ReadOnlySpan<double> time = [0.0, 1.0];

        ReadOnlySpan<double> flux = [1.0, 2.0];

        Span<double> phase = stackalloc double[2];

        var result = LightCurvePhaseFolder.Fold(time, flux, period: 0.0, referenceEpoch: 0.0, phase);

        Assert.True(result.IsFailure);

        Assert.Equal("timeseries.phasefold.invalid_period", result.Error.Code);
    }
}
