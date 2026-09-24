using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ObservationSearchRequest
{
    internal const int DefaultMaxResults = 50;
    internal const int MaxResultsLimit = 10_000;

    private ObservationSearchRequest(
        ArchiveSource archive, string target, string? mission = null, string? instrument = null,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        double? searchRadiusDegrees = null, int maxResults = DefaultMaxResults)
    {
        Archive = archive;
        Target = target;
        Mission = mission;
        Instrument = instrument;
        From = from;
        To = to;
        SearchRadiusDegrees = searchRadiusDegrees;
        MaxResults = maxResults;
    }

    public ArchiveSource Archive { get; }

    public string Target { get; }

    public string? Mission { get; }

    public string? Instrument { get; }

    public DateTimeOffset? From { get; }

    public DateTimeOffset? To { get; }

    public double? SearchRadiusDegrees { get; }

    public int MaxResults { get; }

    public static ObservationSearchRequest Create(
        ArchiveSource archive, string target, string? mission = null, string? instrument = null,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        double? searchRadiusDegrees = null, int maxResults = DefaultMaxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxResults, MaxResultsLimit);

        if (searchRadiusDegrees is { } radius && (!double.IsFinite(radius) || radius <= 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(searchRadiusDegrees), radius, "searchRadiusDegrees must be a finite, positive value.");
        }

        if (!Enum.IsDefined(archive))
        {
            throw new ArgumentOutOfRangeException(nameof(archive), archive, "Unknown archive source.");
        }

        return new ObservationSearchRequest(archive, target, mission, instrument, from, to, searchRadiusDegrees, maxResults);
    }
}
