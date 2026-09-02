using AstroLab.Api.Features.Images.Astrometry;
using AstroLab.Api.Features.Images.Photometry;
using AstroLab.Api.Features.Images.Render;
using AstroLab.Api.Features.Images.Sources;
using AstroLab.Api.Features.Images.Statistics;

namespace AstroLab.Api.Features.Images;

public static class ImagesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapImagesEndpoints()
        {
            var group = app.MapGroup("/api/images").WithTags("Images");

            group.MapRenderEndpoint();

            group.MapStatisticsEndpoint();

            group.MapPhotometryEndpoint();

            group.MapSourcesEndpoint();

            group.MapAstrometryEndpoint();
        }
    }
}
