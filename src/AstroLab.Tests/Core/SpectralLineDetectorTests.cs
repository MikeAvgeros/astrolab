using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectralLineDetectorTests
{
    [Fact]
    public void Detect_SingleEmissionSpikeAboveFlatContinuum_ReportsExactPositionFluxAndFwhm()
    {
        ReadOnlySpan<double> spectrum = [30.0, 30.0, 30.0, 30.0, 300.0, 30.0, 30.0, 30.0, 30.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 3.0);

        Assert.True(result.IsSuccess);

        var line = Assert.Single(result.Value);

        Assert.Equal(4.0, line.Position, precision: 9);

        Assert.Equal(270.0, line.Flux, precision: 9);

        Assert.Equal(1.0, line.Fwhm, precision: 9);
    }

    [Fact]
    public void Detect_FlatContinuumWithNoDeviation_FindsNoLines()
    {
        ReadOnlySpan<double> spectrum = [10.0, 10.0, 10.0, 10.0, 10.0];

        var result = SpectralLineDetector.Detect(spectrum);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_WeakBumpBelowThreshold_IsNotReportedAsALine()
    {
        ReadOnlySpan<double> spectrum = [60.0, 80.0, 100.0, 120.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 5.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_RejectsEmptySpectrum()
    {
        var result = SpectralLineDetector.Detect([]);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.empty_spectrum", result.Error.Code);
    }

    [Fact]
    public void Detect_RejectsNonPositiveSignificanceSigma()
    {
        ReadOnlySpan<double> spectrum = [10.0, 20.0, 10.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.invalid_threshold", result.Error.Code);
    }
}
