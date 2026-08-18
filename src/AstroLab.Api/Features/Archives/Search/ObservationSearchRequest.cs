using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchRequest(
    ArchiveSource Archive,
    string? Target = null,
    string? Instrument = null,
    int MaxResults = ObservationSearchRequest.DefaultMaxResults)
{
    private const int DefaultMaxResults = 50;
}
