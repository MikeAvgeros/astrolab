using AstroLab.Core.Astrometry;

namespace AstroLab.Tests.Core;

public class AngularSeparationTests
{
    private const double ArcsecPerDegree = 3600.0;

    [Fact]
    public void ComputeArcseconds_SamePoint_ReturnsZero()
    {
        var result = AngularSeparation.ComputeArcseconds(180.0, 10.0, 180.0, 10.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value, precision: 6);
    }

    [Fact]
    public void ComputeArcseconds_OneDegreeAlongCelestialEquator_ReturnsOneDegreeInArcseconds()
    {
        var result = AngularSeparation.ComputeArcseconds(0.0, 0.0, 1.0, 0.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(ArcsecPerDegree, result.Value, precision: 6);
    }

    [Fact]
    public void ComputeArcseconds_OneDegreeInDeclination_ReturnsOneDegreeInArcseconds()
    {
        var result = AngularSeparation.ComputeArcseconds(180.0, 0.0, 180.0, 1.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(ArcsecPerDegree, result.Value, precision: 6);
    }

    [Fact]
    public void ComputeArcseconds_IsSymmetric()
    {
        var forward = AngularSeparation.ComputeArcseconds(10.0, 5.0, 20.0, -15.0);

        var backward = AngularSeparation.ComputeArcseconds(20.0, -15.0, 10.0, 5.0);

        Assert.True(forward.IsSuccess);

        Assert.True(backward.IsSuccess);

        Assert.Equal(forward.Value, backward.Value, precision: 9);
    }

    [Theory]
    [InlineData(180.0, 95.0, 180.0, 0.0)]
    [InlineData(180.0, 0.0, 180.0, -95.0)]
    public void ComputeArcseconds_DeclinationOutOfRange_ReturnsValidationError(
        double rightAscension1, double declination1, double rightAscension2, double declination2)
    {
        var result = AngularSeparation.ComputeArcseconds(rightAscension1, declination1, rightAscension2, declination2);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.invalid_declination", result.Error.Code);
    }
}
