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
