using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Measurements.RadialVelocity;

/// <summary>
/// Measures radial velocity from the classical (non-relativistic) Doppler shift between a spectral
/// line's rest wavelength and its observed wavelength, as identified in file <c>fileId</c>'s staged
/// spectrum by the caller.
/// </summary>
public static class RadialVelocityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRadialVelocityEndpoint()
        {
            group.MapGet("/{fileId}/radial-velocity", MeasureRadialVelocity)
                .WithSummary("Measures radial velocity from a spectral line's Doppler shift.");
        }
    }

    private static IResult MeasureRadialVelocity(string fileId, double restWavelengthNm, double observedWavelengthNm)
    {
        var request = RadialVelocityRequest.Create(restWavelengthNm, observedWavelengthNm);

        var velocityResult = RadialVelocityEstimator.EstimateKilometersPerSecond(request.ObservedWavelengthNm, request.RestWavelengthNm);

        return velocityResult.ToApiResult(radialVelocityKmPerSec =>
            Results.Ok(RadialVelocityResponse.Create(fileId, radialVelocityKmPerSec)));
    }
}
