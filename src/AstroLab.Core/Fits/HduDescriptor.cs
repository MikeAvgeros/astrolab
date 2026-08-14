namespace AstroLab.Core.Fits;

/// <summary>
/// Describes a single Header/Data Unit within a FITS file: its position, kind, parsed header,
/// and — for image HDUs — pixel-array shape. Does not carry pixel data itself; large pixel
/// buffers are owned by <c>AstroLab.Infrastructure</c>'s <c>UnmanagedFitsBuffer</c>.
/// </summary>
/// <param name="Index">Zero-based position of this HDU within the file (0 = primary HDU).</param>
/// <param name="Type">The kind of HDU, derived from <c>SIMPLE</c> / <c>XTENSION</c>.</param>
/// <param name="Header">The fully parsed header for this HDU.</param>
/// <param name="Image">Pixel-array shape and scaling metadata, present only for image HDUs with data.</param>
public readonly record struct HduDescriptor(int Index, HduType Type, FitsHeader Header, FitsImageDescriptor? Image)
{
    public static HduDescriptor FromHeader(int index, FitsHeader header)
    {
        var type = ClassifyHduType(index, header);
        var hasPixelData = type is HduType.Primary or HduType.Image;
        var descriptor = hasPixelData ? FitsImageDescriptor.FromHeader(header) : default;
        var image = hasPixelData && descriptor.IsSuccess ? descriptor.Value : (FitsImageDescriptor?)null;
        return new HduDescriptor(index, type, header, image);
    }

    private static HduType ClassifyHduType(int index, FitsHeader header)
    {
        if (index == 0)
        {
            return HduType.Primary;
        }

        var xtension = header.GetString("XTENSION");
        if (xtension.IsFailure)
        {
            return HduType.Unknown;
        }

        return xtension.Value.Trim().ToUpperInvariant() switch
        {
            "IMAGE" => HduType.Image,
            "TABLE" => HduType.AsciiTable,
            "BINTABLE" => HduType.BinaryTable,
            _ => HduType.Unknown,
        };
    }
}
