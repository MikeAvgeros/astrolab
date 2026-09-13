using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class EquivalentWidthCalculatorTests
{
    [Fact]
    public void Calculate_FlatSpectrum_ReturnsZero()
    {
        double[] wavelengths = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [10.0, 10.0, 10.0, 10.0, 10.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value, precision: 9);
    }

    [Fact]
    public void Calculate_EmissionBump_ReturnsNegativeEquivalentWidth()
    {
        double[] wavelengths = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [10.0, 10.0, 100.0, 10.0, 10.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsSuccess);

        Assert.Equal(-9.0, result.Value, precision: 9);
    }

    [Fact]
    public void Calculate_AbsorptionDip_ReturnsPositiveEquivalentWidth()
    {
        double[] wavelengths = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [10.0, 10.0, 1.0, 10.0, 10.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.9, result.Value, precision: 9);
    }

    [Fact]
    public void Calculate_DescendingWavelengthAxis_ReturnsSameSignAsAscending()
    {
        double[] wavelengths = [4.0, 3.0, 2.0, 1.0, 0.0];

        double[] flux = [10.0, 10.0, 1.0, 10.0, 10.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.9, result.Value, precision: 9);
    }

    [Fact]
    public void Calculate_RejectsDegenerateSinglePointWindow()
    {
        double[] wavelengths = [0.0];

        double[] flux = [10.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.equivalent_width.insufficient_points", result.Error.Code);
    }

    [Fact]
    public void Calculate_RejectsZeroPseudoContinuum()
    {
        double[] wavelengths = [0.0, 1.0, 2.0];

        double[] flux = [0.0, 5.0, 0.0];

        var result = EquivalentWidthCalculator.Calculate(wavelengths, flux);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.equivalent_width.zero_continuum", result.Error.Code);
    }
}
