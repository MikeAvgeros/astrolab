using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ArchiveObservationDto
{
    private ArchiveObservationDto(string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source)
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

    public static ArchiveObservationDto Create(string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        return new ArchiveObservationDto(datasetId, target, instrument, observationDate, source);
    }

    public static ArchiveObservationDto Create(ArchiveObservation observation) =>
        Create(observation.DatasetId, observation.Target, observation.Instrument, observation.ObservationDate, observation.Source);
}
