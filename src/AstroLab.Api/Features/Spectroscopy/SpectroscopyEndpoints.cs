using AstroLab.Api.Features.Spectroscopy.Calibrate;
using AstroLab.Api.Features.Spectroscopy.Compare;
using AstroLab.Api.Features.Spectroscopy.Extract;
using AstroLab.Api.Features.Spectroscopy.Lines;
using AstroLab.Api.Features.Spectroscopy.Redshift;

namespace AstroLab.Api.Features.Spectroscopy;

public static class SpectroscopyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapSpectroscopyEndpoints()
        {
            var group = app.MapGroup("/api/spectroscopy").WithTags("Spectroscopy");

            group.MapExtractEndpoint();

            group.MapCalibrateEndpoint();

            group.MapLinesEndpoint();

            group.MapRedshiftEndpoint();

            group.MapCompareEndpoint();
        }
    }
}
