using System.Buffers.Binary;
using System.Text;
using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Tests.Infrastructure;

public class FitsPixelConverterTests
{
    private static FitsImageDescriptor BuildDescriptor(params string[] cards)
    {
        var block = Encoding.ASCII.GetBytes(string.Concat(cards.Select(card => card.PadRight(FitsCardParser.CardLength))));

        return FitsImageDescriptor.FromHeader(FitsHeader.Parse(block).Value).Value;
    }

    private static byte[] BigEndianInt16(params short[] values)
    {
        var bytes = new byte[values.Length * sizeof(short)];

        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(i * sizeof(short)), values[i]);
        }

        return bytes;
    }

    [Fact]
    public void ToFloatBuffer_Int16WithBlank_MapsBlankPixelsToNaNAndScalesTheRest()
    {
        var descriptor = BuildDescriptor(
            "SIMPLE  =                    T",
            "BITPIX  =                   16",
            "NAXIS   =                    2",
            "NAXIS1  =                    2",
            "NAXIS2  =                    2",
            "BZERO   =                32768",
            "BLANK   =               -32768",
            "END");

        using var buffer = FitsPixelConverter.ToFloatBuffer(BigEndianInt16(-32768, 0, 100, -32768), descriptor);

        var pixels = buffer.AsFloatSpan().ToArray();

        Assert.True(float.IsNaN(pixels[0]));

        Assert.Equal(32768f, pixels[1]);

        Assert.Equal(32868f, pixels[2]);

        Assert.True(float.IsNaN(pixels[3]));
    }

    [Fact]
    public void ToFloatBuffer_Int16WithoutBlank_KeepsEveryPixel()
    {
        var descriptor = BuildDescriptor(
            "SIMPLE  =                    T",
            "BITPIX  =                   16",
            "NAXIS   =                    1",
            "NAXIS1  =                    2",
            "END");

        using var buffer = FitsPixelConverter.ToFloatBuffer(BigEndianInt16(-32768, 7), descriptor);

        Assert.Equal([-32768f, 7f], buffer.AsFloatSpan().ToArray());
    }
}
