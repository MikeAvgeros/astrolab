namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

/// <summary>
/// Roadmap slice: fitting a wavelength-dispersion solution from known pixel/wavelength pairs.
/// Request/response contract is final; the fitting algorithm itself is not yet implemented (see
/// spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class CalibrateEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCalibrateEndpoint()
        {
            group.MapPost("/{fileId}/calibrate", CalibrateWavelengths)
                .WithSummary("Fits a wavelength-dispersion solution from known pixel/wavelength pairs. Not yet implemented.");
        }
    }

    private static IResult CalibrateWavelengths(string fileId, WavelengthCalibrationRequest request) =>
        NotImplementedResult.Value("spectroscopy.calibrate.not_implemented", "Wavelength calibration is not yet implemented.");
}
