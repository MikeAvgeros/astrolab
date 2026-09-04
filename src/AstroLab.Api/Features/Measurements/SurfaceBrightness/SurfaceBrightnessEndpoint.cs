namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

/// <summary>
/// Roadmap slice: measuring surface brightness (magnitude per square arcsecond) within an
/// aperture on a staged image. Request/response contract is final; the calculation itself is not
/// yet implemented (see spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class SurfaceBrightnessEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSurfaceBrightnessEndpoint()
        {
            group.MapGet("/{fileId}/surface-brightness", MeasureSurfaceBrightness)
                .WithSummary("Measures surface brightness within an aperture on a staged image. Not yet implemented.");
        }
    }

    private static IResult MeasureSurfaceBrightness(string fileId, double centerX, double centerY, double apertureRadius)
    {
        _ = SurfaceBrightnessRequest.Create(centerX, centerY, apertureRadius);

        return NotImplementedResult.Value("measurements.surfacebrightness.not_implemented", "Surface brightness measurement is not yet implemented.");
    }
}
