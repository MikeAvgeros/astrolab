using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchRequest(ArchiveSource Archive, string? Target = null, string? Instrument = null, int MaxResults = ObservationSearchRequest.DefaultMaxResults)
{
    internal const int DefaultMaxResults = 50;
}

/// <summary>Static factory accompanying <see cref="ObservationSearchRequest"/>. Validates arguments before constructing.</summary>
public static class ObservationSearchRequestFactory
{
    public static ObservationSearchRequest Create(ArchiveSource archive, string? target = null, string? instrument = null, int maxResults = ObservationSearchRequest.DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ObservationSearchRequest(archive, target, instrument, maxResults);
    }
}
