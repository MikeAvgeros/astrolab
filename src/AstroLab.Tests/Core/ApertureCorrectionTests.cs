using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class ApertureCorrectionTests
{
    [Fact]
    public void Apply_ScalesMeasuredFluxByCorrectionFactor()
    {
        var (correctedFlux, correctedFluxUncertainty) = ApertureCorrection.Apply(measuredFlux: 100.0, correctionFactor: 1.2, measuredFluxUncertainty: null);

        Assert.Equal(120.0, correctedFlux, precision: 9);

        Assert.Null(correctedFluxUncertainty);
    }

    [Fact]
    public void Apply_WithUncertainty_ScalesUncertaintyByTheSameFactor()
    {
        var (correctedFlux, correctedFluxUncertainty) = ApertureCorrection.Apply(measuredFlux: 100.0, correctionFactor: 1.5, measuredFluxUncertainty: 4.0);

        Assert.Equal(150.0, correctedFlux, precision: 9);

        Assert.Equal(6.0, correctedFluxUncertainty!.Value, precision: 9);
    }
}
