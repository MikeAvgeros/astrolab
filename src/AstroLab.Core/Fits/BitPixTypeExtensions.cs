namespace AstroLab.Core.Fits;

public static class BitPixTypeExtensions
{
    extension(BitPixType bitPix)
    {
        /// <summary>The number of bits a single pixel occupies on disk for this representation.</summary>
        public int BitsPerPixel() => Math.Abs((int)bitPix);

        /// <summary>The number of bytes a single pixel occupies on disk for this representation.</summary>
        public int BytesPerPixel() => bitPix.BitsPerPixel() / 8;

        public bool IsFloatingPoint() => (int)bitPix < 0;
    }
}
