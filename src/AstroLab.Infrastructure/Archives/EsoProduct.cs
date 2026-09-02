namespace AstroLab.Infrastructure.Archives;

public sealed record EsoProduct(
    string Id,
    string ObservationId,
    string? FileName,
    string DataUri,
    string? ProductType,
    string? DataProductType,
    int? CalibrationLevel,
    string? Format,
    long? Size,
    string? DataRights);
