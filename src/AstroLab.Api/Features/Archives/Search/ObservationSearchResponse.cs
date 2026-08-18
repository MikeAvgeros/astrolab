using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchResponse(IReadOnlyList<ArchiveObservationDto> Observations)
{
    public static ObservationSearchResponse FromObservations(IReadOnlyList<ArchiveObservation> observations)
    {
        var dtos = new List<ArchiveObservationDto>(observations.Count);
        
        dtos.AddRange(observations.Select(ArchiveObservationDto.FromObservation));

        return new ObservationSearchResponse(dtos);
    }
}
