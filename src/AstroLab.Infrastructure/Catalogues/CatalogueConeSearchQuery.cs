namespace AstroLab.Infrastructure.Catalogues;

/// <summary>A spatial cone-search request against a named external catalogue (e.g. a VizieR table identifier such as "I/355/gaiadr3").</summary>
public readonly record struct CatalogueConeSearchQuery
{
    private const int DefaultMaxResults = 50;

    private CatalogueConeSearchQuery(string catalogueId, double rightAscension, double declination, double radiusArcsec, int maxResults)
    {
        CatalogueId = catalogueId;
        RightAscension = rightAscension;
        Declination = declination;
        RadiusArcsec = radiusArcsec;
        MaxResults = maxResults;
    }

    public string CatalogueId { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public double RadiusArcsec { get; }

    public int MaxResults { get; }

    public static CatalogueConeSearchQuery Create(
        string catalogueId, double rightAscension, double declination, double radiusArcsec, int maxResults = DefaultMaxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        return new CatalogueConeSearchQuery(catalogueId, rightAscension, declination, radiusArcsec, maxResults);
    }
}
