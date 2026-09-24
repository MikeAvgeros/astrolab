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

    private static float[] BuildUniformDiskImage(int width, int height, double centerX, double centerY, double radius)
    {
        var pixels = BuildBackground(width, height);

        PaintDisk(pixels, width, height, centerX, centerY, radius, 500f);

        return pixels;
    }

    /// <summary>A de Vaucouleurs (Sersic n = 4) profile, I(r) = I_e exp(-7.669((r/R_e)^(1/4) - 1)), on the cyclic background.</summary>
    private static float[] BuildDeVaucouleursImage(int size, double centerX, double centerY, double effectiveRadius)
    {
        const double surfaceBrightnessAtEffectiveRadius = 200.0;

        var pixels = BuildBackground(size, size);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var radius = Math.Sqrt(Math.Pow(x + 0.5 - centerX, 2) + Math.Pow(y + 0.5 - centerY, 2));

                pixels[(y * size) + x] += (float)(surfaceBrightnessAtEffectiveRadius * Math.Exp(-7.669 * (Math.Pow(radius / effectiveRadius, 0.25) - 1.0)));
            }
        }

        return pixels;
    }

    [Fact]
    public void Analyze_UniformDisk_ReportsHalfLightRadiusAndSecondMomentEllipticity()
    {
        const int size = 90;

        var pixels = BuildUniformDiskImage(size, size, 45.0, 45.0, 15.0);

        var morphologyResult = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 45.0, 45.0);

        Assert.True(morphologyResult.IsSuccess);

        var shape = Assert.Single(SourceShapeAnalyzer.Analyze(pixels, size, size).Value);

        // Half the flux of a uniform disk of radius R lies within R / sqrt(2).
        Assert.NotNull(morphologyResult.Value.EffectiveRadiusPixels);

        Assert.InRange(morphologyResult.Value.EffectiveRadiusPixels!.Value, 15.0 / Math.Sqrt(2.0) - 0.3, 15.0 / Math.Sqrt(2.0) + 0.3);

        Assert.Equal(shape.Ellipticity, morphologyResult.Value.Ellipticity, precision: 6);
    }

    [Fact]
    public void Analyze_ExponentialDisk_ReportsHalfLightRadiusAndDiskConcentration()
    {
        // An exponential disk with scale length h has R_e = 1.678 h and a Petrosian R90/R50 of ~2.3.
        const int size = 200;

        const double scaleLength = 6.0;

        var pixels = new float[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var radius = Math.Sqrt(Math.Pow(x + 0.5 - 100.0, 2) + Math.Pow(y + 0.5 - 100.0, 2));

                pixels[y * size + x] = (float)(10.0 + 1000.0 * Math.Exp(-radius / scaleLength) + (x + y) % 3);
            }
        }

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 100.0, 100.0);

        Assert.True(result.IsSuccess);

        Assert.InRange(result.Value.EffectiveRadiusPixels!.Value, 1.678 * scaleLength * 0.93, 1.678 * scaleLength * 1.07);

        Assert.InRange(result.Value.ConcentrationIndex!.Value, 2.1, 2.5);

        Assert.Equal("Spiral", result.Value.MorphologicalType);
    }

    [Fact]
    public void Analyze_RequestedPositionAwayFromEverySource_ReturnsNotFound()
    {
        const int size = 150;

        var pixels = BuildUniformDiskImage(size, size, 40.0, 40.0, 10.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 120.0, 120.0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.galaxymorphology.no_source_at_position", result.Error.Code);
    }

    [Fact]
    public void Analyze_NonFiniteRequestedPosition_ReturnsValidationError()
    {
        const int size = 90;

        var pixels = BuildUniformDiskImage(size, size, 45.0, 45.0, 15.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, double.NaN, 45.0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.galaxymorphology.invalid_position", result.Error.Code);
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
    public void Analyze_UniformDisk_ExposesConcentrationIndexBelowEllipticalThreshold()
    {
        const int size = 90;

        var pixels = BuildUniformDiskImage(size, size, 45.0, 45.0, 15.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 45.0, 45.0);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.ConcentrationIndex);

        Assert.True(result.Value.ConcentrationIndex < 2.6);
    }

    [Fact]
    public void Analyze_DeVaucouleursProfile_ClassifiesAsElliptical()
    {
        const int size = 200;

        var pixels = BuildDeVaucouleursImage(size, 100.0, 100.0, effectiveRadius: 8.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 100.0, 100.0);

        Assert.True(result.IsSuccess);

        Assert.Equal("Elliptical", result.Value.MorphologicalType);
    }

    [Fact]
    public void Analyze_DeVaucouleursProfile_ExposesPetrosianConcentrationNearPublishedValue()
    {
        // Blanton et al. (2001): an untruncated n = 4 profile has a Petrosian R90/R50 of about 3.3.
        const int size = 200;

        var pixels = BuildDeVaucouleursImage(size, 100.0, 100.0, effectiveRadius: 8.0);

        var result = GalaxyMorphologyAnalyzer.Analyze(pixels, size, size, 100.0, 100.0);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.ConcentrationIndex);

        Assert.InRange(result.Value.ConcentrationIndex!.Value, 2.9, 3.6);
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

        Assert.Equal(soloResult.Value.EffectiveRadiusPixels!.Value, combinedResult.Value.EffectiveRadiusPixels!.Value, precision: 6);

        Assert.Equal("Spiral", combinedResult.Value.MorphologicalType);
    }

    [Fact]
    public void MethodName_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(GalaxyMorphologyAnalyzer.MethodName));
    }
}
