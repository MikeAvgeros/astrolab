using AstroLab.Core.Result;

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

    public static Result<HduDescriptor> FromHeader(int index, FitsHeader header)
    {
        var type = ClassifyHduType(index, header);

        var hasPixelData = type is HduType.Primary or HduType.Image;

        if (!hasPixelData)
        {
            return Create(index, type, header, null);
        }

        return FitsImageDescriptor.FromHeader(header).Map(image => Create(index, type, header, image));
    }

    private static HduDescriptor Create(int index, HduType type, FitsHeader header, FitsImageDescriptor? image)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        ArgumentNullException.ThrowIfNull(header);

        return new HduDescriptor(index, type, header, image);
    }

    private long TableDataSizeBytes()
    {
        var rowLength = BoundedNonNegative(Header.GetInteger("NAXIS1").GetValueOrDefault(NoDataBytes));

        var rowCount = BoundedNonNegative(Header.GetInteger("NAXIS2").GetValueOrDefault(NoDataBytes));

        var heapSize = BoundedNonNegative(Header.GetInteger("PCOUNT").GetValueOrDefault(NoDataBytes));

        return rowLength * rowCount + heapSize;
    }

    private static long BoundedNonNegative(long value) => Math.Clamp(value, NoDataBytes, int.MaxValue);

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
