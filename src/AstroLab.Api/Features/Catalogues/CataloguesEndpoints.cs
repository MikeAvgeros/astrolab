using AstroLab.Api.Features.Catalogues.CrossMatch;
using AstroLab.Api.Features.Catalogues.Query;

namespace AstroLab.Api.Features.Catalogues;

public static class CataloguesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCataloguesEndpoints()
        {
            var group = app.MapGroup("/api/catalogues").WithTags("Catalogues");

            group.MapQueryEndpoint();

            group.MapCrossMatchEndpoint();
        }
    }
}
