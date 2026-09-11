using AstroLab.Infrastructure.Catalogues;

namespace AstroLab.Api.Features.Catalogues.Query;

/// <summary>Cone-searches a named external catalogue (e.g. a VizieR table such as "I/355/gaiadr3") for sources near a sky position.</summary>
public static class QueryEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapQueryEndpoint()
        {
            group.MapGet("/query", QueryCatalogueAsync)
                .WithSummary("Cone-searches an external astronomical catalogue via VizieR's TAP service.");
        }
    }

    private static async Task<IResult> QueryCatalogueAsync(
        string catalogueId,
        double rightAscension,
        double declination,
        double radiusArcsec,
        ICatalogueClient catalogueClient,
        CancellationToken cancellationToken,
        int maxResults = CatalogueQueryRequest.DefaultMaxResults)
    {
        var request = CatalogueQueryRequest.Create(catalogueId, rightAscension, declination, radiusArcsec, maxResults);

        var query = CatalogueConeSearchQuery.Create(
            request.CatalogueId, request.RightAscension, request.Declination, request.RadiusArcsec, request.MaxResults);

        var result = await catalogueClient.ConeSearchAsync(query, cancellationToken);

        return result.ToApiResult(entries => Results.Ok(CatalogueQueryResponse.Create(request.CatalogueId, entries)));
    }
}
