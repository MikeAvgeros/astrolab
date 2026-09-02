namespace AstroLab.Infrastructure.Archives;

internal sealed record MastObservation(
    string ObsId,
    string? TargetName,
    string? Collection,
    string? Instrument,
    string? DataProductType,
    int? CalibrationLevel,
    double? ObservationStart,
    double? ObservationEnd,
    double? ExposureTime,
    double? RightAscension,
    double? Declination,
    double? WavelengthMin,
    double? WavelengthMax,
    string? ProposalId,
    string? ProposalPi,
    string? DataRights);
