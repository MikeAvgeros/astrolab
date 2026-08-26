namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryRequest
{
    public CatalogueQueryRequest(double rightAscension, double declination, double radiusArcsec)
    {
        RightAscension = rightAscension;
        Declination = declination;
        RadiusArcsec = radiusArcsec;
    }

    public double RightAscension { get; }

    public double Declination { get; }

    public double RadiusArcsec { get; }

    public static CatalogueQueryRequest Create(double rightAscension, double declination, double radiusArcsec)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        return new CatalogueQueryRequest(rightAscension, declination, radiusArcsec);
    }
}
