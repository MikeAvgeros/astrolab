using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Tests.Infrastructure;

public class FitsPixelDataReaderTests
{
    [Fact]
    public async Task ReadImageDataAsync_SeekableStreamShorterThanDeclaredSize_ReturnsTruncatedFailureWithoutAllocating()
    {
        var fitsBytes = FitsImageWriter.WriteFloatImage([1f, 2f, 3f, 4f], width: 2, height: 2);

        var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(new MemoryStream(fitsBytes));

        var location = Assert.Single(locationsResult.Value);

        var descriptor = location.Descriptor.Image!.Value;

        // Truncate the file well short of the pixel data the header declares (4 floats = 16 bytes).
        using var truncatedStream = new MemoryStream(fitsBytes, 0, (int)location.DataOffset + 4);

        truncatedStream.Seek(location.DataOffset, SeekOrigin.Begin);

        var result = await FitsPixelDataReader.ReadImageDataAsync(truncatedStream, descriptor);

        Assert.True(result.IsFailure);

        Assert.Equal("fits.data.truncated", result.Error.Code);
    }

    [Fact]
    public async Task ReadImageDataAsync_NonSeekableStreamShorterThanDeclaredSize_ReturnsTruncatedFailure()
    {
        var fitsBytes = FitsImageWriter.WriteFloatImage([1f, 2f, 3f, 4f], width: 2, height: 2);

        var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(new MemoryStream(fitsBytes));

        var location = Assert.Single(locationsResult.Value);

        var descriptor = location.Descriptor.Image!.Value;

        var truncatedData = fitsBytes[(int)location.DataOffset..((int)location.DataOffset + 4)];

        await using var nonSeekableStream = new NonSeekableStream(new MemoryStream(truncatedData));

        var result = await FitsPixelDataReader.ReadImageDataAsync(nonSeekableStream, descriptor);

        Assert.True(result.IsFailure);

        Assert.Equal("fits.data.truncated", result.Error.Code);
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
