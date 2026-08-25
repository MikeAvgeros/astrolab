namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryRequest(double RightAscension, double Declination, double RadiusArcsec);

/// <summary>Static factory accompanying <see cref="CatalogueQueryRequest"/>. Validates arguments before constructing.</summary>
public static class CatalogueQueryRequestFactory
{
    public static CatalogueQueryRequest Create(double rightAscension, double declination, double radiusArcsec)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        return new CatalogueQueryRequest(rightAscension, declination, radiusArcsec);
    }
}
