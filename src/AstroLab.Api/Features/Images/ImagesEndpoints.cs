using AstroLab.Api.Features.Images.Astrometry;
using AstroLab.Api.Features.Images.Photometry;
using AstroLab.Api.Features.Images.Render;
using AstroLab.Api.Features.Images.Sources;
using AstroLab.Api.Features.Images.Statistics;

namespace AstroLab.Api.Features.Images;

/// <summary>
/// "What can I learn from this image?" — visualization and scientific analysis of 2D FITS image
/// data: PNG rendering (Render), pixel statistics (Statistics), and aperture photometry
/// (Photometry). Source detection (Sources) and astrometry (Astrometry) are scaffolded roadmap
/// slices that return HTTP 501 pending their Core algorithms (see spec.md).
/// </summary>
public static class ImagesEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapImagesEndpoints()
        {
            var group = app.MapGroup("/api/images").WithTags("Images");

            group.MapRenderEndpoint();

            group.MapStatisticsEndpoint();

            group.MapPhotometryEndpoint();

            group.MapSourcesEndpoint();

            group.MapAstrometryEndpoint();

            return group;
        }
    }
}
