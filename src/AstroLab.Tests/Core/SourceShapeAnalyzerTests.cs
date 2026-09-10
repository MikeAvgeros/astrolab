using AstroLab.Core.Sources;

namespace AstroLab.Tests.Core;

public class SourceShapeAnalyzerTests
{
    private const int Width = 12;

    private const int Height = 12;

    /// <summary>A 12x12 background with a repeating 5..15 cycle and one symmetric 3x3, constant-value 1000 block at columns/rows 4-6.</summary>
    private static float[] BuildImageWithSquareBlock()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        for (var y = 4; y <= 6; y++)
        {
            for (var x = 4; x <= 6; x++)
            {
                pixels[(y * Width) + x] = 1000f;
            }
        }

        return pixels;
    }

    [Fact]
    public void Analyze_SymmetricSquareBlock_IsNearlyCircularWithMatchingSourceId()
    {
        var pixels = BuildImageWithSquareBlock();

        var detectionResult = SourceDetector.Detect(pixels, Width, Height);

        var shapeResult = SourceShapeAnalyzer.Analyze(pixels, Width, Height);

        Assert.True(shapeResult.IsSuccess);

        var shape = Assert.Single(shapeResult.Value);

        Assert.Equal(Assert.Single(detectionResult.Value).Id, shape.Id);

        // A 3x3 constant block has zero product moment and equal Ixx/Iyy, so its axes must match exactly.
        Assert.Equal(shape.SemiMajorAxisPixels, shape.SemiMinorAxisPixels, precision: 9);

        Assert.Equal(0.0, shape.Ellipticity, precision: 9);

        Assert.True(shape.SemiMajorAxisPixels > 0.0);
    }

    [Fact]
    public void Analyze_ElongatedHorizontalBlock_ReportsMajorAxisAlongXAndNearZeroPositionAngle()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        // A 1x5 horizontal strip: elongated along x, so the major axis must dominate and theta ~ 0.
        for (var x = 3; x <= 7; x++)
        {
            pixels[(5 * Width) + x] = 1000f;
        }

        var result = SourceShapeAnalyzer.Analyze(pixels, Width, Height, minimumArea: 1);

        Assert.True(result.IsSuccess);

        var shape = Assert.Single(result.Value);

        Assert.True(shape.SemiMajorAxisPixels > shape.SemiMinorAxisPixels);

        Assert.True(shape.Ellipticity > 0.0);

        Assert.Equal(0.0, shape.PositionAngleDegrees, precision: 6);
    }

    [Fact]
    public void Analyze_SinglePixelSource_ReportsZeroSizeAndZeroEllipticity()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        pixels[(5 * Width) + 5] = 1000f;

        var result = SourceShapeAnalyzer.Analyze(pixels, Width, Height, minimumArea: 1);

        Assert.True(result.IsSuccess);

        var shape = Assert.Single(result.Value);

        Assert.Equal(0.0, shape.SemiMajorAxisPixels, precision: 9);

        Assert.Equal(0.0, shape.SemiMinorAxisPixels, precision: 9);

        Assert.Equal(0.0, shape.Ellipticity, precision: 9);
    }

    [Fact]
    public void Analyze_IsDeterministicAcrossRepeatedCalls()
    {
        var pixels = BuildImageWithSquareBlock();

        var first = SourceShapeAnalyzer.Analyze(pixels, Width, Height).Value;

        var second = SourceShapeAnalyzer.Analyze(pixels, Width, Height).Value;

        Assert.Equal(first.Length, second.Length);

        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void Analyze_RejectsMismatchedImageBounds()
    {
        var result = SourceShapeAnalyzer.Analyze([1f, 2f, 3f], width: 2, height: 2);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Analyze_OnUniformImage_ReturnsNoShapes()
    {
        var pixels = new float[Width * Height];

        Array.Fill(pixels, 5.0f);

        var result = SourceShapeAnalyzer.Analyze(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }
}
