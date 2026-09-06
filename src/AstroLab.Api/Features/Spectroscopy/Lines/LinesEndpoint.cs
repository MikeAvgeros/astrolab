using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Lines;

/// <summary>
/// Detects spectral lines in a 1D spectrum collapsed from the full spatial extent of a staged
/// spectroscopic frame (no trace/aperture is requested here, unlike <c>Extract</c>).
/// </summary>
public static class LinesEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLinesEndpoint()
        {
            group.MapGet("/{fileId}/lines", DetectLinesAsync)
                .WithSummary("Detects spectral lines in an extracted 1D spectrum.");
        }
    }

    private static async Task<IResult> DetectLinesAsync(
        string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken, double? significanceThreshold = null)
    {
        var request = LineDetectionRequest.Create(significanceThreshold);

        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var axis = SpectrumExtractor.ResolveDispersionAxis(dataset.Hdu.Header);

        var dispersionBins = axis == DispersionAxis.Horizontal ? width : height;

        var spatialExtent = axis == DispersionAxis.Horizontal ? height : width;

        var traceCenters = new double[dispersionBins];

        Array.Fill(traceCenters, spatialExtent / 2.0);

        var spectrum = new double[dispersionBins];

        var extractResult = SpectrumExtractor.ExtractBoxcar(
            dataset.Pixels, width, height, axis, traceCenters, spatialExtent / 2.0, spectrum);

        if (extractResult.IsFailure)
        {
            return extractResult.Error.ToProblem();
        }

        var detectResult = SpectralLineDetector.Detect(spectrum, request.SignificanceThreshold ?? SpectralLineDetector.DefaultSignificanceSigma);

        return detectResult.ToApiResult(lines => Results.Ok(LineDetectionResponse.Create(
            fileId,
            [.. lines.Select(line => SpectralLineDto.Create(line.Position, line.Flux, line.Fwhm))])));
    }
}
