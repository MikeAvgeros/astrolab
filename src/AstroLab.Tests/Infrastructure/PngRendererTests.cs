using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using AstroLab.Infrastructure.ImageRendering;

namespace AstroLab.Tests.Infrastructure;

public class PngRendererTests
{
    /// <summary>
    /// A minimal, independent PNG parser used only to verify <see cref="PngRenderer"/>'s output:
    /// walks the chunk structure, reads IHDR, concatenates and inflates IDAT, and strips the
    /// "None" scanline filter byte PngRenderer always writes. Deliberately does not reuse any
    /// PngRenderer code, so it exercises the encoder's actual byte-level output.
    /// </summary>
    private static DecodedPng Decode(byte[] png)
    {
        ReadOnlySpan<byte> expectedSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(png.AsSpan(0, 8).SequenceEqual(expectedSignature));

        var offset = 8;
        int width = 0, height = 0;
        using var idat = new MemoryStream();

        while (offset < png.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.AsSpan(offset + 8, length);

            if (type == "IHDR")
            {
                width = (int)BinaryPrimitives.ReadUInt32BigEndian(data[..4]);
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(data[4..8]);
                Assert.Equal(8, data[8]);
                Assert.Equal(2, data[9]);
            }
            else if (type == "IDAT")
            {
                idat.Write(data);
            }

            offset += 8 + length + 4;
        }

        idat.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
        {
            zlib.CopyTo(inflated);
        }

        var stride = (width * 3) + 1;
        var rawScanlines = inflated.ToArray();
        var rgb = new byte[width * height * 3];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * stride;
            Assert.Equal(0, rawScanlines[rowStart]);
            Buffer.BlockCopy(rawScanlines, rowStart + 1, rgb, y * width * 3, width * 3);
        }

        return new DecodedPng(width, height, rgb);
    }

    [Fact]
    public void Encode_RoundTripsPixelDataThroughPngStructure()
    {
        byte[] rgb = [255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255];
        var image = RenderedImageFactory.Create(2, 2, rgb);

        var png = PngRenderer.Encode(image);
        var decoded = Decode(png);

        Assert.Equal(2, decoded.Width);
        Assert.Equal(2, decoded.Height);
        Assert.Equal(rgb, decoded.Rgb);
    }

    [Fact]
    public void Encode_ProducesValidPngSignature()
    {
        var image = RenderedImageFactory.Create(1, 1, [10, 20, 30]);

        var png = PngRenderer.Encode(image);

        Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], png[..8]);
    }
}
