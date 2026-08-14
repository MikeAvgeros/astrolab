using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// A minimal, dependency-free PNG encoder (8-bit truecolor, no interlacing, "None" scanline
/// filtering) — see the PNG 1.2 specification. Hand-rolled rather than pulled in from a general
/// imaging library so that the rendering pipeline has no third-party dependency beyond the
/// deflate/zlib support already built into the .NET base class library.
/// </summary>
public static class PngRenderer
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    private const byte BitDepth = 8;
    private const byte ColorTypeTruecolor = 2;
    private const int BytesPerPixel = 3;
    private const int IhdrLength = 13;
    private const int ChunkLengthFieldSize = 4;
    private const byte CompressionMethod = 0;
    private const byte FilterMethod = 0;
    private const byte InterlaceMethod = 0;
    private const byte NoFilterScanlineTag = 0;

    /// <summary>Encodes <paramref name="image"/> as a complete PNG file.</summary>
    public static byte[] Encode(RenderedImage image)
    {
        var raw = ToFilteredScanlines(image);

        using var output = new MemoryStream();
        output.Write(Signature);
        WriteChunk(output, "IHDR", BuildIhdr(image.Width, image.Height));
        WriteChunk(output, "IDAT", Deflate(raw));
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    /// <summary>Prefixes every scanline with a filter-type byte (always 0 / "None") as PNG's raw image data requires.</summary>
    private static byte[] ToFilteredScanlines(RenderedImage image)
    {
        var stride = image.Width * BytesPerPixel;
        var raw = new byte[(stride + 1) * image.Height];

        for (var y = 0; y < image.Height; y++)
        {
            var rawRowOffset = y * (stride + 1);
            raw[rawRowOffset] = NoFilterScanlineTag;
            Buffer.BlockCopy(image.Rgb, y * stride, raw, rawRowOffset + 1, stride);
        }

        return raw;
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var ihdr = new byte[IhdrLength];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0, ChunkLengthFieldSize), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4, ChunkLengthFieldSize), (uint)height);
        ihdr[8] = BitDepth;
        ihdr[9] = ColorTypeTruecolor;
        ihdr[10] = CompressionMethod;
        ihdr[11] = FilterMethod;
        ihdr[12] = InterlaceMethod;
        return ihdr;
    }

    private static byte[] Deflate(byte[] raw)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream output, string chunkType, byte[] data)
    {
        Span<byte> lengthBytes = stackalloc byte[ChunkLengthFieldSize];
        BinaryPrimitives.WriteUInt32BigEndian(lengthBytes, (uint)data.Length);
        output.Write(lengthBytes);

        Span<byte> typeBytes = stackalloc byte[ChunkLengthFieldSize];
        Encoding.ASCII.GetBytes(chunkType, typeBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = new Crc32();
        crc.Append(typeBytes);
        crc.Append(data);

        Span<byte> crcBytes = stackalloc byte[ChunkLengthFieldSize];
        crc.GetCurrentHash(crcBytes);
        crcBytes.Reverse();
        output.Write(crcBytes);
    }
}
