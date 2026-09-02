namespace AstroLab.Infrastructure.Archives;

public sealed record MastProduct(
    string DataUri,
    string? Filename,
    string? ProductType,
    string? DataProductType,
    int? CalibrationLevel,
    long? Size,
    string? DataRights);
