using AstroLab.Api.Features.Images.Align;
using AstroLab.Api.Features.Images.Astrometry;
using AstroLab.Api.Features.Images.Background;
using AstroLab.Api.Features.Images.Compare;
using AstroLab.Api.Features.Images.DifferentialPhotometry;
using AstroLab.Api.Features.Images.Footprint;
using AstroLab.Api.Features.Images.Histogram;
using AstroLab.Api.Features.Images.MultiPhotometry;
using AstroLab.Api.Features.Images.Overlay;
using AstroLab.Api.Features.Images.Photometry;
using AstroLab.Api.Features.Images.Render;
using AstroLab.Api.Features.Images.Segmentation;
using AstroLab.Api.Features.Images.Separation;
using AstroLab.Api.Features.Images.SourceCharacterization;
using AstroLab.Api.Features.Images.Sources;
using AstroLab.Api.Features.Images.Stack;
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

            group.MapHistogramEndpoint();

            group.MapPhotometryEndpoint();

            group.MapSourcesEndpoint();

            group.MapAstrometryEndpoint();

            group.MapMultiPhotometryEndpoint();

            group.MapDifferentialPhotometryEndpoint();

            group.MapSourceCharacterizationEndpoint();

            group.MapBackgroundEndpoint();

            group.MapSegmentationEndpoint();

            group.MapCompareEndpoint();

            group.MapAlignEndpoint();

            group.MapStackEndpoint();

            group.MapSeparationEndpoint();

            group.MapFootprintEndpoint();

            group.MapOverlayEndpoint();
        }
    }
}
