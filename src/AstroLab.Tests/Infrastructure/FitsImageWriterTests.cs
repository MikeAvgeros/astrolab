using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Tests.Infrastructure;

public class FitsImageWriterTests
{
    [Fact]
    public async Task WriteFloatImage_RoundTripsThroughTheReadPipelineAsync()
    {
        float[] pixels = [1.5f, -2.25f, 3.0f, 4.75f, 5.0f, 6.125f];

        var fitsBytes = FitsImageWriter.WriteFloatImage(pixels, width: 3, height: 2);

        using var stream = new MemoryStream(fitsBytes);

        var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(stream);

        Assert.True(locationsResult.IsSuccess);

        var location = Assert.Single(locationsResult.Value);

        Assert.Equal(FitsDatasetKind.Image, FitsDatasetClassifier.Classify([location.Descriptor]));

        var descriptor = location.Descriptor.Image!.Value;

        Assert.Equal(3, descriptor.NAxes[0]);

        Assert.Equal(2, descriptor.NAxes[1]);

        stream.Seek(location.DataOffset, SeekOrigin.Begin);

        var bufferResult = await FitsPixelDataReader.ReadImageDataAsync(stream, descriptor);

        Assert.True(bufferResult.IsSuccess);

        using var buffer = bufferResult.Value;

        using var floatBuffer = FitsPixelConverter.ToFloatBuffer(buffer.AsSpan(), descriptor);

        Assert.Equal(pixels, floatBuffer.AsFloatSpan().ToArray());
    }

    [Fact]
    public void WriteFloatImage_PadsHeaderAndDataToFitsBlockBoundaries()
    {
        var fitsBytes = FitsImageWriter.WriteFloatImage([1f, 2f, 3f, 4f], width: 2, height: 2);

        Assert.Equal(0, fitsBytes.Length % 2880);
    }

    [Fact]
    public void WriteFloatImage_OnMismatchedPixelCount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FitsImageWriter.WriteFloatImage([1f, 2f, 3f], width: 2, height: 2));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void WriteFloatImage_OnNonPositiveDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FitsImageWriter.WriteFloatImage([], width, height));
    }
}
