namespace AstroLab.Api.Features.Catalogues.CrossMatch;

/// <summary>
/// Roadmap slice: cross-matching detected sources from a staged image against an external
/// catalogue. Request/response contract is final; the cross-match algorithm itself is not yet
/// implemented (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class CrossMatchEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCrossMatchEndpoint()
        {
            group.MapPost("/cross-match", CrossMatchSources)
                .WithSummary("Cross-matches detected sources against an external catalogue. Not yet implemented.");
        }
    }

    private static IResult CrossMatchSources(CrossMatchRequest request) =>
        NotImplementedResult.Value("catalogues.crossmatch.not_implemented", "Catalogue cross-matching is not yet implemented.");
}
