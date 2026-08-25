using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectrumExtractorTests
{
    private static float[] CreateRowGradientImage(int width, int height)
    {
        var image = new float[width * height];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                image[row * width + col] = row + 1;
            }
        }

        return image;
    }

    [Fact]
    public void ExtractBoxcar_WithFullyCoveredRows_SumsExactRowValues()
    {
        var image = CreateRowGradientImage(width: 5, height: 5);

        ReadOnlySpan<double> traceCenters = [2.0, 2.0, 2.0, 2.0, 2.0];

        Span<double> spectrum = stackalloc double[5];

        var result = SpectrumExtractor.ExtractBoxcar(image, 5, 5, DispersionAxis.Horizontal, traceCenters,
            apertureHalfWidth: 1.0, spectrum);

        Assert.True(result.IsSuccess);

        foreach (var flux in spectrum)
        {
            Assert.Equal(5.0, flux, precision: 6);
        }
    }

    [Fact]
    public void ExtractBoxcar_WeightsPartialEdgeCoverage()
    {
        var image = CreateRowGradientImage(width: 3, height: 5);

        ReadOnlySpan<double> traceCenters = [1.5, 1.5, 1.5];

        Span<double> spectrum = stackalloc double[3];

        var result = SpectrumExtractor.ExtractBoxcar(image, 3, 5, DispersionAxis.Horizontal, traceCenters,
            apertureHalfWidth: 1.0, spectrum);

        Assert.True(result.IsSuccess);

        Assert.Equal(4.0, spectrum[0], precision: 6);
    }

    [Fact]
    public void ExtractBoxcar_RejectsTraceLengthMismatch()
    {
        var image = new float[25];

        ReadOnlySpan<double> traceCenters = [1.0, 2.0];

        Span<double> spectrum = stackalloc double[5];

        var result = SpectrumExtractor.ExtractBoxcar(image, 5, 5, DispersionAxis.Horizontal, traceCenters, 1.0, spectrum);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.trace_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void SubtractBackground_SubtractsElementwise()
    {
        Span<double> spectrum = [10.0, 20.0, 30.0];

        ReadOnlySpan<double> background = [1.0, 2.0, 3.0];

        var result = SpectrumExtractor.SubtractBackground(spectrum, background);

        Assert.True(result.IsSuccess);

        Assert.Equal([9.0, 18.0, 27.0], spectrum.ToArray());
    }

    [Fact]
    public void SubtractBackground_RejectsLengthMismatch()
    {
        Span<double> spectrum = [1.0, 2.0];

        ReadOnlySpan<double> background = [1.0];

        var result = SpectrumExtractor.SubtractBackground(spectrum, background);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.background_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void EvaluateWavelength_AppliesLinearDispersionSolution()
    {
        ReadOnlySpan<double> coefficients = [500.0, 2.0];

        var wavelength = SpectrumExtractor.EvaluateWavelength(10, coefficients);

        Assert.Equal(520.0, wavelength, precision: 6);
    }

    [Fact]
    public void ComputeWavelengths_AppliesSolutionAcrossAllPixels()
    {
        ReadOnlySpan<double> coefficients = [0.0, 1.0, 0.1];

        ReadOnlySpan<double> pixelIndices = [0.0, 1.0, 2.0];

        Span<double> wavelengths = stackalloc double[3];

        var result = SpectrumExtractor.ComputeWavelengths(pixelIndices, coefficients, wavelengths);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, wavelengths[0], precision: 6);

        Assert.Equal(1.1, wavelengths[1], precision: 6);

        Assert.Equal(2.4, wavelengths[2], precision: 6);
    }
}
