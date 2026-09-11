using AstroLab.Core.Sources;

namespace AstroLab.Tests.Core;

public class GalaxyMorphologyAnalyzerTests
{
    private const int BackgroundBase = 5;

    private const int BackgroundCyclePeriod = 11;

    private static float[] BuildBackground(int width, int height)
    {
        var pixels = new float[width * height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = BackgroundBase + (i % BackgroundCyclePeriod);
        }

        return pixels;
    }

    private static void PaintDisk(float[] pixels, int width, int height, double centerX, double centerY, double radius, float value)
    {
        var radiusSquared = radius * radius;

        for (var y = 0; y < height; y++)
        {
            var dy = y + 0.5 - centerY;

            for (var x = 0; x < width; x++)
            {
                var dx = x + 0.5 - centerX;

                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    pixels[(y * width) + x] = value;
                }
            }
        }
    }

    private static void PaintAnnulus(
        float[] pixels, int width, int height, double centerX, double centerY, double innerRadius, double outerRadius, float value)
    {
        var innerRadiusSquared = innerRadius * innerRadius;

        var outerRadiusSquared = outerRadius * outerRadius;

        for (var y = 0; y < height; y++)
        {
            var dy = y + 0.5 - centerY;

            for (var x = 0; x < width; x++)
            {
                var dx = x + 0.5 - centerX;

                var distanceSquared = (dx * dx) + (dy * dy);

                if (distanceSquared > innerRadiusSquared && distanceSquared <= outerRadiusSquared)
                {
                    pixels[(y * width) + x] = value;
                }
            }
        }
    }

    private static float[] BuildUniformDiskImage(int width, int height, double centerX, double centerY, double radius)
    {
        var pixels = BuildBackground(width, height);

        PaintDisk(pixels, width, height, centerX, centerY, radius, 500f);

        return pixels;
    }

    /// <summary>A bright, compact core surrounded by a much fainter (but still detectable) extended halo: a steeply centrally-concentrated profile.</summary>
    private static float[] BuildCoreHaloImage(int width, int height, double centerX, double centerY, double coreRadius, double haloRadius)
    {
        var pixels = BuildBackground(width, height);

        PaintAnnulus(pixels, width, height, centerX, centerY, coreRadius, haloRadius, 60f);

        PaintDisk(pixels, width, height, centerX, centerY, coreRadius, 2000f);

        return pixels;
    }

    [Fact]
    public void Analyze_UniformDisk_MatchesSecondMomentShapeFromSourceShapeAnalyzer()
    {
        const int size = 90;

        var pixels = BuildUniformDiskImage(size, size, 45.0, 45.0, 15.0);

        var morphologyResult = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 45.0, 45.0);

        Assert.True(morphologyResult.IsSuccess);

        var shapeResult = SourceShapeAnalyzer.Analyze(pixels, size, size);

        var shape = Assert.Single(shapeResult.Value);

        var expectedEffectiveRadius = Math.Sqrt(shape.SemiMajorAxisPixels * shape.SemiMinorAxisPixels);

        Assert.Equal(expectedEffectiveRadius, morphologyResult.Value.EffectiveRadiusPixels, precision: 6);

        Assert.Equal(shape.Ellipticity, morphologyResult.Value.Ellipticity, precision: 6);
    }

    [Fact]
    public void Analyze_UniformDisk_ClassifiesAsSpiral()
    {
        const int size = 90;

        var pixels = BuildUniformDiskImage(size, size, 45.0, 45.0, 15.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 45.0, 45.0);

        Assert.True(result.IsSuccess);

        Assert.Equal("Spiral", result.Value.MorphologicalType);
    }

    [Fact]
    public void Analyze_CoreDominatedProfile_ClassifiesAsElliptical()
    {
        const int size = 90;

        var pixels = BuildCoreHaloImage(size, size, 45.0, 45.0, coreRadius: 4.0, haloRadius: 20.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 45.0, 45.0);

        Assert.True(result.IsSuccess);

        Assert.Equal("Elliptical", result.Value.MorphologicalType);
    }

    [Fact]
    public void Analyze_NoSourceDetected_ReturnsNotFound()
    {
        const int size = 40;

        var pixels = BuildBackground(size, size);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 20.0, 20.0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.galaxymorphology.no_source_found", result.Error.Code);
    }

    [Fact]
    public void Analyze_MultipleSources_SelectsNearestToRequestedPosition()
    {
        const int size = 150;

        var soloPixels = BuildBackground(size, size);

        PaintDisk(soloPixels, size, size, 40.0, 40.0, 10.0, 500f);

        var soloResult = GalaxyMorphologyAnalyzer.Analyze(soloPixels, size, size, 40.0, 40.0);

        var combinedPixels = BuildBackground(size, size);

        PaintDisk(combinedPixels, size, size, 40.0, 40.0, 10.0, 500f);

        PaintDisk(combinedPixels, size, size, 110.0, 110.0, 15.0, 500f);

        var combinedResult = GalaxyMorphologyAnalyzer.Analyze(combinedPixels, size, size, 40.0, 40.0);

        Assert.True(soloResult.IsSuccess);

        Assert.True(combinedResult.IsSuccess);

        Assert.Equal(soloResult.Value.EffectiveRadiusPixels, combinedResult.Value.EffectiveRadiusPixels, precision: 6);

        Assert.Equal("Spiral", combinedResult.Value.MorphologicalType);
    }
}
