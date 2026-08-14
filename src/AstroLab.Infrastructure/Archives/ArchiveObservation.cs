namespace AstroLab.Infrastructure.Archives;

/// <summary>A single observation record returned by an archive metadata search.</summary>
public readonly record struct ArchiveObservation(
    string DatasetId,
    string Target,
    string Instrument,
    DateTimeOffset ObservationDate,
    ArchiveSource Source);
