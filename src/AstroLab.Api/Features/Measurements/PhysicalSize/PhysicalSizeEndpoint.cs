namespace AstroLab.Api.Features.Measurements.PhysicalSize;

/// <summary>
/// Roadmap slice: converting an angular size and a known distance into a physical size. Request/
/// response contract is final; the calculation itself is not yet implemented (see spec.md §4.1),
/// so this route always returns HTTP 501. Distance is a caller-supplied assumption, so the result
/// is always distance-dependent rather than a direct measurement.
/// </summary>
public static class PhysicalSizeEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPhysicalSizeEndpoint()
        {
            group.MapGet("/physical-size", CalculatePhysicalSize)
                .WithSummary("Converts an angular size and a known distance into a physical size. Not yet implemented.");
        }
    }

    private static IResult CalculatePhysicalSize(double angularSizeArcsec, double distanceParsecs)
    {
        _ = PhysicalSizeRequest.Create(angularSizeArcsec, distanceParsecs);

        return NotImplementedResult.Value("measurements.physicalsize.not_implemented", "Angular-to-physical size conversion is not yet implemented.");
    }
}
