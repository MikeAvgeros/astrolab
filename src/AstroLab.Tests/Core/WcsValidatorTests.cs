using System.Text;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class WcsValidatorTests
{
    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static FitsHeader BuildHeader(params string[] cards)
    {
        var allCards = new string[cards.Length + 1];

        Array.Copy(cards, allCards, cards.Length);

        allCards[^1] = "END";

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(allCards, PadCard)));

        return FitsHeader.Parse(block).Value;
    }

    private static Wcs BuildOrthogonalWcs() => Wcs.FromHeader(BuildHeader(
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                 50.0",
        "CRPIX2  =                 50.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        "CDELT1  =              -0.0002",
        "CDELT2  =               0.0002",
        "RADESYS = 'ICRS    '")).Value;

    private static Wcs BuildSkewedWcs() => Wcs.FromHeader(BuildHeader(
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                 50.0",
        "CRPIX2  =                 50.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        "CD1_1   =              -0.0002",
        "CD1_2   =               0.0001",
        "CD2_1   =              0.00005",
        "CD2_2   =               0.0002")).Value;

    private static Wcs BuildSingularWcs() => Wcs.FromHeader(BuildHeader(
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                 50.0",
        "CRPIX2  =                 50.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        "CD1_1   =                  0.0",
        "CD1_2   =                  0.0",
        "CD2_1   =                  0.0",
        "CD2_2   =                  0.0")).Value;

    [Fact]
    public void Validate_OrthogonalSquarePixelWcs_ReportsNoIssues()
    {
        var wcs = BuildOrthogonalWcs();

        var result = WcsValidator.Validate(wcs, imageWidth: 100, imageHeight: 100);

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.True(report.IsValid);

        Assert.True(report.IsInvertible);

        Assert.Equal(1.0, report.PixelScaleRatio, precision: 6);

        Assert.True(report.RoundTripErrorPixels < 0.001);
    }

    [Fact]
    public void Validate_SkewedAxes_FlagsNonOrthogonalAxes()
    {
        var wcs = BuildSkewedWcs();

        var result = WcsValidator.Validate(wcs, imageWidth: 100, imageHeight: 100);

        Assert.True(result.IsSuccess);

        Assert.Contains("astrometry.wcs_validation.non_orthogonal_axes", result.Value.Issues);
    }

    [Fact]
    public void Validate_SingularTransform_ReportsNotInvertibleAndRoundTripNotComputable()
    {
        var wcs = BuildSingularWcs();

        var result = WcsValidator.Validate(wcs, imageWidth: 100, imageHeight: 100);

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.False(report.IsValid);

        Assert.False(report.IsInvertible);

        Assert.Equal(0.0, report.Determinant, precision: 12);

        Assert.Contains("astrometry.wcs_validation.singular_transform", report.Issues);
    }

    [Fact]
    public void Validate_NonPositiveImageDimensions_ReturnsValidationError()
    {
        var wcs = BuildOrthogonalWcs();

        var result = WcsValidator.Validate(wcs, imageWidth: 0, imageHeight: 100);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.wcs_validation.invalid_image_dimensions", result.Error.Code);
    }
}
