namespace AstroLab.Infrastructure.Archives;

/// <summary>Search criteria for querying an archive's observation catalogue.</summary>
public readonly record struct ArchiveSearchQuery
{
    private const int DefaultMaxResults = 50;

    private ArchiveSearchQuery(string? target, string? instrument, DateTimeOffset? from, DateTimeOffset? to, int maxResults)
    {
        Target = target;
        Instrument = instrument;
        From = from;
        To = to;
        MaxResults = maxResults;
    }

    /// <summary>Free-text target/object name filter.</summary>
    public string? Target { get; }

    /// <summary>Instrument name filter.</summary>
    public string? Instrument { get; }

    /// <summary>Earliest observation date (inclusive).</summary>
    public DateTimeOffset? From { get; }

    /// <summary>Latest observation date (inclusive).</summary>
    public DateTimeOffset? To { get; }

    /// <summary>Upper bound on the number of results returned.</summary>
    public int MaxResults { get; }

    public static ArchiveSearchQuery Create(string? target = null, string? instrument = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ArchiveSearchQuery(target, instrument, from, to, maxResults);
    }
}
