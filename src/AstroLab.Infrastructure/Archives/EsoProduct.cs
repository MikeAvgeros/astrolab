namespace AstroLab.Infrastructure.Archives;

public sealed record EsoProduct
{
    private EsoProduct(
        string id, string observationId, string? fileName, string dataUri, string? productType,
        string? dataProductType, int? calibrationLevel, string? format, long? size, string? dataRights)
    {
        Id = id;
        ObservationId = observationId;
        FileName = fileName;
        DataUri = dataUri;
        ProductType = productType;
        DataProductType = dataProductType;
        CalibrationLevel = calibrationLevel;
        Format = format;
        Size = size;
        DataRights = dataRights;
    }

    public string Id { get; }

    public string ObservationId { get; }

    public string? FileName { get; }

    public string DataUri { get; }

    public string? ProductType { get; }

    public string? DataProductType { get; }

    public int? CalibrationLevel { get; }

    public string? Format { get; }

    public long? Size { get; }

    public string? DataRights { get; }

    public static EsoProduct Create(
        string id, string observationId, string? fileName, string dataUri, string? productType,
        string? dataProductType, int? calibrationLevel, string? format, long? size, string? dataRights)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        ArgumentException.ThrowIfNullOrWhiteSpace(observationId);

        ArgumentNullException.ThrowIfNull(dataUri);

        return new EsoProduct(id, observationId, fileName, dataUri, productType, dataProductType, calibrationLevel, format, size, dataRights);
    }
}
