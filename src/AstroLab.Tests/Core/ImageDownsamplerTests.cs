using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageDownsamplerTests
{
    [Fact]
    public void ComputeFactor_OnImageWithinBounds_ReturnsOne()
    {
        var result = ImageDownsampler.ComputeFactor(width: 100, height: 50, maxDimension: 4096);

        Assert.True(result.IsSuccess);

        Assert.Equal(1, result.Value);
    }

    [Fact]
    public void ComputeFactor_OnOversizedImage_RoundsUp()
    {
        var result = ImageDownsampler.ComputeFactor(width: 5000, height: 100, maxDimension: 4096);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void ComputeFactor_RejectsNonPositiveMaxDimension()
    {
        var result = ImageDownsampler.ComputeFactor(width: 100, height: 100, maxDimension: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.invalid_downsample_max_dimension", result.Error.Code);
    }

    [Fact]
    public void Downsample_OnKnownArray_AveragesBlocksExactly()
    {
        float[] source =
        [
            1f, 2f, 3f, 4f,
            5f, 6f, 7f, 8f,
        ];

        var destination = new float[2];

        var result = ImageDownsampler.Downsample(source, width: 4, height: 2, factor: 2, destination);

        Assert.True(result.IsSuccess);

        Assert.Equal(3.5f, destination[0]);

        Assert.Equal(5.5f, destination[1]);
    }

    [Fact]
    public void Downsample_IgnoresNonFinitePixelsWithinABlock()
    {
        float[] source = [1f, float.NaN, 3f, 5f];

        var destination = new float[1];

        var result = ImageDownsampler.Downsample(source, width: 2, height: 2, factor: 2, destination);

        Assert.True(result.IsSuccess);

        Assert.Equal(3.0f, destination[0]);
    }

    [Fact]
    public void Downsample_OnBlockWithNoFinitePixels_ProducesNaN()
    {
        float[] source = [float.NaN, float.NaN];

        var destination = new float[1];

        var result = ImageDownsampler.Downsample(source, width: 2, height: 1, factor: 2, destination);

        Assert.True(result.IsSuccess);

        Assert.True(float.IsNaN(destination[0]));
    }

    [Fact]
    public void Downsample_RejectsMismatchedDestinationLength()
    {
        float[] source = [1f, 2f, 3f, 4f];

        var destination = new float[4];

        var result = ImageDownsampler.Downsample(source, width: 2, height: 2, factor: 2, destination);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.downsample_buffer_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void ComputeDownsampledDimensions_RoundsUpForNonExactDivision()
    {
        var (width, height) = ImageDownsampler.ComputeDownsampledDimensions(width: 5, height: 3, factor: 2);

        Assert.Equal(3, width);

        Assert.Equal(2, height);
    }
}
