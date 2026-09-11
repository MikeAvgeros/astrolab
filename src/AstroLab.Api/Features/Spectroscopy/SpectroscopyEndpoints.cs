using AstroLab.Api.Features.Spectroscopy.Calibrate;
using AstroLab.Api.Features.Spectroscopy.Compare;
using AstroLab.Api.Features.Spectroscopy.Continuum;
using AstroLab.Api.Features.Spectroscopy.ContinuumSubtract;
using AstroLab.Api.Features.Spectroscopy.EquivalentWidth;
using AstroLab.Api.Features.Spectroscopy.Extract;
using AstroLab.Api.Features.Spectroscopy.LineFit;
using AstroLab.Api.Features.Spectroscopy.Lines;
using AstroLab.Api.Features.Spectroscopy.Redshift;
using AstroLab.Api.Features.Spectroscopy.Snr;

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

            group.MapContinuumEndpoint();

            group.MapContinuumSubtractEndpoint();

            group.MapLineFitEndpoint();

            group.MapEquivalentWidthEndpoint();

            group.MapSnrEndpoint();
        }
    }
}
