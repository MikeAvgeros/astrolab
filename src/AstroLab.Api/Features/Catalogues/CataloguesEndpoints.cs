using AstroLab.Api.Features.Catalogues.CrossMatch;
using AstroLab.Api.Features.Catalogues.Query;

namespace AstroLab.Api.Features.Catalogues;

/// <summary>
/// External astronomical catalogue integration: cone-search querying (Query) and cross-matching
/// detected sources against a catalogue (CrossMatch). Scaffolded roadmap feature: every leaf
/// returns HTTP 501 pending its Core algorithm and catalogue client (see spec.md §4.1).
/// </summary>
public static class CataloguesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapCataloguesEndpoints()
        {
            var group = app.MapGroup("/api/catalogues").WithTags("Catalogues");

            group.MapQueryEndpoint();

            group.MapCrossMatchEndpoint();

            return group;
        }
    }
}
