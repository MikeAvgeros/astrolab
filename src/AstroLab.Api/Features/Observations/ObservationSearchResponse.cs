using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Observations;

public sealed record ObservationSearchResponse(IReadOnlyList<ArchiveObservationDto> Observations)
{
    public static ObservationSearchResponse FromObservations(IReadOnlyList<ArchiveObservation> observations)
    {
        var dtos = new List<ArchiveObservationDto>(observations.Count);

        foreach (var observation in observations)
        {
            dtos.Add(ArchiveObservationDto.FromObservation(observation));
        }

        return new ObservationSearchResponse(dtos);
    }
}
