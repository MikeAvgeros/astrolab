using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Encodes a 2D float32 pixel array as a minimal, single-HDU FITS file (<c>SIMPLE</c>/
/// <c>BITPIX</c>/<c>NAXIS</c>/<c>NAXIS1</c>/<c>NAXIS2</c> only) so a derived image — such as a
/// stacked composite — can be staged and re-read through the same <see cref="FitsDatasetReader"/>
/// pipeline as an uploaded file. Per spec.md §5.1, FITS read/write round-trip preservation is not
/// a repository-wide requirement: this writer deliberately preserves only pixel dimensions and
/// values. No WCS, provenance, or other source metadata is carried over from the input frames.
/// </summary>
public static class FitsImageWriter
{
    private const int BlockSize = 2880;
    private const int CardLength = 80;
    private const int KeywordFieldWidth = 8;
    private const int BitPixFloat32 = -32;
    private const int PixelChunkCount = 8192;

    public static byte[] WriteFloatImage(ReadOnlySpan<float> pixels, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (pixels.Length != width * height)
        {
            throw new ArgumentException(
                $"Pixel span length ({pixels.Length}) does not match width x height ({width}x{height}).", nameof(pixels));
        }

        using var output = new MemoryStream();

        WriteHeader(output, width, height);

        WritePixelData(output, pixels);

        return output.ToArray();
    }

    private static void WriteHeader(Stream output, int width, int height)
    {
        var header = new StringBuilder();

        header.Append(BuildCard("SIMPLE", "T"));

        header.Append(BuildCard("BITPIX", BitPixFloat32.ToString(CultureInfo.InvariantCulture)));

        header.Append(BuildCard("NAXIS", "2"));

        header.Append(BuildCard("NAXIS1", width.ToString(CultureInfo.InvariantCulture)));

        header.Append(BuildCard("NAXIS2", height.ToString(CultureInfo.InvariantCulture)));

        header.Append("END".PadRight(CardLength));

        while (header.Length % BlockSize != 0)
        {
            header.Append(' ', CardLength);
        }

        output.Write(Encoding.ASCII.GetBytes(header.ToString()));
    }

    private static string BuildCard(string keyword, string valueToken) =>
        (keyword.PadRight(KeywordFieldWidth) + "= " + valueToken).PadRight(CardLength);

    private static void WritePixelData(Stream output, ReadOnlySpan<float> pixels)
    {
        var chunkBuffer = new byte[Math.Min(PixelChunkCount, pixels.Length) * sizeof(float)];

        var offset = 0;

        while (offset < pixels.Length)
        {
            var chunkLength = Math.Min(PixelChunkCount, pixels.Length - offset);

            for (var i = 0; i < chunkLength; i++)
            {
                BinaryPrimitives.WriteSingleBigEndian(chunkBuffer.AsSpan(i * sizeof(float), sizeof(float)), pixels[offset + i]);
            }

            output.Write(chunkBuffer, 0, chunkLength * sizeof(float));

            offset += chunkLength;
        }

        var dataSizeBytes = (long)pixels.Length * sizeof(float);

        var padding = (int)((BlockSize - dataSizeBytes % BlockSize) % BlockSize);

        if (padding > 0)
        {
            output.Write(new byte[padding]);
        }
    }
}
