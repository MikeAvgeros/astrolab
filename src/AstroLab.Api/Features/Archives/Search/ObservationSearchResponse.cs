using System.Collections.Immutable;
using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchResponse(ImmutableList<ArchiveObservationDto> Observations);

/// <summary>Static factory accompanying <see cref="ObservationSearchResponse"/>.</summary>
public static class ObservationSearchResponseFactory
{
    public static ObservationSearchResponse Create(ImmutableList<ArchiveObservationDto> observations) =>
        new(observations);

    public static ObservationSearchResponse Create(IReadOnlyList<ArchiveObservation> observations) =>
        Create(observations.Select(ArchiveObservationDtoFactory.Create).ToImmutableList());
}
