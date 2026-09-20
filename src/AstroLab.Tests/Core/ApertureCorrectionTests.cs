using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class ApertureCorrectionTests
{
    [Fact]
    public void Apply_ScalesMeasuredFluxByCorrectionFactor()
    {
        var result = ApertureCorrection.Apply(measuredFlux: 100.0, correctionFactor: 1.2, measuredFluxUncertainty: null);

        Assert.True(result.IsSuccess);

        Assert.Equal(120.0, result.Value.CorrectedFlux, precision: 9);

        Assert.Null(result.Value.CorrectedFluxUncertainty);
    }

    [Fact]
    public void Apply_WithUncertainty_ScalesUncertaintyByTheSameFactor()
    {
        var result = ApertureCorrection.Apply(measuredFlux: 100.0, correctionFactor: 1.5, measuredFluxUncertainty: 4.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(150.0, result.Value.CorrectedFlux, precision: 9);

        Assert.Equal(6.0, result.Value.CorrectedFluxUncertainty!.Value, precision: 9);
    }

    [Fact]
    public void Apply_OverflowingToInfinity_ReturnsFailure()
    {
        var result = ApertureCorrection.Apply(measuredFlux: 1e300, correctionFactor: 1e300, measuredFluxUncertainty: null);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.aperturecorrection.overflow", result.Error.Code);
    }

    [Fact]
    public void Apply_NonFiniteCorrectionFactor_ReturnsFailure()
    {
        var result = ApertureCorrection.Apply(measuredFlux: 100.0, correctionFactor: double.PositiveInfinity, measuredFluxUncertainty: null);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.aperturecorrection.invalid_factor", result.Error.Code);
    }
}
