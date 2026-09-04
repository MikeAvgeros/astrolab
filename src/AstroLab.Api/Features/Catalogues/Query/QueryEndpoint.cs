namespace AstroLab.Api.Features.Catalogues.Query;

/// <summary>
/// Roadmap slice: querying an external astronomical catalogue by cone search. Request/response
/// contract is final; the catalogue client itself is not yet implemented (see spec.md §6.5), so
/// this route always returns HTTP 501.
/// </summary>
public static class QueryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapQueryEndpoint()
        {
            group.MapGet("/query", QueryCatalogue)
                .WithSummary("Cone-searches an external astronomical catalogue. Not yet implemented.");
        }
    }

    private static IResult QueryCatalogue(double rightAscension, double declination, double radiusArcsec)
    {
        _ = CatalogueQueryRequest.Create(rightAscension, declination, radiusArcsec);

        return NotImplementedResult.Value("catalogues.query.not_implemented", "Catalogue querying is not yet implemented.");
    }
}
