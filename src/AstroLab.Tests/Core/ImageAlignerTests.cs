using System.Collections.Immutable;
using System.Text;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;
using AstroLab.Core.Sources;

namespace AstroLab.Tests.Core;

public class ImageAlignerTests
{
    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static Wcs BuildWcs(double crPix1, double crPix2, double crVal1, double crVal2, double cdelt = 0.0001)
    {
        string[] cards =
        [
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            $"CRPIX1  =                {crPix1}",
            $"CRPIX2  =                {crPix2}",
            $"CRVAL1  =                {crVal1}",
            $"CRVAL2  =                {crVal2}",
            $"CDELT1  =                {-cdelt}",
            $"CDELT2  =                {cdelt}",
            "RADESYS = 'ICRS    '",
            "END",
        ];

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

        var header = FitsHeader.Parse(block).Value;

        return Wcs.FromHeader(header).Value;
    }

    private static DetectedSource Source(double pixelX, double pixelY, int id = 1) =>
        DetectedSource.Create(id, pixelX, pixelY, pixelCount: 1, peakValue: 100.0, totalFlux: 100.0, background: 0.0, signalToNoiseRatio: 10.0);

    [Fact]
    public void AlignByWcs_OnPureTranslationBetweenGrids_ReturnsExpectedOffsetWithNoRotationOrScale()
    {
        var target = BuildWcs(crPix1: 1.0, crPix2: 1.0, crVal1: 180.0, crVal2: 0.0);

        var reference = BuildWcs(crPix1: 11.0, crPix2: 21.0, crVal1: 180.0, crVal2: 0.0);

        var result = ImageAligner.AlignByWcs(target, reference);

        Assert.True(result.IsSuccess);

        var transform = result.Value;

        Assert.Equal(10.0, transform.OffsetX, precision: 6);

        Assert.Equal(20.0, transform.OffsetY, precision: 6);

        Assert.Equal(0.0, transform.RotationDegrees, precision: 6);

        Assert.Equal(1.0, transform.Scale, precision: 6);
    }

    [Fact]
    public void AlignByWcs_OnDifferentPixelScales_ReturnsScaleRatio()
    {
        var target = BuildWcs(crPix1: 1.0, crPix2: 1.0, crVal1: 180.0, crVal2: 0.0, cdelt: 0.0002);

        var reference = BuildWcs(crPix1: 1.0, crPix2: 1.0, crVal1: 180.0, crVal2: 0.0, cdelt: 0.0001);

        var result = ImageAligner.AlignByWcs(target, reference);

        Assert.True(result.IsSuccess);

        Assert.Equal(2.0, result.Value.Scale, precision: 6);
    }

    [Fact]
    public void AlignBySourceCentroids_AveragesPerAxisOffsetOverMatchedPairs()
    {
        ImmutableArray<DetectedSource> targetSources = [Source(10.0, 10.0, 1), Source(20.0, 30.0, 2)];

        ImmutableArray<DetectedSource> referenceSources = [Source(15.0, 12.0, 1), Source(23.0, 34.0, 2)];

        var result = ImageAligner.AlignBySourceCentroids(targetSources, referenceSources);

        Assert.True(result.IsSuccess);

        var transform = result.Value;

        Assert.Equal(4.0, transform.OffsetX, precision: 6);

        Assert.Equal(3.0, transform.OffsetY, precision: 6);

        Assert.Equal(0.0, transform.RotationDegrees, precision: 6);

        Assert.Equal(1.0, transform.Scale, precision: 6);
    }

    [Fact]
    public void AlignBySourceCentroids_OnEmptySources_ReturnsValidationError()
    {
        var result = ImageAligner.AlignBySourceCentroids(ImmutableArray<DetectedSource>.Empty, [Source(1.0, 1.0)]);

        Assert.True(result.IsFailure);

        Assert.Equal("images.align.no_reference_points", result.Error.Code);
    }
}
