using System.Collections.Immutable;
using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchResponse
{
    private ObservationSearchResponse(ImmutableList<ArchiveObservationDto> observations)
    {
        Observations = observations;
    }

    public ImmutableList<ArchiveObservationDto> Observations { get; }

    public static ObservationSearchResponse Create(ImmutableList<ArchiveObservationDto> observations) =>
        new(observations);

    public static ObservationSearchResponse Create(IReadOnlyList<ArchiveObservation> observations) =>
        Create(observations.Select(ArchiveObservationDto.Create).ToImmutableList());
}
