using AstroLab.Api.Features.Archives.Download;
using AstroLab.Api.Features.Archives.Search;

namespace AstroLab.Api.Features.Archives;

public static class ArchivesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapArchivesEndpoints()
        {
            var group = app.MapGroup("/api/archives").WithTags("Archives");

            group.MapSearchEndpoint();

            group.MapDownloadEndpoint();
        }
    }
}
