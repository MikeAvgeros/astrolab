namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryRequest(double RightAscension, double Declination, double RadiusArcsec);
