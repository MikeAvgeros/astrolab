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
    private const long NoDataBytes = 0;

    /// <summary>
    /// The size, in bytes, of this HDU's data segment as stored on disk — the primary/image
    /// pixel array size for <see cref="HduType.Primary"/>/<see cref="HduType.Image"/>, or
    /// <c>(NAXIS1 * NAXIS2) + PCOUNT</c> (row length in bytes × row count, plus the variable-length-
    /// array heap size) for a table HDU. Every component is clamped to non-negative before combining,
    /// so a malformed/adversarial header can't produce a negative or wildly wrong total. Lets a
    /// caller walking a FITS file HDU-by-HDU know how many bytes to skip to reach the next header,
    /// without needing to understand table column layouts.
    /// </summary>
    public long DataSizeBytes => Type switch
    {
        HduType.Primary or HduType.Image => Image?.DataSizeBytes ?? NoDataBytes,
        HduType.AsciiTable or HduType.BinaryTable => TableDataSizeBytes(),
        _ => NoDataBytes,
    };

    private long TableDataSizeBytes()
    {
        var rowLength = NonNegative(Header.GetInteger("NAXIS1").GetValueOrDefault(NoDataBytes));
        var rowCount = NonNegative(Header.GetInteger("NAXIS2").GetValueOrDefault(NoDataBytes));
        var heapSize = NonNegative(Header.GetInteger("PCOUNT").GetValueOrDefault(NoDataBytes));
        return (rowLength * rowCount) + heapSize;
    }

    private static long NonNegative(long value) => Math.Max(value, NoDataBytes);

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
