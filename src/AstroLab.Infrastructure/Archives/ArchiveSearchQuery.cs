namespace AstroLab.Infrastructure.Archives;

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
    
    public string? Target { get; }
    
    public string? Instrument { get; }
    
    public DateTimeOffset? From { get; }
    
    public DateTimeOffset? To { get; }
    
    public int MaxResults { get; }

    public static ArchiveSearchQuery Create(string? target = null, string? instrument = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int maxResults = DefaultMaxResults)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new ArchiveSearchQuery(target, instrument, from, to, maxResults);
    }
}
