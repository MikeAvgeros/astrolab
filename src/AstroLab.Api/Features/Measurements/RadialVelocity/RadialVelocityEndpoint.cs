namespace AstroLab.Api.Features.Measurements.RadialVelocity;

/// <summary>
/// Roadmap slice: measuring radial velocity from the Doppler shift between a spectral line's rest
/// and observed wavelength in a staged spectrum. Request/response contract is final; the
/// calculation itself is not yet implemented (see spec.md §6.5), so this route always returns
/// HTTP 501.
/// </summary>
public static class RadialVelocityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRadialVelocityEndpoint()
        {
            group.MapGet("/{fileId}/radial-velocity", MeasureRadialVelocity)
                .WithSummary("Measures radial velocity from a spectral line's Doppler shift. Not yet implemented.");
        }
    }

    private static IResult MeasureRadialVelocity(string fileId, double restWavelengthNm, double observedWavelengthNm)
    {
        _ = RadialVelocityRequest.Create(restWavelengthNm, observedWavelengthNm);

        return NotImplementedResult.Value("measurements.radialvelocity.not_implemented", "Radial velocity measurement is not yet implemented.");
    }
}
