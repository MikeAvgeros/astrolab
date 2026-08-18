using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

/// <summary>Searches an upstream archive's (ESO or MAST) observation catalogue.</summary>
public static class SearchEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSearchEndpoint()
        {
            group.MapGet("/search", SearchAsync)
                .WithSummary("Searches an upstream archive's observation catalogue.");
        }
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] ObservationSearchRequest request,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        CancellationToken cancellationToken)
    {
        var query = new ArchiveSearchQuery(request.Target, request.Instrument, MaxResults: request.MaxResults);
        var client = ArchiveClientResolver.Resolve(request.Archive, esoClient, mastClient);

        var result = await client.SearchAsync(query, cancellationToken);
        return result.ToApiResult(observations => Results.Ok(ObservationSearchResponse.FromObservations(observations)));
    }
}
