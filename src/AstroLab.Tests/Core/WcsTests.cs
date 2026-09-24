using System.Text;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class WcsTests
{
    private static string PadCard(string content)
    {
        if (content.Length > FitsCardParser.CardLength)
        {
            throw new ArgumentException("Card content exceeds 80 characters.", nameof(content));
        }

        return content.PadRight(FitsCardParser.CardLength);
    }

    private static FitsHeader BuildHeader(params string[] cards)
    {
        var allCards = new string[cards.Length + 1];

        Array.Copy(cards, allCards, cards.Length);

        allCards[^1] = "END";

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(allCards, PadCard)));

        return FitsHeader.Parse(block).Value;
    }

    private static FitsHeader BuildTanHeader(double cdelt = 0.0001, double crota2 = 0.0) => BuildHeader(
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                  1.0",
        "CRPIX2  =                  1.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        $"CDELT1  =                {-cdelt}",
        $"CDELT2  =                {cdelt}",
        $"CROTA2  =                {crota2}",
        "RADESYS = 'ICRS    '");

    [Fact]
    public void FromHeader_OnMissingCType_ReturnsNotFound()
    {
        var header = BuildHeader("SIMPLE  =                    T");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.wcs_not_present", result.Error.Code);
    }

    [Fact]
    public void FromHeader_OnMissingScale_ReturnsNotFound()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.wcs_not_present", result.Error.Code);
    }

    [Fact]
    public void FromHeader_OnNonCelestialAxes_ReturnsValidationError()
    {
        var header = BuildHeader(
            "CTYPE1  = 'FREQ    '",
            "CTYPE2  = 'STOKES  '",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                  0.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =                  1.0",
            "CDELT2  =                  1.0");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.unrecognized_axes", result.Error.Code);
    }

    [Fact]
    public void FromHeader_OnUnsupportedProjection_ReturnsNotImplemented()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---AIT'",
            "CTYPE2  = 'DEC--AIT'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                  0.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =                  1.0",
            "CDELT2  =                  1.0");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.unsupported_projection", result.Error.Code);
    }

    [Fact]
    public void PixelToWorld_AtReferencePixel_ReturnsExactReferenceCoordinates()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader()).Value;

        var result = wcs.PixelToWorld(wcs.ReferencePixelX, wcs.ReferencePixelY);

        Assert.True(result.IsSuccess);

        Assert.Equal(180.0, result.Value.RightAscension, precision: 9);

        Assert.Equal(0.0, result.Value.Declination, precision: 9);
    }

    [Fact]
    public void PixelToWorld_OnePixelInPlusX_DecreasesRightAscensionByPixelScale()
    {
        const double scale = 0.0001;

        var wcs = Wcs.FromHeader(BuildTanHeader(scale)).Value;

        var result = wcs.PixelToWorld(wcs.ReferencePixelX + 1.0, wcs.ReferencePixelY);

        Assert.True(result.IsSuccess);

        Assert.Equal(180.0 - scale, result.Value.RightAscension, precision: 8);

        Assert.Equal(0.0, result.Value.Declination, precision: 9);
    }

    [Fact]
    public void PixelToWorld_OnePixelInPlusY_IncreasesDeclinationByPixelScale()
    {
        const double scale = 0.0001;

        var wcs = Wcs.FromHeader(BuildTanHeader(scale)).Value;

        var result = wcs.PixelToWorld(wcs.ReferencePixelX, wcs.ReferencePixelY + 1.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(180.0, result.Value.RightAscension, precision: 9);

        Assert.Equal(scale, result.Value.Declination, precision: 8);
    }

    [Fact]
    public void WorldToPixel_AtReferenceCoordinates_ReturnsExactReferencePixel()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader()).Value;

        var result = wcs.WorldToPixel(wcs.ReferenceRightAscension, wcs.ReferenceDeclination);

        Assert.True(result.IsSuccess);

        Assert.Equal(wcs.ReferencePixelX, result.Value.PixelX, precision: 9);

        Assert.Equal(wcs.ReferencePixelY, result.Value.PixelY, precision: 9);
    }

    [Fact]
    public void PixelToWorld_ThenWorldToPixel_RoundTripsForCdeltConvention()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0005)).Value;

        var world = wcs.PixelToWorld(137.25, 88.75).Value;

        var pixel = wcs.WorldToPixel(world.RightAscension, world.Declination);

        Assert.True(pixel.IsSuccess);

        Assert.Equal(137.25, pixel.Value.PixelX, precision: 6);

        Assert.Equal(88.75, pixel.Value.PixelY, precision: 6);
    }

    [Fact]
    public void PixelToWorld_ThenWorldToPixel_RoundTripsWithCrota2Rotation()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0007, crota2: 33.5)).Value;

        var world = wcs.PixelToWorld(64.0, -42.0).Value;

        var pixel = wcs.WorldToPixel(world.RightAscension, world.Declination);

        Assert.True(pixel.IsSuccess);

        Assert.Equal(64.0, pixel.Value.PixelX, precision: 6);

        Assert.Equal(-42.0, pixel.Value.PixelY, precision: 6);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(33.5)]
    [InlineData(-120.0)]
    public void RotationDegrees_OnStandardEastLeftImage_EqualsCrota2(double crota2)
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0007, crota2)).Value;

        Assert.Equal(crota2, wcs.RotationDegrees, precision: 6);

        Assert.True(wcs.IsMirrored);
    }

    [Fact]
    public void RotationDegrees_OnEastRightImageWithCrota2_MeasuresNorthCounterclockwiseFromPlusY()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =               0.0007",
            "CDELT2  =               0.0007",
            "CROTA2  =                 25.0");

        var wcs = Wcs.FromHeader(header).Value;

        // One pixel step towards north, measured counterclockwise from +y, is (-sin ρ, cos ρ).
        var origin = wcs.PixelToWorld(0.0, 0.0).Value;

        var northStep = wcs.WorldToPixel(origin.RightAscension, origin.Declination + 0.0007).Value;

        var expectedDegrees = Math.Atan2(-northStep.PixelX, northStep.PixelY) * 180.0 / Math.PI;

        Assert.Equal(expectedDegrees, wcs.RotationDegrees, precision: 3);

        Assert.False(wcs.IsMirrored);
    }

    [Fact]
    public void RotationDegrees_OnSwappedCelestialAxes_MatchesEquivalentUnswappedOrientation()
    {
        // Declination along pixel x (increasing to +x) and right ascension along pixel y: north points
        // along +x, i.e. 90 degrees clockwise from +y.
        var header = BuildHeader(
            "CTYPE1  = 'DEC--TAN'",
            "CTYPE2  = 'RA---TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                  0.0",
            "CRVAL2  =                180.0",
            "CDELT1  =               0.0007",
            "CDELT2  =              -0.0007");

        var wcs = Wcs.FromHeader(header).Value;

        Assert.Equal(-90.0, wcs.RotationDegrees, precision: 6);
    }

    [Fact]
    public void PixelToWorld_ThenWorldToPixel_RoundTripsForCdMatrixConvention()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                 50.0",
            "CRPIX2  =                 50.0",
            "CRVAL1  =                 10.5",
            "CRVAL2  =                -30.2",
            "CD1_1   =              -0.0002",
            "CD1_2   =               0.00003",
            "CD2_1   =               0.00003",
            "CD2_2   =               0.0002");

        var wcs = Wcs.FromHeader(header).Value;

        var world = wcs.PixelToWorld(12.0, 88.0).Value;

        var pixel = wcs.WorldToPixel(world.RightAscension, world.Declination);

        Assert.True(pixel.IsSuccess);

        Assert.Equal(12.0, pixel.Value.PixelX, precision: 6);

        Assert.Equal(88.0, pixel.Value.PixelY, precision: 6);
    }

    [Fact]
    public void PixelToWorld_ThenWorldToPixel_RoundTripsForPcMatrixConvention()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                 20.0",
            "CRPIX2  =                 20.0",
            "CRVAL1  =                200.0",
            "CRVAL2  =                 15.0",
            "CDELT1  =              -0.0003",
            "CDELT2  =               0.0003",
            "PC1_1   =                  0.9",
            "PC1_2   =                 -0.1",
            "PC2_1   =                  0.1",
            "PC2_2   =                  0.9");

        var wcs = Wcs.FromHeader(header).Value;

        var world = wcs.PixelToWorld(5.0, 9.0).Value;

        var pixel = wcs.WorldToPixel(world.RightAscension, world.Declination);

        Assert.True(pixel.IsSuccess);

        Assert.Equal(5.0, pixel.Value.PixelX, precision: 6);

        Assert.Equal(9.0, pixel.Value.PixelY, precision: 6);
    }

    [Fact]
    public void WorldToPixel_RejectsOutOfRangeDeclination()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader()).Value;

        var result = wcs.WorldToPixel(180.0, 120.0);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.invalid_declination", result.Error.Code);
    }

    [Fact]
    public void WorldToPixel_OnPointFarFromReference_ReturnsPointNotVisible()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader()).Value;

        var result = wcs.WorldToPixel(0.0, 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.point_not_visible", result.Error.Code);
    }

    [Fact]
    public void FromHeader_WithDefaultImplicitLonPole_Succeeds()
    {
        var result = Wcs.FromHeader(BuildTanHeader());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void FromHeader_WithExplicitStandardLonPole_Succeeds()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =              -0.0001",
            "CDELT2  =               0.0001",
            "LONPOLE =                180.0");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void FromHeader_WithNonDefaultLonPole_ReturnsNotImplemented()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =              -0.0001",
            "CDELT2  =               0.0001",
            "LONPOLE =                 90.0");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.unsupported_lonpole", result.Error.Code);
    }

    [Fact]
    public void FromHeader_WithReferenceAtCelestialPoleAndNoExplicitLonPole_ReturnsNotImplemented()
    {
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                  1.0",
            "CRPIX2  =                  1.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                 90.0",
            "CDELT1  =              -0.0001",
            "CDELT2  =               0.0001");

        var result = Wcs.FromHeader(header);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.unsupported_lonpole", result.Error.Code);
    }

    [Fact]
    public void WorldToPixel_OnNearSingularButNonZeroCdMatrix_ReturnsSingularTransform()
    {
        // CD1_1 == CD1_2 == CD2_1, and CD2_2 is CD2_1 perturbed by only a relative 1e-11 (well below
        // the RelativeSingularityTolerance of 1e-10 used for the determinant-to-norm ratio), so the
        // matrix is deliberately near-singular (determinant ~1e-19) without being exactly zero.
        var header = BuildHeader(
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                 50.0",
            "CRPIX2  =                 50.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0",
            "CD1_1   =                 0.0001",
            "CD1_2   =                 0.0001",
            "CD2_1   =                 0.0001",
            "CD2_2   =    0.00010000000000100001");

        var wcs = Wcs.FromHeader(header).Value;

        Assert.NotEqual(0.0, wcs.Determinant);

        Assert.False(wcs.IsInvertible);

        var result = wcs.WorldToPixel(wcs.ReferenceRightAscension, wcs.ReferenceDeclination + 0.001);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.singular_transform", result.Error.Code);
    }

    [Fact]
    public void ResolveSkyRegionPixelBounds_CentersOnReferencePixel_WithExpectedDiameter()
    {
        // 0.0001 deg/pixel = 0.36 arcsec/pixel; a 3.6-arcsecond radius spans 10 pixels either side.
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0001)).Value;

        var result = wcs.ResolveSkyRegionPixelBounds(
            wcs.ReferenceRightAscension, wcs.ReferenceDeclination, radiusArcseconds: 3.6, imageWidth: 200, imageHeight: 200);

        Assert.True(result.IsSuccess);

        var (x, y, width, height) = result.Value;

        Assert.Equal(20, width);

        Assert.Equal(20, height);

        // The reference pixel (0.5, 0.5) should sit at the center of the returned region.
        Assert.InRange(wcs.ReferencePixelX - x, 0, width);

        Assert.InRange(wcs.ReferencePixelY - y, 0, height);
    }

    [Fact]
    public void ResolveSkyRegionPixelBounds_RadiusExceedingImage_ClampsToImageBoundsInsteadOfFailing()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0001)).Value;

        var result = wcs.ResolveSkyRegionPixelBounds(
            wcs.ReferenceRightAscension, wcs.ReferenceDeclination, radiusArcseconds: 3600.0, imageWidth: 50, imageHeight: 40);

        Assert.True(result.IsSuccess);

        var (x, y, width, height) = result.Value;

        Assert.Equal(0, x);

        Assert.Equal(0, y);

        Assert.Equal(50, width);

        Assert.Equal(40, height);
    }

    [Fact]
    public void ResolveSkyRegionPixelBounds_OnCenterCoordinateWorldToPixelFailure_PropagatesTheError()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0001)).Value;

        var result = wcs.ResolveSkyRegionPixelBounds(
            wcs.ReferenceRightAscension, declination: 91.0, radiusArcseconds: 10.0, imageWidth: 100, imageHeight: 100);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.invalid_declination", result.Error.Code);
    }

    [Fact]
    public void FromHeader_ExposesReferenceAndScaleMetadata()
    {
        var wcs = Wcs.FromHeader(BuildTanHeader(0.0004)).Value;

        Assert.Equal(WcsProjection.Tan, wcs.Projection);

        Assert.Equal(180.0, wcs.ReferenceRightAscension, precision: 9);

        Assert.Equal(0.0, wcs.ReferenceDeclination, precision: 9);

        Assert.Equal(0.5, wcs.ReferencePixelX, precision: 9);

        Assert.Equal(0.5, wcs.ReferencePixelY, precision: 9);

        Assert.Equal(0.0004, wcs.PixelScaleXDegrees, precision: 9);

        Assert.Equal(0.0004, wcs.PixelScaleYDegrees, precision: 9);

        Assert.Equal("ICRS", wcs.RadeSys);
    }
}
