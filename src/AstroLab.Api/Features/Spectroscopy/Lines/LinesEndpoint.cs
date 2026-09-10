using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Lines;

/// <summary>
/// Detects and characterises spectral lines in a 1D spectrum collapsed from the full spatial extent
/// of a staged spectroscopic frame (no trace/aperture is requested here, unlike <c>Extract</c>).
/// When a wavelength-dispersion solution is supplied, line centroids and widths are reported as
/// physical wavelengths (see <c>Calibrate</c>); otherwise they remain dispersion-bin indices.
/// </summary>
public static class LinesEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLinesEndpoint()
        {
            group.MapGet("/{fileId}/lines", DetectLinesAsync)
                .WithSummary("Detects and characterises absorption/emission spectral lines in an extracted 1D spectrum.");
        }
    }

    private static async Task<IResult> DetectLinesAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double? significanceThreshold = null,
        double[]? dispersionCoefficients = null)
    {
        var request = LineDetectionRequest.Create(significanceThreshold, dispersionCoefficients);

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

        var detectResult = SpectralLineDetector.Detect(extractResult.Value, request.SignificanceThreshold ?? SpectralLineDetector.DefaultSignificanceSigma);

        return detectResult.ToApiResult(lines => Results.Ok(LineDetectionResponse.Create(
            fileId,
            [.. lines.Select(line => ToDto(line, request.DispersionCoefficients))])));
    }

    private static SpectralLineDto ToDto(DetectedSpectralLine line, double[]? dispersionCoefficients)
    {
        if (dispersionCoefficients is not { Length: > 0 })
        {
            return SpectralLineDto.Create(line.Position, line.Flux, line.Fwhm, line.Position, isWavelengthCalibrated: false);
        }

        var wavelength = SpectrumExtractor.EvaluateWavelength(line.Position, dispersionCoefficients);

        var leftWavelength = SpectrumExtractor.EvaluateWavelength(line.Position - line.Fwhm / 2.0, dispersionCoefficients);

        var rightWavelength = SpectrumExtractor.EvaluateWavelength(line.Position + line.Fwhm / 2.0, dispersionCoefficients);

        var fwhmWavelength = Math.Abs(rightWavelength - leftWavelength);

        return SpectralLineDto.Create(wavelength, line.Flux, fwhmWavelength, line.Position, isWavelengthCalibrated: true);
    }
}
