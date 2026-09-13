using System.Text;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class WcsGridGeneratorTests
{
    private const int ImageWidth = 100;
    private const int ImageHeight = 100;

    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static Wcs BuildTanWcs()
    {
        string[] cards =
        [
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            "CRPIX1  =                 50.0",
            "CRPIX2  =                 50.0",
            "CRVAL1  =                180.0",
            "CRVAL2  =                  0.0",
            "CDELT1  =            -0.0002778",
            "CDELT2  =             0.0002778",
            "RADESYS = 'ICRS    '",
            "END",
        ];

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

        var header = FitsHeader.Parse(block).Value;

        return Wcs.FromHeader(header).Value;
    }

    [Fact]
    public void GenerateGridLines_AllPointsFallWithinImageBounds()
    {
        var wcs = BuildTanWcs();

        var result = WcsGridGenerator.GenerateGridLines(wcs, ImageWidth, ImageHeight);

        Assert.True(result.IsSuccess);

        var grid = result.Value;

        Assert.NotEmpty(grid.RightAscensionLines);

        Assert.NotEmpty(grid.DeclinationLines);

        foreach (var line in grid.RightAscensionLines.Concat(grid.DeclinationLines))
        {
            Assert.True(line.Length >= 2);

            foreach (var (x, y) in line)
            {
                // A sub-millipixel tolerance is allowed at the image edge (see BoundsTolerancePixels
                // in WcsGridGenerator) to absorb floating-point noise from the WCS round-trip.
                Assert.InRange(x, -0.001, ImageWidth + 0.001);

                Assert.InRange(y, -0.001, ImageHeight + 0.001);
            }
        }
    }

    [Fact]
    public void GenerateGridLines_RespectsRequestedLineCount()
    {
        var wcs = BuildTanWcs();

        var result = WcsGridGenerator.GenerateGridLines(wcs, ImageWidth, ImageHeight, linesPerAxis: 4);

        Assert.True(result.IsSuccess);

        Assert.True(result.Value.RightAscensionLines.Length <= 4);

        Assert.True(result.Value.DeclinationLines.Length <= 4);
    }

    [Fact]
    public void GenerateGridLines_NonPositiveDimensions_ReturnsValidationError()
    {
        var wcs = BuildTanWcs();

        var result = WcsGridGenerator.GenerateGridLines(wcs, 0, ImageHeight);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.wcsgrid.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void GenerateGridLines_NonPositiveLineCount_ReturnsValidationError()
    {
        var wcs = BuildTanWcs();

        var result = WcsGridGenerator.GenerateGridLines(wcs, ImageWidth, ImageHeight, linesPerAxis: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("astrometry.wcsgrid.invalid_line_count", result.Error.Code);
    }
}
