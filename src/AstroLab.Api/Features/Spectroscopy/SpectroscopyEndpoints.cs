using AstroLab.Api.Features.Spectroscopy.Calibrate;
using AstroLab.Api.Features.Spectroscopy.Extract;
using AstroLab.Api.Features.Spectroscopy.Lines;
using AstroLab.Api.Features.Spectroscopy.Redshift;

namespace AstroLab.Api.Features.Spectroscopy;

/// <summary>
/// "What can I learn from this spectrum?" — 1D spectral extraction and analysis. Extract holds
/// boxcar flux extraction with optional wavelength calibration; Calibrate, Lines, and Redshift are
/// scaffolded roadmap slices that return HTTP 501 pending their Core algorithms (see spec.md).
/// </summary>
public static class SpectroscopyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapSpectroscopyEndpoints()
        {
            var group = app.MapGroup("/api/spectroscopy").WithTags("Spectroscopy");

            group.MapExtractEndpoint();

            group.MapCalibrateEndpoint();

            group.MapLinesEndpoint();

            group.MapRedshiftEndpoint();

            return group;
        }
    }
}
