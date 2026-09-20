using AstroLab.Core.Astrometry;

namespace AstroLab.Tests.Core;

public class SphericalCentroidTests
{
    [Fact]
    public void Compute_ClosePositionsAwayFromWraparound_MatchesArithmeticMean()
    {
        // For closely-spaced points, the spherical vector mean converges to the simple arithmetic
        // mean; wider separations legitimately pull the vector mean's latitude toward the pole,
        // which is correct spherical-geometry behaviour, not something this test should assert away.
        var positions = new[] { (RightAscensionDegrees: 179.9, DeclinationDegrees: 10.0), (RightAscensionDegrees: 180.1, DeclinationDegrees: 10.0) };

        var result = SphericalCentroid.Compute(positions);

        Assert.True(result.IsSuccess);
        Assert.Equal(180.0, result.Value.RightAscensionDegrees, precision: 6);
        Assert.Equal(10.0, result.Value.DeclinationDegrees, precision: 3);
    }

    [Fact]
    public void Compute_PositionsStraddlingZeroThreeSixty_WrapsCorrectlyInsteadOfAveragingToOppositeSide()
    {
        // 359.9 and 0.1 straddle the wraparound seam; the correct spherical mean sits near 0.0,
        // not near 180.0 (which a naive arithmetic mean of the raw degree values would produce).
        var positions = new[] { (RightAscensionDegrees: 359.9, DeclinationDegrees: 0.0), (RightAscensionDegrees: 0.1, DeclinationDegrees: 0.0) };

        var result = SphericalCentroid.Compute(positions);

        Assert.True(result.IsSuccess);
        Assert.True(
            result.Value.RightAscensionDegrees is < 1.0 or > 359.0,
            $"Expected the centroid near the 0/360 seam, got {result.Value.RightAscensionDegrees}.");
        Assert.Equal(0.0, result.Value.DeclinationDegrees, precision: 6);
    }

    [Fact]
    public void Compute_EmptyPositions_ReturnsValidationFailure()
    {
        var result = SphericalCentroid.Compute([]);

        Assert.True(result.IsFailure);
        Assert.Equal("astrometry.centroid.no_positions", result.Error.Code);
    }
}
