namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryRequest
{
    internal const int DefaultMaxResults = 50;
    internal const int MaxResultsLimit = 10_000;
    private const double MinRightAscension = 0.0;
    private const double MaxRightAscension = 360.0;
    private const double MinDeclination = -90.0;
    private const double MaxDeclination = 90.0;

    private CatalogueQueryRequest(string catalogueId, double rightAscension, double declination, double radiusArcsec, int maxResults)
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

    public static CatalogueQueryRequest Create(
        string catalogueId, double rightAscension, double declination, double radiusArcsec, int maxResults = DefaultMaxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueId);

        if (!double.IsFinite(rightAscension) || rightAscension is < MinRightAscension or >= MaxRightAscension)
        {
            throw new ArgumentOutOfRangeException(nameof(rightAscension), rightAscension, "rightAscension must be in [0, 360) degrees.");
        }

        if (!double.IsFinite(declination) || declination is < MinDeclination or > MaxDeclination)
        {
            throw new ArgumentOutOfRangeException(nameof(declination), declination, "declination must be in [-90, 90] degrees.");
        }

        if (!double.IsFinite(radiusArcsec))
        {
            throw new ArgumentOutOfRangeException(nameof(radiusArcsec), radiusArcsec, "radiusArcsec must be finite.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxResults, MaxResultsLimit);

        return new CatalogueQueryRequest(catalogueId, rightAscension, declination, radiusArcsec, maxResults);
    }
}
