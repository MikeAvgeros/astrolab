namespace AstroLab.Core.Fits;

/// <summary>Bit/byte-width and floating-point-ness derived from a <see cref="BitPixType"/> value.</summary>
public static class BitPixTypeExtensions
{
    private const int BitsPerByte = 8;

    extension(BitPixType bitPix)
    {
        public int BitsPerPixel() => Math.Abs((int)bitPix);

        public int BytesPerPixel() => bitPix.BitsPerPixel() / BitsPerByte;

        public bool IsFloatingPoint() => (int)bitPix < 0;
    }
}
