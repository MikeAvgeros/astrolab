namespace AstroLab.Infrastructure.Archives;

public sealed record MastProduct
{
    private MastProduct(
        string dataUri, string? filename, string? productType, string? dataProductType,
        int? calibrationLevel, long? size, string? dataRights)
    {
        DataUri = dataUri;
        Filename = filename;
        ProductType = productType;
        DataProductType = dataProductType;
        CalibrationLevel = calibrationLevel;
        Size = size;
        DataRights = dataRights;
    }

    public string DataUri { get; }

    public string? Filename { get; }

    public string? ProductType { get; }

    public string? DataProductType { get; }

    public int? CalibrationLevel { get; }

    public long? Size { get; }

    public string? DataRights { get; }

    public static MastProduct Create(
        string dataUri, string? filename, string? productType, string? dataProductType,
        int? calibrationLevel, long? size, string? dataRights)
    {
        ArgumentNullException.ThrowIfNull(dataUri);

        return new MastProduct(dataUri, filename, productType, dataProductType, calibrationLevel, size, dataRights);
    }
}
