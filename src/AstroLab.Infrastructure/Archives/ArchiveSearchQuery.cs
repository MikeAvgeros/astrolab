namespace AstroLab.Infrastructure.Archives;

/// <summary>Search criteria for querying an archive's observation catalogue.</summary>
/// <param name="Target">Free-text target/object name filter.</param>
/// <param name="Instrument">Instrument name filter.</param>
/// <param name="From">Earliest observation date (inclusive).</param>
/// <param name="To">Latest observation date (inclusive).</param>
/// <param name="MaxResults">Upper bound on the number of results returned.</param>
public readonly record struct ArchiveSearchQuery(string? Target = null, string? Instrument = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int MaxResults = ArchiveSearchQueryFactory.DefaultMaxResults);

/// <summary>Static factory accompanying <see cref="ArchiveSearchQuery"/>. Validates arguments before constructing.</summary>
public static class ArchiveSearchQueryFactory
{
    public const int DefaultMaxResults = 50;

    public static ArchiveSearchQuery Create(string? target = null, string? instrument = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ArchiveSearchQuery(target, instrument, from, to, maxResults);
    }
}
