using AstroLab.Core.Astrometry;

namespace AstroLab.Api.Features.Measurements.PhysicalSize;

/// <summary>
/// Converts an angular size and a caller-supplied distance into a physical size, using the
/// small-angle relation that defines the parsec: physical size in AU equals angular size in
/// arcseconds times distance in parsecs. Distance is a caller-supplied assumption, so the result is
/// always distance-dependent rather than a direct measurement.
/// </summary>
public static class PhysicalSizeEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPhysicalSizeEndpoint()
        {
            group.MapGet("/physical-size", CalculatePhysicalSize)
                .WithSummary("Converts an angular size and a known distance into a physical size.");
        }
    }

    private static IResult CalculatePhysicalSize(double angularSizeArcsec, double distanceParsecs)
    {
        var request = PhysicalSizeRequest.Create(angularSizeArcsec, distanceParsecs);

        var physicalSizeResult = PhysicalSizeCalculator.ComputeAstronomicalUnits(request.AngularSizeArcsec, request.DistanceParsecs);

        return physicalSizeResult.ToApiResult(physicalSizeAu =>
            Results.Ok(PhysicalSizeResponse.Create(request.AngularSizeArcsec, request.DistanceParsecs, physicalSizeAu)));
    }
}
