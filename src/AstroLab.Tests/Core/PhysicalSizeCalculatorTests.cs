using AstroLab.Core.Astrometry;

namespace AstroLab.Tests.Core;

public class PhysicalSizeCalculatorTests
{
    [Fact]
    public void ComputeAstronomicalUnits_OneArcsecAtOneParsec_EqualsOneAu()
    {
        var result = PhysicalSizeCalculator.ComputeAstronomicalUnits(angularSizeArcsec: 1.0, distanceParsecs: 1.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(1.0, result.Value, precision: 9);
    }

    [Fact]
    public void ComputeAstronomicalUnits_ScalesLinearlyWithAngularSizeAndDistance()
    {
        var result = PhysicalSizeCalculator.ComputeAstronomicalUnits(angularSizeArcsec: 2.5, distanceParsecs: 10.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(25.0, result.Value, precision: 9);
    }

    [Fact]
    public void ComputeAstronomicalUnits_RejectsNonPositiveAngularSize()
    {
        var result = PhysicalSizeCalculator.ComputeAstronomicalUnits(angularSizeArcsec: 0.0, distanceParsecs: 10.0);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.physicalsize.invalid_angular_size", result.Error.Code);
    }

    [Fact]
    public void ComputeAstronomicalUnits_RejectsNonPositiveDistance()
    {
        var result = PhysicalSizeCalculator.ComputeAstronomicalUnits(angularSizeArcsec: 1.0, distanceParsecs: -5.0);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.physicalsize.invalid_distance", result.Error.Code);
    }
}
