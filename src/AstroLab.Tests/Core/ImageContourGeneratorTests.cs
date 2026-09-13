using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageContourGeneratorTests
{
    // 5x5 grid, all zero except a value of 100 at the center (2,2): a single symmetric bump.
    private static float[] BuildCenteredBumpGrid()
    {
        var pixels = new float[25];

        pixels[(2 * 5) + 2] = 100f;

        return pixels;
    }

    [Fact]
    public void Trace_CenteredBump_ProducesSegmentsNearTheCenter()
    {
        var pixels = BuildCenteredBumpGrid();

        var result = ImageContourGenerator.Trace(pixels, 5, 5, level: 50.0);

        Assert.True(result.IsSuccess);

        Assert.NotEmpty(result.Value);

        foreach (var segment in result.Value)
        {
            foreach (var (x, y) in segment)
            {
                var distanceFromCenter = Math.Sqrt(Math.Pow(x - 2, 2) + Math.Pow(y - 2, 2));

                Assert.True(distanceFromCenter <= 1.5, $"Point ({x},{y}) is farther from the center than expected.");
            }
        }
    }

    [Fact]
    public void Trace_FlatImageAtHigherLevel_ProducesNoSegments()
    {
        var pixels = new float[16];

        Array.Fill(pixels, 10f);

        var result = ImageContourGenerator.Trace(pixels, 4, 4, level: 20.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Trace_LevelAboveDataRange_ProducesEmptyResultNotError()
    {
        var pixels = BuildCenteredBumpGrid();

        var result = ImageContourGenerator.Trace(pixels, 5, 5, level: 1000.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Trace_MismatchedBounds_ReturnsValidationError()
    {
        var pixels = new float[10];

        var result = ImageContourGenerator.Trace(pixels, 4, 4, level: 1.0);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.contours.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Trace_TooSmallForAnyCell_ReturnsEmptyResult()
    {
        var pixels = new float[] { 1f };

        var result = ImageContourGenerator.Trace(pixels, 1, 1, level: 0.5);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void SuggestLevels_EvenlySpacesAcrossPercentileBounds()
    {
        var pixels = new float[101];

        for (var i = 0; i < 101; i++)
        {
            pixels[i] = i;
        }

        var result = ImageContourGenerator.SuggestLevels(pixels, levelCount: 5);

        Assert.True(result.IsSuccess);

        var levels = result.Value;

        Assert.Equal(5, levels.Length);

        for (var i = 1; i < levels.Length; i++)
        {
            Assert.True(levels[i] > levels[i - 1]);
        }

        Assert.True(levels[0] > 0.0 && levels[0] < 10.0);

        Assert.True(levels[^1] > 90.0 && levels[^1] < 100.0);
    }

    [Fact]
    public void SuggestLevels_NonPositiveLevelCount_ReturnsValidationError()
    {
        float[] pixels = [1f, 2f, 3f];

        var result = ImageContourGenerator.SuggestLevels(pixels, levelCount: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.contours.invalid_level_count", result.Error.Code);
    }
}
