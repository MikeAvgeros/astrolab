using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ArchiveObservationDto(string DatasetId, string Target, string Instrument, DateTimeOffset ObservationDate, ArchiveSource Source);

/// <summary>Static factory accompanying <see cref="ArchiveObservationDto"/>. Validates arguments before constructing.</summary>
public static class ArchiveObservationDtoFactory
{
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
