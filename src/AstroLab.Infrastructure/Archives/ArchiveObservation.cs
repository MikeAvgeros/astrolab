namespace AstroLab.Infrastructure.Archives;

/// <summary>A single observation record returned by an archive metadata search.</summary>
public readonly record struct ArchiveObservation(string DatasetId, string Target, string Instrument, DateTimeOffset ObservationDate, ArchiveSource Source);

/// <summary>Static factory accompanying <see cref="ArchiveObservation"/>. Validates arguments before constructing.</summary>
public static class ArchiveObservationFactory
{
    public static ArchiveObservation Create(string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        return new ArchiveObservation(datasetId, target, instrument, observationDate, source);
    }
}
