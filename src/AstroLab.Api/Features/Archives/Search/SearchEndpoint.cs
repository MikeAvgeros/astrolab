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
        ArchiveSource archive,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        CancellationToken cancellationToken,
        string? target = null,
        string? mission = null,
        string? instrument = null,
        double? searchRadiusDegrees = null,
        int maxResults = ObservationSearchRequest.DefaultMaxResults)
    {
        var request = ObservationSearchRequest.Create(archive, target, mission, instrument, searchRadiusDegrees, maxResults);

        var query = request.SearchRadiusDegrees is { } searchRadiusDegreesValue
            ? ArchiveSearchQuery.Create(request.Target, request.Mission, request.Instrument, searchRadiusDegrees: searchRadiusDegreesValue, maxResults: request.MaxResults)
            : ArchiveSearchQuery.Create(request.Target, request.Mission, request.Instrument, maxResults: request.MaxResults);

        var client = ArchiveClientResolver.Resolve(request.Archive, esoClient, mastClient);

        var result = await client.SearchAsync(query, cancellationToken);

        return result.ToApiResult(observations => Results.Ok(ObservationSearchResponse.Create(observations)));
    }
}
