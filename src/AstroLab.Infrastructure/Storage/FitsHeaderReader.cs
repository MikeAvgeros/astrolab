using System.Collections.Immutable;
using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Reads FITS header(s) directly from a stream, one 2880-byte block at a time, handing each
/// growing buffer to <see cref="FitsHeader.Parse"/> until it reports a complete header (an
/// <c>END</c> card was found). All parsing logic stays in <c>AstroLab.Core</c>; this class is
/// pure I/O sequencing.
/// </summary>
public static class FitsHeaderReader
{
    private const int BlockSize = 2880;
    private const int MaxBlocks = 200;

    /// <summary>Reads the single header beginning at the stream's current position.</summary>
    public static async Task<Result<FitsHeader>> ReadHeaderAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var accumulated = new MemoryStream();

        var block = new byte[BlockSize];

        for (var blockIndex = 0; blockIndex < MaxBlocks; blockIndex++)
        {
            var bytesRead = await ReadExactAsync(stream, block, cancellationToken);

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

    /// <summary>
    /// Walks every HDU in <paramref name="stream"/> from its current position to end-of-stream,
    /// parsing each header and seeking over its data segment (sized via
    /// <see cref="HduDescriptor.DataSizeBytes"/>, rounded up to the next 2880-byte block) to reach
    /// the next one. Requires a seekable stream — true for every <c>FileStream</c> returned by
    /// <see cref="ILocalFileStore.OpenRead"/>.
    /// </summary>
    public static async Task<Result<ImmutableArray<HduLocation>>> ReadAllHeadersAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var locations = ImmutableArray.CreateBuilder<HduLocation>();

        for (var index = 0; stream.Position < stream.Length; index++)
        {
            var headerResult = await ReadHeaderAsync(stream, cancellationToken);

            if (headerResult.IsFailure)
            {
                return Result<ImmutableArray<HduLocation>>.Failure(headerResult.Error);
            }

            var descriptor = HduDescriptor.FromHeader(index, headerResult.Value);

            var dataOffset = stream.Position;

            locations.Add(HduLocationFactory.Create(descriptor, dataOffset));

            var skipBytes = RoundUpToBlockSize(descriptor.DataSizeBytes);

            if (skipBytes > 0)
            {
                stream.Seek(skipBytes, SeekOrigin.Current);
            }
        }

        if (locations.Count == 0)
        {
            return Error.Validation("fits.header.empty_file", "The staged file contains no FITS headers.");
        }

        return Result<ImmutableArray<HduLocation>>.Success(locations.ToImmutable());
    }

    private static long RoundUpToBlockSize(long byteCount)
    {
        var nonNegativeByteCount = Math.Max(byteCount, 0);

        return (nonNegativeByteCount + BlockSize - 1) / BlockSize * BlockSize;
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
