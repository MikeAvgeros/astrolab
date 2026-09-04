using AstroLab.Api.Features.Measurements.GalaxyMorphology;
using AstroLab.Api.Features.Measurements.PhysicalSize;
using AstroLab.Api.Features.Measurements.RadialVelocity;
using AstroLab.Api.Features.Measurements.SpectralClassification;
using AstroLab.Api.Features.Measurements.StellarColour;
using AstroLab.Api.Features.Measurements.StellarTemperature;
using AstroLab.Api.Features.Measurements.SurfaceBrightness;

namespace AstroLab.Api.Features.Measurements;

public static class MeasurementsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapMeasurementsEndpoints()
        {
            var group = app.MapGroup("/api/measurements").WithTags("Measurements");

            group.MapStellarColourEndpoint();

            group.MapStellarTemperatureEndpoint();

            group.MapSpectralClassificationEndpoint();

            group.MapRadialVelocityEndpoint();

            group.MapGalaxyMorphologyEndpoint();

            group.MapSurfaceBrightnessEndpoint();

            group.MapPhysicalSizeEndpoint();
        }
    }
}
