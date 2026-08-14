namespace AstroLab.Infrastructure.Archives;

/// <summary>Search criteria for querying an archive's observation catalogue.</summary>
/// <param name="Target">Free-text target/object name filter.</param>
/// <param name="Instrument">Instrument name filter.</param>
/// <param name="From">Earliest observation date (inclusive).</param>
/// <param name="To">Latest observation date (inclusive).</param>
/// <param name="MaxResults">Upper bound on the number of results returned.</param>
public readonly record struct ArchiveSearchQuery(
    string? Target = null,
    string? Instrument = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int MaxResults = 50);
