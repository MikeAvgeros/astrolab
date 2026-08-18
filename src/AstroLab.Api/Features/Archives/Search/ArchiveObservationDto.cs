using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ArchiveObservationDto(string DatasetId, string Target, string Instrument, DateTimeOffset ObservationDate, ArchiveSource Source)
{
    public static ArchiveObservationDto FromObservation(ArchiveObservation observation) =>
        new(observation.DatasetId, observation.Target, observation.Instrument, observation.ObservationDate, observation.Source);
}
