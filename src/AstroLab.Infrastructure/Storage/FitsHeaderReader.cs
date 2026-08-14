using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Reads a FITS header directly from a stream, one 2880-byte block at a time, handing each
/// growing buffer to <see cref="FitsHeader.Parse"/> until it reports a complete header (an
/// <c>END</c> card was found). All parsing logic stays in <c>AstroLab.Core</c>; this class is
/// pure I/O sequencing.
/// </summary>
public static class FitsHeaderReader
{
    private const int BlockSize = 2880;
    private const int MaxBlocks = 200;

    public static async Task<Result<FitsHeader>> ReadPrimaryHeaderAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var accumulated = new MemoryStream();
        var block = new byte[BlockSize];

        for (var blockIndex = 0; blockIndex < MaxBlocks; blockIndex++)
        {
            var bytesRead = await ReadExactAsync(stream, block, cancellationToken).ConfigureAwait(false);
            if (bytesRead < BlockSize)
            {
                return Error.Validation(
                    "fits.header.truncated_file", "File ended before a complete FITS header block was read.");
            }

            accumulated.Write(block);

            var parseResult = FitsHeader.Parse(accumulated.GetBuffer().AsSpan(0, (int)accumulated.Length));
            if (parseResult.IsSuccess || parseResult.Error.Code != "fits.header.missing_end")
            {
                return parseResult;
            }
        }

        return Error.Validation(
            "fits.header.too_large", $"Header exceeded {MaxBlocks * BlockSize:N0} bytes without an END card.");
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
