namespace AstroLab.Core.Catalogues;

/// <summary>A detected image source paired with the nearest external-catalogue candidate found within the search radius.</summary>
public readonly record struct CatalogueMatch
{
    private CatalogueMatch(int sourceId, CatalogueMatchCandidate candidate, double separationArcsec)
    {
        SourceId = sourceId;
        Candidate = candidate;
        SeparationArcsec = separationArcsec;
    }

    public int SourceId { get; }

    public CatalogueMatchCandidate Candidate { get; }

    public double SeparationArcsec { get; }

    public static CatalogueMatch Create(int sourceId, CatalogueMatchCandidate candidate, double separationArcsec)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);

        ArgumentOutOfRangeException.ThrowIfNegative(separationArcsec);

        return new CatalogueMatch(sourceId, candidate, separationArcsec);
    }
}
