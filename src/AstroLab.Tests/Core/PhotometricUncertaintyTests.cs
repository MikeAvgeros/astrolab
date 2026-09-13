using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class PhotometricUncertaintyTests
{
    [Fact]
    public void EstimateFluxUncertainty_WithoutGain_MatchesSkyNoiseOnlyModel()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(InstrumentalPhotometry.EstimateFluxUncertainty(2.0, 25.0), result.Value, precision: 9);
    }

    [Fact]
    public void EstimateFluxUncertainty_WithGain_IncludesSourceShotNoise()
    {
        var withoutGainResult = PhotometricUncertainty.EstimateFluxUncertainty(netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0);

        var withGainResult = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0, detectorGain: 2.0);

        Assert.True(withoutGainResult.IsSuccess);

        Assert.True(withGainResult.IsSuccess);

        Assert.True(withGainResult.Value > 0 && double.IsFinite(withGainResult.Value));

        Assert.NotEqual(withoutGainResult.Value, withGainResult.Value, precision: 6);
    }

    [Fact]
    public void EstimateFluxUncertainty_WithReadNoiseButNoGain_ReturnsSkyNoiseOnlyModel()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0, detectorGain: null, readNoiseElectrons: 5.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(InstrumentalPhotometry.EstimateFluxUncertainty(2.0, 25.0), result.Value, precision: 9);
    }

    [Fact]
    public void EstimateFluxUncertainty_RejectsNonPositiveApertureArea()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(netFlux: 1000.0, apertureArea: 0.0, skyBackgroundSigma: 2.0);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.uncertainty.invalid_aperture_area", result.Error.Code);
    }

    [Fact]
    public void EstimateFluxUncertainty_RejectsNonPositiveGain()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0, detectorGain: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.uncertainty.invalid_gain", result.Error.Code);
    }

    [Fact]
    public void ComputeSignalToNoiseRatio_ReturnsFluxOverUncertainty()
    {
        var result = PhotometricUncertainty.ComputeSignalToNoiseRatio(netFlux: 100.0, fluxUncertainty: 4.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(25.0, result.Value, precision: 9);
    }

    [Fact]
    public void ComputeSignalToNoiseRatio_RejectsNonPositiveUncertainty()
    {
        var result = PhotometricUncertainty.ComputeSignalToNoiseRatio(netFlux: 100.0, fluxUncertainty: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("photometry.snr.invalid_flux_uncertainty", result.Error.Code);
    }
}
