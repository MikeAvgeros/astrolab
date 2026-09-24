namespace AstroLab.Infrastructure.Archives;

public readonly record struct ArchiveSearchQuery
{
    private const int DefaultMaxResults = 50;

    private ArchiveSearchQuery(string? target, string? mission, string? instrument, DateTimeOffset? from, DateTimeOffset? to, double? searchRadiusDegrees, int maxResults)
    {
        Target = target;
        Mission = mission;
        Instrument = instrument;
        From = from;
        To = to;
        SearchRadiusDegrees = searchRadiusDegrees;
        MaxResults = maxResults;
    }

    public string? Target { get; }

    public string? Mission { get; }

    public string? Instrument { get; }

    public DateTimeOffset? From { get; }

    public DateTimeOffset? To { get; }

    public double? SearchRadiusDegrees { get; }

    public int MaxResults { get; }

    public static ArchiveSearchQuery Create(string? target = null, string? mission = null, string? instrument = null, DateTimeOffset? from = null, DateTimeOffset? to = null, double? searchRadiusDegrees = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        if (searchRadiusDegrees is { } radius)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        }

        return new ArchiveSearchQuery(target, mission, instrument, from, to, searchRadiusDegrees, maxResults);
    }
}
