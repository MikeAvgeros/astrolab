using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchRequest
{
    private const int DefaultMaxResults = 50;

    private ObservationSearchRequest(
        ArchiveSource archive, string? target = null, string? mission = null, string? instrument = null,
        double? searchRadiusDegrees = null, int maxResults = DefaultMaxResults)
    {
        Archive = archive;
        Target = target;
        Mission = mission;
        Instrument = instrument;
        SearchRadiusDegrees = searchRadiusDegrees;
        MaxResults = maxResults;
    }

    public ArchiveSource Archive { get; }

    public string? Target { get; }

    public string? Mission { get; }

    public string? Instrument { get; }

    public double? SearchRadiusDegrees { get; }

    public int MaxResults { get; }

    public static ObservationSearchRequest Create(
        ArchiveSource archive, string? target = null, string? mission = null, string? instrument = null,
        double? searchRadiusDegrees = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ObservationSearchRequest(archive, target, mission, instrument, searchRadiusDegrees, maxResults);
    }
}
