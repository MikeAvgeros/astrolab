using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

/// <summary>
/// Fits a wavelength-dispersion solution from known pixel/wavelength pairs and applies it to a 1D
/// spectrum extracted from the staged file, optionally also applying flux calibration when a
/// sensitivity curve is supplied.
/// </summary>
public static class CalibrateEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCalibrateEndpoint()
        {
            group.MapPost("/{fileId}/calibrate", CalibrateAsync)
                .WithSummary("Applies wavelength (and, where supplied, flux) calibration to a 1D spectrum extracted from the staged file.");
        }
    }

    private static async Task<IResult> CalibrateAsync(
        string fileId, WavelengthCalibrationRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var fitResult = SpectrumExtractor.FitDispersionSolution([.. request.PixelPositions], [.. request.KnownWavelengths]);

        if (fitResult.IsFailure)
        {
            return fitResult.Error.ToProblem();
        }

        var (coefficients, residualRms) = fitResult.Value;

        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var extractResult = SpectrumFrameExtraction.ExtractFullFrame(dataset);

        if (extractResult.IsFailure)
        {
            return extractResult.Error.ToProblem();
        }

        var spectrum = extractResult.Value;

        var dispersionBins = spectrum.Length;

        var pixelIndices = new double[dispersionBins];

        for (var i = 0; i < dispersionBins; i++)
        {
            pixelIndices[i] = i;
        }

        var wavelengths = new double[dispersionBins];

        var wavelengthResult = SpectrumExtractor.ComputeWavelengths(pixelIndices, coefficients, wavelengths);

        if (wavelengthResult.IsFailure)
        {
            return wavelengthResult.Error.ToProblem();
        }

        var fluxCalibrated = false;

        if (request.FluxSensitivity is { Count: > 0 } sensitivity)
        {
            var calibrationResult = SpectrumExtractor.ApplyFluxCalibration(spectrum, [.. sensitivity]);

            if (calibrationResult.IsFailure)
            {
                return calibrationResult.Error.ToProblem();
            }

            fluxCalibrated = true;
        }

        return Results.Ok(WavelengthCalibrationResponse.Create(
            fileId, [.. coefficients], residualRms, [.. wavelengths], [.. spectrum], fluxCalibrated));
    }
}
