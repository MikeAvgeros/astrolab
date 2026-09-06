using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

/// <summary>Fits a wavelength-dispersion solution from known pixel/wavelength pairs.</summary>
public static class CalibrateEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCalibrateEndpoint()
        {
            group.MapPost("/{fileId}/calibrate", CalibrateWavelengths)
                .WithSummary("Fits a wavelength-dispersion solution from known pixel/wavelength pairs.");
        }
    }

    private static IResult CalibrateWavelengths(string fileId, WavelengthCalibrationRequest request)
    {
        var fitResult = SpectrumExtractor.FitDispersionSolution([.. request.PixelPositions], [.. request.KnownWavelengths]);

        return fitResult.ToApiResult(fit =>
            Results.Ok(WavelengthCalibrationResponse.Create(fileId, [.. fit.Coefficients], fit.ResidualRms)));
    }
}
