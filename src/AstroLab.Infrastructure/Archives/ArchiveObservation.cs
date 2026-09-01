namespace AstroLab.Infrastructure.Archives;

public readonly record struct ArchiveObservation
{
    private ArchiveObservation(string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source)
    {
        DatasetId = datasetId;
        Target = target;
        Instrument = instrument;
        ObservationDate = observationDate;
        Source = source;
    }

    public string DatasetId { get; }

    public string Target { get; }

    public string Instrument { get; }

    public DateTimeOffset ObservationDate { get; }

    public ArchiveSource Source { get; }

    public static ArchiveObservation Create(string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        return new ArchiveObservation(datasetId, target, instrument, observationDate, source);
    }
}
