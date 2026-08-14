namespace AstroLab.Core.Fits;

public static class BitPixTypeExtensions
{
    private const int BitsPerByte = 8;

    extension(BitPixType bitPix)
    {
        /// <summary>The number of bits a single pixel occupies on disk for this representation.</summary>
        public int BitsPerPixel() => Math.Abs((int)bitPix);

        /// <summary>The number of bytes a single pixel occupies on disk for this representation.</summary>
        public int BytesPerPixel() => bitPix.BitsPerPixel() / BitsPerByte;

        public bool IsFloatingPoint() => (int)bitPix < 0;
    }
}
