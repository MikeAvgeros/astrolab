using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageCutoutExtractorTests
{
    private static readonly float[] Source =
    [
        1, 2, 3, 4,
        5, 6, 7, 8,
        9, 10, 11, 12,
    ];

    [Fact]
    public void ExtractRegion_ExactFullImage_CopiesEveryPixel()
    {
        Span<float> destination = stackalloc float[12];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 0, 0, 4, 3, destination);

        Assert.True(result.IsSuccess);

        Assert.Equal(Source, destination.ToArray());
    }

    [Fact]
    public void ExtractRegion_InteriorSubRegion_CopiesExpectedRows()
    {
        Span<float> destination = stackalloc float[4];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 1, 1, 2, 2, destination);

        Assert.True(result.IsSuccess);

        Assert.Equal([6f, 7f, 10f, 11f], destination.ToArray());
    }

    [Fact]
    public void ExtractRegion_SinglePixel_CopiesExactValue()
    {
        Span<float> destination = stackalloc float[1];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 3, 2, 1, 1, destination);

        Assert.True(result.IsSuccess);

        Assert.Equal(12f, destination[0]);
    }

    [Fact]
    public void ExtractRegion_ExtendsPastRightEdge_ReturnsOutOfBounds()
    {
        Span<float> destination = stackalloc float[4];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 3, 0, 2, 2, destination);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.cutout.out_of_bounds", result.Error.Code);
    }

    [Fact]
    public void ExtractRegion_NegativeOrigin_ReturnsOutOfBounds()
    {
        Span<float> destination = stackalloc float[4];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, -1, 0, 2, 2, destination);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.cutout.out_of_bounds", result.Error.Code);
    }

    [Fact]
    public void ExtractRegion_DestinationLengthMismatch_ReturnsValidationError()
    {
        Span<float> destination = stackalloc float[3];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 0, 0, 2, 2, destination);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.cutout.destination_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void ExtractRegion_NonPositiveRegionSize_ReturnsValidationError()
    {
        Span<float> destination = stackalloc float[1];

        var result = ImageCutoutExtractor.ExtractRegion(Source, 4, 3, 0, 0, 0, 1, destination);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.cutout.invalid_region_size", result.Error.Code);
    }
}
