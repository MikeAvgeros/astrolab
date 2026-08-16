using AstroLab.Core.Fits;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.CFITSIO;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Reads the raw pixel bytes for an image HDU directly from a stream (positioned immediately
/// after its header) into an <see cref="UnmanagedFitsBuffer"/>, in fixed-size chunks so the full
/// pixel array is never buffered as an intermediate managed array.
/// </summary>
public static class FitsPixelDataReader
{
    private const int ChunkSize = 81_920;

    public static async Task<Result<UnmanagedFitsBuffer>> ReadImageDataAsync(
        Stream stream, FitsImageDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (descriptor.PixelCount == 0)
        {
            return Error.Validation("fits.data.no_pixels", "HDU has no pixel data (NAXIS = 0).");
        }

        var totalBytes = checked((nuint)descriptor.DataSizeBytes);
        var buffer = UnmanagedFitsBuffer.Allocate(totalBytes);

        try
        {
            var chunk = new byte[Math.Min(ChunkSize, (int)Math.Min(totalBytes, int.MaxValue))];
            nuint offset = 0;

            while (offset < totalBytes)
            {
                var remaining = totalBytes - offset;
                var toRead = (int)Math.Min((nuint)chunk.Length, remaining);
                var bytesRead = await stream.ReadAsync(chunk.AsMemory(0, toRead), cancellationToken);
                if (bytesRead == 0)
                {
                    buffer.Dispose();
                    return Error.Validation("fits.data.truncated", "File ended before all pixel data was read.");
                }

                buffer.CopyFrom(chunk.AsSpan(0, bytesRead), offset);
                offset += (nuint)bytesRead;
            }

            return Result<UnmanagedFitsBuffer>.Success(buffer);
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }
}
