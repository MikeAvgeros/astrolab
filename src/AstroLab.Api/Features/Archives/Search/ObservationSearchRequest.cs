using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchRequest
{
    private const int DefaultMaxResults = 50;

    public ObservationSearchRequest(ArchiveSource archive, string? target = null, string? instrument = null, int maxResults = DefaultMaxResults)
    {
        Archive = archive;
        Target = target;
        Instrument = instrument;
        MaxResults = maxResults;
    }

    public ArchiveSource Archive { get; }

    public string? Target { get; }

    public string? Instrument { get; }

    public int MaxResults { get; }

    public static ObservationSearchRequest Create(ArchiveSource archive, string? target = null, string? instrument = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ObservationSearchRequest(archive, target, instrument, maxResults);
    }
}
