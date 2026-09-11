using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class InstrumentalPhotometryTests
{
    [Fact]
    public void EstimateFluxUncertainty_ScalesSkySigmaBySquareRootOfArea()
    {
        var uncertainty = InstrumentalPhotometry.EstimateFluxUncertainty(skyBackgroundSigma: 2.0, apertureArea: 25.0);

        Assert.Equal(10.0, uncertainty, precision: 9);
    }

    [Fact]
    public void ComputeMagnitude_ComputesMagnitudeAndPropagatedUncertainty()
    {
        var result = InstrumentalPhotometry.ComputeMagnitude(netFlux: 100.0, fluxUncertainty: 5.0, zeroPoint: 25.0);

        Assert.True(result.IsSuccess);

        var expectedMagnitude = 25.0 - (2.5 * Math.Log10(100.0));

        var expectedUncertainty = (2.5 / Math.Log(10.0)) * (5.0 / 100.0);

        Assert.Equal(expectedMagnitude, result.Value.Magnitude, precision: 9);

        Assert.Equal(expectedUncertainty, result.Value.MagnitudeUncertainty, precision: 9);
    }

    [Fact]
    public void ComputeMagnitude_RejectsNonPositiveNetFlux()
    {
        var result = InstrumentalPhotometry.ComputeMagnitude(netFlux: 0.0, fluxUncertainty: 1.0, zeroPoint: 25.0);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.non_positive_net_flux", result.Error.Code);
    }

    [Fact]
    public void ComputeMagnitude_RejectsNegativeFluxUncertainty()
    {
        var result = InstrumentalPhotometry.ComputeMagnitude(netFlux: 100.0, fluxUncertainty: -1.0, zeroPoint: 25.0);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.invalid_flux_uncertainty", result.Error.Code);
    }

    [Fact]
    public void ComputeDifferentialMagnitude_CombinesUncertaintiesInQuadrature()
    {
        var (differential, uncertainty) = InstrumentalPhotometry.ComputeDifferentialMagnitude(
            targetMagnitude: 15.0, targetMagnitudeUncertainty: 0.03, comparisonMagnitude: 15.5, comparisonMagnitudeUncertainty: 0.04);

        Assert.Equal(-0.5, differential, precision: 9);

        Assert.Equal(0.05, uncertainty, precision: 9);
    }

    [Fact]
    public void ComputeSurfaceBrightness_OneArcsecSquareAperture_AddsNothingToMagnitude()
    {
        var pixelScaleDegrees = 1.0 / 3600.0;

        var result = InstrumentalPhotometry.ComputeSurfaceBrightness(
            magnitude: 15.0, apertureAreaPixels: 1.0, pixelScaleXDegrees: pixelScaleDegrees, pixelScaleYDegrees: pixelScaleDegrees);

        Assert.True(result.IsSuccess);

        Assert.Equal(15.0, result.Value, precision: 9);
    }

    [Fact]
    public void ComputeSurfaceBrightness_LargerArea_IncreasesSurfaceBrightnessMagnitude()
    {
        var pixelScaleDegrees = 1.0 / 3600.0;

        var result = InstrumentalPhotometry.ComputeSurfaceBrightness(
            magnitude: 15.0, apertureAreaPixels: 100.0, pixelScaleXDegrees: pixelScaleDegrees, pixelScaleYDegrees: pixelScaleDegrees);

        Assert.True(result.IsSuccess);

        Assert.Equal(15.0 + (2.5 * Math.Log10(100.0)), result.Value, precision: 9);
    }

    [Fact]
    public void ComputeSurfaceBrightness_RejectsNonPositiveArea()
    {
        var result = InstrumentalPhotometry.ComputeSurfaceBrightness(
            magnitude: 15.0, apertureAreaPixels: 0.0, pixelScaleXDegrees: 0.001, pixelScaleYDegrees: 0.001);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.surfacebrightness.invalid_area", result.Error.Code);
    }

    [Fact]
    public void ComputeSurfaceBrightness_RejectsNonPositivePixelScale()
    {
        var result = InstrumentalPhotometry.ComputeSurfaceBrightness(
            magnitude: 15.0, apertureAreaPixels: 10.0, pixelScaleXDegrees: 0.0, pixelScaleYDegrees: 0.001);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.surfacebrightness.invalid_pixel_scale", result.Error.Code);
    }
}
