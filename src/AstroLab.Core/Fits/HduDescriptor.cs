namespace AstroLab.Core.Fits;

public readonly record struct HduDescriptor
{
    private const long NoDataBytes = 0;

    private HduDescriptor(int index, HduType type, FitsHeader header, FitsImageDescriptor? image)
    {
        Index = index;
        Type = type;
        Header = header;
        Image = image;
    }

    public int Index { get; }

    public HduType Type { get; }

    public FitsHeader Header { get; }

    public FitsImageDescriptor? Image { get; }

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

        return rowLength * rowCount + heapSize;
    }

    private static long NonNegative(long value) => Math.Max(value, NoDataBytes);

    public static HduDescriptor FromHeader(int index, FitsHeader header)
    {
        var type = ClassifyHduType(index, header);

        var hasPixelData = type is HduType.Primary or HduType.Image;

        var descriptor = hasPixelData ? FitsImageDescriptor.FromHeader(header) : default;

        var image = hasPixelData && descriptor.IsSuccess ? descriptor.Value : (FitsImageDescriptor?)null;

        return HduDescriptor.Create(index, type, header, image);
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

    public static HduDescriptor Create(int index, HduType type, FitsHeader header, FitsImageDescriptor? image)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        ArgumentNullException.ThrowIfNull(header);

        return new HduDescriptor(index, type, header, image);
    }
}
