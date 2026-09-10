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
    public void ApplyFluxCalibration_DividesElementwise()
    {
        Span<double> spectrum = [100.0, 200.0, 300.0];

        ReadOnlySpan<double> sensitivity = [2.0, 4.0, 5.0];

        var result = SpectrumExtractor.ApplyFluxCalibration(spectrum, sensitivity);

        Assert.True(result.IsSuccess);

        Assert.Equal([50.0, 50.0, 60.0], spectrum.ToArray());
    }

    [Fact]
    public void ApplyFluxCalibration_RejectsLengthMismatch()
    {
        Span<double> spectrum = [1.0, 2.0];

        ReadOnlySpan<double> sensitivity = [1.0];

        var result = SpectrumExtractor.ApplyFluxCalibration(spectrum, sensitivity);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.flux_calibration.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void ApplyFluxCalibration_RejectsNonPositiveSensitivity()
    {
        Span<double> spectrum = [1.0, 2.0];

        ReadOnlySpan<double> sensitivity = [1.0, 0.0];

        var result = SpectrumExtractor.ApplyFluxCalibration(spectrum, sensitivity);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.flux_calibration.invalid_sensitivity", result.Error.Code);
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

    [Fact]
    public void FitDispersionSolution_TwoPoints_RecoversExactLinearFit()
    {
        ReadOnlySpan<double> pixelPositions = [0.0, 10.0];

        ReadOnlySpan<double> knownWavelengths = [500.0, 520.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsSuccess);

        Assert.Equal([500.0, 2.0], result.Value.Coefficients.Select(c => Math.Round(c, 9)).ToArray());

        Assert.Equal(0.0, result.Value.ResidualRms, precision: 6);
    }

    [Fact]
    public void FitDispersionSolution_FourPointsOnAQuadratic_RecoversExactQuadraticFitWithZeroCubicTerm()
    {
        ReadOnlySpan<double> pixelPositions = [0.0, 1.0, 2.0, 3.0];

        ReadOnlySpan<double> knownWavelengths = [1.0, 3.5, 7.0, 11.5];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsSuccess);

        Assert.Equal(4, result.Value.Coefficients.Length);

        Assert.Equal(1.0, result.Value.Coefficients[0], precision: 6);

        Assert.Equal(2.0, result.Value.Coefficients[1], precision: 6);

        Assert.Equal(0.5, result.Value.Coefficients[2], precision: 6);

        Assert.Equal(0.0, result.Value.Coefficients[3], precision: 6);

        Assert.Equal(0.0, result.Value.ResidualRms, precision: 6);
    }

    [Fact]
    public void FitDispersionSolution_OverdeterminedSystem_ResidualRmsIsConsistentWithReturnedCoefficients()
    {
        ReadOnlySpan<double> pixelPositions = [0.0, 1.0, 2.0, 3.0, 4.0];

        ReadOnlySpan<double> knownWavelengths = [500.0, 502.0, 504.0, 506.0, 509.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsSuccess);

        var expectedRms = 0.0;

        for (var i = 0; i < pixelPositions.Length; i++)
        {
            var residual = SpectrumExtractor.EvaluateWavelength(pixelPositions[i], result.Value.Coefficients) - knownWavelengths[i];

            expectedRms += residual * residual;
        }

        expectedRms = Math.Sqrt(expectedRms / pixelPositions.Length);

        Assert.Equal(expectedRms, result.Value.ResidualRms, precision: 9);

        Assert.True(result.Value.ResidualRms > 0.0);
    }

    [Fact]
    public void FitDispersionSolution_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> pixelPositions = [0.0, 1.0];

        ReadOnlySpan<double> knownWavelengths = [500.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.calibration_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void FitDispersionSolution_RejectsFewerThanTwoPoints()
    {
        ReadOnlySpan<double> pixelPositions = [0.0];

        ReadOnlySpan<double> knownWavelengths = [500.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.calibration_insufficient_points", result.Error.Code);
    }

    [Fact]
    public void FitDispersionSolution_RejectsNonFiniteValue()
    {
        ReadOnlySpan<double> pixelPositions = [0.0, double.NaN];

        ReadOnlySpan<double> knownWavelengths = [500.0, 520.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.calibration_non_finite_value", result.Error.Code);
    }

    [Fact]
    public void FitDispersionSolution_RejectsSingularSystemFromDuplicatePixelPositions()
    {
        ReadOnlySpan<double> pixelPositions = [5.0, 5.0];

        ReadOnlySpan<double> knownWavelengths = [500.0, 510.0];

        var result = SpectrumExtractor.FitDispersionSolution(pixelPositions, knownWavelengths);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.calibration_singular_system", result.Error.Code);
    }
}
