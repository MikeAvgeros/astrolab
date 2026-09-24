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
    public void EstimateFluxUncertainty_WithBackgroundPixelCount_IncludesBackgroundEstimateError()
    {
        // Howell's n_pix * (1 + n_pix / n_B) * sigma^2 term: 25 aperture pixels, 100 annulus pixels.
        var result = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0, backgroundPixelCount: 100);

        Assert.True(result.IsSuccess);

        Assert.Equal(Math.Sqrt(25.0 * (1.0 + 25.0 / 100.0) * 4.0), result.Value, precision: 9);
    }

    [Fact]
    public void EstimateFluxUncertainty_WithGainAndBackgroundPixelCount_InflatesOnlyTheBackgroundTerm()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 2.0, detectorGain: 2.0, backgroundPixelCount: 50);

        Assert.True(result.IsSuccess);

        Assert.Equal(Math.Sqrt(1000.0 / 2.0 + 25.0 * 4.0 * (1.0 + 25.0 / 50.0)), result.Value, precision: 9);
    }

    [Theory]
    [InlineData("GAIN    =                  2.5", 2.5)]
    [InlineData("GAIN    =                  0.0", null)]
    [InlineData("GAIN    =                 -1.0", null)]
    [InlineData("OBJECT  = 'M31'", null)]
    public void ReadDetectorGain_TreatsMissingOrNonPositiveGainAsAbsent(string card, double? expected)
    {
        var block = System.Text.Encoding.ASCII.GetBytes(card.PadRight(80) + "END".PadRight(80));

        var header = AstroLab.Core.Fits.FitsHeader.Parse(block).Value;

        Assert.Equal(expected, PhotometricUncertainty.ReadDetectorGain(header));
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
    public void EstimateFluxUncertainty_WithGainAndReadNoise_DoesNotDoubleCountReadNoiseAlreadyInMeasuredSkySigma()
    {
        var withGainOnlyResult = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 5.0, detectorGain: 2.0);

        var withGainAndReadNoiseResult = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 1000.0, apertureArea: 25.0, skyBackgroundSigma: 5.0, detectorGain: 2.0, readNoiseElectrons: 4.0);

        Assert.True(withGainOnlyResult.IsSuccess);

        Assert.True(withGainAndReadNoiseResult.IsSuccess);

        // skyBackgroundSigma is measured directly from the image, so it already includes the read
        // noise physically present in the data. Since it is large enough to already account for the
        // supplied read noise, supplying readNoiseElectrons on top must not change the result.
        Assert.Equal(withGainOnlyResult.Value, withGainAndReadNoiseResult.Value, precision: 9);
    }

    [Fact]
    public void EstimateFluxUncertainty_WithReadNoiseExceedingMeasuredSkySigma_UsesReadNoiseAsAFloor()
    {
        var result = PhotometricUncertainty.EstimateFluxUncertainty(
            netFlux: 0.0, apertureArea: 25.0, skyBackgroundSigma: 0.1, detectorGain: 2.0, readNoiseElectrons: 10.0);

        Assert.True(result.IsSuccess);

        var expectedReadNoiseVariance = 25.0 * (10.0 * 10.0) / (2.0 * 2.0);

        Assert.Equal(Math.Sqrt(expectedReadNoiseVariance), result.Value, precision: 9);
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
