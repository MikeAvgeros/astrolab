using AstroLab.Api.Features.Archives.Download;
using AstroLab.Api.Features.Archives.Search;

namespace AstroLab.Api.Features.Archives;

/// <summary>Archive metadata search (Search) and dataset download (Download) endpoints for ESO and MAST.</summary>
public static class ArchivesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapArchivesEndpoints()
        {
            var group = app.MapGroup("/api/archives").WithTags("Archives");

            group.MapSearchEndpoint();
            group.MapDownloadEndpoint();

            return group;
        }
    }
}
