using AstroLab.Core.Sources;

namespace AstroLab.Tests.Core;

public class ImageSegmenterTests
{
    private const int Width = 20;

    private const int Height = 20;

    /// <summary>A 20x20 background with a repeating 5..15 cycle (nonzero mesh background/RMS) and one 3x3, constant-value-1000 block.</summary>
    private static float[] BuildImageWithOneFlatBlock()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        for (var y = 8; y <= 10; y++)
        {
            for (var x = 8; x <= 10; x++)
            {
                pixels[(y * Width) + x] = 1000f;
            }
        }

        return pixels;
    }

    [Fact]
    public void Segment_FlatConstantBlock_IsOneSegmentNotDeblendedByPlateau()
    {
        var pixels = BuildImageWithOneFlatBlock();

        var result = ImageSegmenter.Segment(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        var segment = Assert.Single(result.Value);

        Assert.Equal(1, segment.SegmentId);

        Assert.Equal(9, segment.PixelCount);

        Assert.Equal(8, segment.MinX);

        Assert.Equal(10, segment.MaxX);

        Assert.Equal(8, segment.MinY);

        Assert.Equal(10, segment.MaxY);

        Assert.Equal(9.5, segment.CentroidX, precision: 6);

        Assert.Equal(9.5, segment.CentroidY, precision: 6);
    }

    [Fact]
    public void Segment_TwoDistinctPeaksAboveASaddle_AreDeblendedIntoTwoSegments()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        // A single 8-connected blob with two well-separated single-pixel peaks and a lower (but
        // still above-threshold) saddle between them.
        for (var x = 2; x <= 12; x++)
        {
            pixels[(5 * Width) + x] = 300f;
        }

        pixels[(5 * Width) + 3] = 1000f;

        pixels[(5 * Width) + 11] = 1000f;

        var result = ImageSegmenter.Segment(pixels, Width, Height, minimumArea: 1);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.Length);

        Assert.All(result.Value, segment => Assert.True(segment.PixelCount > 0));

        var totalPixels = result.Value.Sum(s => s.PixelCount);

        Assert.Equal(11, totalPixels);
    }

    [Fact]
    public void Segment_WithMinimumAreaAboveBlockSize_ExcludesTheBlock()
    {
        var pixels = BuildImageWithOneFlatBlock();

        var result = ImageSegmenter.Segment(pixels, Width, Height, minimumArea: 10);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Segment_OnUniformImage_ReturnsNoSegments()
    {
        var pixels = new float[Width * Height];

        Array.Fill(pixels, 5.0f);

        var result = ImageSegmenter.Segment(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Segment_IsDeterministicAcrossRepeatedCalls()
    {
        var pixels = BuildImageWithOneFlatBlock();

        var first = ImageSegmenter.Segment(pixels, Width, Height).Value;

        var second = ImageSegmenter.Segment(pixels, Width, Height).Value;

        Assert.Equal(first.Length, second.Length);

        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void Segment_RejectsMismatchedImageBounds()
    {
        var result = ImageSegmenter.Segment([1f, 2f, 3f], width: 2, height: 2);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.segmentation.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Segment_RejectsNonPositiveThreshold()
    {
        var pixels = BuildImageWithOneFlatBlock();

        var result = ImageSegmenter.Segment(pixels, Width, Height, thresholdSigma: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.segmentation.invalid_threshold", result.Error.Code);
    }

    [Fact]
    public void Segment_RejectsNonPositiveMinimumArea()
    {
        var pixels = BuildImageWithOneFlatBlock();

        var result = ImageSegmenter.Segment(pixels, Width, Height, minimumArea: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.segmentation.invalid_minimum_area", result.Error.Code);
    }
}
