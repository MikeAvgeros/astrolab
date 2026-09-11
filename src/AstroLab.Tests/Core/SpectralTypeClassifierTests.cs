using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectralTypeClassifierTests
{
    private const int SpectrumLength = 1000;

    /// <summary>
    /// A spectrum of exact-zero continuum with <paramref name="lineCount"/> isolated, well-separated
    /// single-bin spikes, so <see cref="SpectralLineDetector"/>'s zero-noise threshold flags exactly
    /// one line per spike and the resulting line density is exactly lineCount / SpectrumLength.
    /// </summary>
    private static double[] BuildSpectrumWithLineCount(int lineCount)
    {
        var spectrum = new double[SpectrumLength];

        var stride = SpectrumLength / (lineCount + 1);

        for (var i = 0; i < lineCount; i++)
        {
            spectrum[(i + 1) * stride] = 100.0;
        }

        return spectrum;
    }

    [Theory]
    [InlineData(3, "O")]
    [InlineData(8, "B")]
    [InlineData(15, "A")]
    [InlineData(25, "F")]
    [InlineData(35, "G")]
    [InlineData(50, "K")]
    [InlineData(60, "M")]
    public void Classify_LineDensity_MapsToExpectedSpectralType(int lineCount, string expectedType)
    {
        var spectrum = BuildSpectrumWithLineCount(lineCount);

        var result = SpectralTypeClassifier.Classify(spectrum);

        Assert.True(result.IsSuccess);

        Assert.Equal(expectedType, result.Value.SpectralType);
    }

    [Fact]
    public void Classify_ConfidenceIsWithinUnitRange()
    {
        var spectrum = BuildSpectrumWithLineCount(15);

        var result = SpectralTypeClassifier.Classify(spectrum);

        Assert.True(result.IsSuccess);

        Assert.InRange(result.Value.Confidence, 0.0, 1.0);
    }

    [Fact]
    public void Classify_RejectsEmptySpectrum()
    {
        var result = SpectralTypeClassifier.Classify([]);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.empty_spectrum", result.Error.Code);
    }
}
