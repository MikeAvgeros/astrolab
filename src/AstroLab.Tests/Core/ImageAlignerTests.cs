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

    [Theory]
    [InlineData(0.0, 90.0, false)]
    [InlineData(15.0, -40.0, false)]
    [InlineData(0.0, 90.0, true)]
    [InlineData(-30.0, 60.0, true)]
    public void AlignByWcs_OnRotatedGrids_MapsTargetPixelsOntoTheSameSkyPositionInTheReference(
        double targetCrota2, double referenceCrota2, bool cdelt1Positive)
    {
        const double PixelTolerance = 0.01;

        var target = BuildRotatedWcs(crPix1: 50.0, crPix2: 40.0, crVal1: 150.0, crVal2: 20.0, targetCrota2, cdelt1Positive);

        var reference = BuildRotatedWcs(crPix1: 70.0, crPix2: 65.0, crVal1: 150.002, crVal2: 20.001, referenceCrota2, cdelt1Positive);

        var result = ImageAligner.AlignByWcs(target, reference);

        Assert.True(result.IsSuccess);

        var transform = result.Value;

        var radians = transform.RotationDegrees * Math.PI / 180.0;

        foreach (var (x, y) in new[] { (0.0, 0.0), (99.0, 0.0), (0.0, 79.0), (60.0, 30.0) })
        {
            var world = target.PixelToWorld(x, y).Value;

            var expected = reference.WorldToPixel(world.RightAscension, world.Declination).Value;

            var mappedX = transform.Scale * (Math.Cos(radians) * x - Math.Sin(radians) * y) + transform.OffsetX;

            var mappedY = transform.Scale * (Math.Sin(radians) * x + Math.Cos(radians) * y) + transform.OffsetY;

            Assert.InRange(mappedX, expected.PixelX - PixelTolerance, expected.PixelX + PixelTolerance);

            Assert.InRange(mappedY, expected.PixelY - PixelTolerance, expected.PixelY + PixelTolerance);
        }
    }

    private static Wcs BuildRotatedWcs(
        double crPix1, double crPix2, double crVal1, double crVal2, double crota2, bool cdelt1Positive)
    {
        const double cdelt = 0.0001;

        string[] cards =
        [
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            $"CRPIX1  =                {crPix1}",
            $"CRPIX2  =                {crPix2}",
            $"CRVAL1  =                {crVal1}",
            $"CRVAL2  =                {crVal2}",
            $"CDELT1  =                {(cdelt1Positive ? cdelt : -cdelt)}",
            $"CDELT2  =                {cdelt}",
            $"CROTA2  =                {crota2}",
            "RADESYS = 'ICRS    '",
            "END",
        ];

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

        return Wcs.FromHeader(FitsHeader.Parse(block).Value).Value;
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
    public void AlignByWcs_OnMismatchedParity_ReturnsValidationError()
    {
        var target = BuildWcs(crPix1: 1.0, crPix2: 1.0, crVal1: 180.0, crVal2: 0.0, cdelt: 0.0001);

        // Both CDELT values positive (rather than the usual CDELT1 negative / CDELT2 positive
        // convention) gives this WCS the opposite parity (non-negative determinant) from `target`.
        var reference = BuildMirroredWcs(crPix1: 11.0, crPix2: 21.0, crVal1: 180.0, crVal2: 0.0, cdelt: 0.0001);

        Assert.NotEqual(target.IsMirrored, reference.IsMirrored);

        var result = ImageAligner.AlignByWcs(target, reference);

        Assert.True(result.IsFailure);

        Assert.Equal("images.align.mismatched_parity", result.Error.Code);
    }

    private static Wcs BuildMirroredWcs(double crPix1, double crPix2, double crVal1, double crVal2, double cdelt) =>
        BuildWcsWithCdelt1Sign(crPix1, crPix2, crVal1, crVal2, cdelt, cdelt1Positive: true);

    private static Wcs BuildWcsWithCdelt1Sign(
        double crPix1, double crPix2, double crVal1, double crVal2, double cdelt, bool cdelt1Positive)
    {
        string[] cards =
        [
            "CTYPE1  = 'RA---TAN'",
            "CTYPE2  = 'DEC--TAN'",
            $"CRPIX1  =                {crPix1}",
            $"CRPIX2  =                {crPix2}",
            $"CRVAL1  =                {crVal1}",
            $"CRVAL2  =                {crVal2}",
            $"CDELT1  =                {(cdelt1Positive ? cdelt : -cdelt)}",
            $"CDELT2  =                {cdelt}",
            "RADESYS = 'ICRS    '",
            "END",
        ];

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

        var header = FitsHeader.Parse(block).Value;

        return Wcs.FromHeader(header).Value;
    }

    [Fact]
    public void AlignBySourceCentroids_ReturnsMedianOffsetOfConsistentPairs()
    {
        ImmutableArray<DetectedSource> targetSources = [Source(10.0, 10.0, 1), Source(20.0, 30.0, 2), Source(50.0, 5.0, 3)];

        ImmutableArray<DetectedSource> referenceSources = [Source(14.2, 13.1, 1), Source(23.8, 32.9, 2), Source(54.0, 8.0, 3)];

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

    [Fact]
    public void AlignBySourceCentroids_SourceLeavingAndEnteringField_IgnoresUnmatchedSourcesAndRankChanges()
    {
        // The reference field is shifted by (+5, -3): target source 2 falls off the reference frame and a
        // new source appears, so ranks no longer line up one-to-one.
        ImmutableArray<DetectedSource> targetSources =
            [Source(40.0, 40.0, 1), Source(2.0, 60.0, 2), Source(70.0, 20.0, 3), Source(25.0, 80.0, 4)];

        ImmutableArray<DetectedSource> referenceSources =
            [Source(45.0, 37.0, 1), Source(90.0, 90.0, 2), Source(75.0, 17.0, 3), Source(30.0, 77.0, 4)];

        var result = ImageAligner.AlignBySourceCentroids(targetSources, referenceSources);

        Assert.True(result.IsSuccess);

        Assert.Equal(5.0, result.Value.OffsetX, precision: 6);

        Assert.Equal(-3.0, result.Value.OffsetY, precision: 6);
    }

    [Fact]
    public void AlignBySourceCentroids_NoTwoSourcesAgreeOnAnOffset_ReturnsValidationError()
    {
        ImmutableArray<DetectedSource> targetSources = [Source(10.0, 10.0, 1), Source(20.0, 30.0, 2)];

        ImmutableArray<DetectedSource> referenceSources = [Source(15.0, 12.0, 1), Source(23.0, 38.0, 2)];

        var result = ImageAligner.AlignBySourceCentroids(targetSources, referenceSources);

        Assert.True(result.IsFailure);

        Assert.Equal("images.align.no_consistent_offset", result.Error.Code);
    }
}
