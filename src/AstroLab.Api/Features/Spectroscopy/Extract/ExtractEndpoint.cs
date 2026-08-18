using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Extract;

/// <summary>Extracts a 1D flux spectrum (optionally wavelength-calibrated) from the first image-bearing HDU classified as a spectrum.</summary>
public static class ExtractEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapExtractEndpoint()
        {
            group.MapPost("/{fileId}/extract", ExtractAsync)
                .WithSummary("Extracts a 1D flux spectrum (optionally wavelength-calibrated) from the first image-bearing HDU classified as a spectrum.");
        }
    }

    private static async Task<IResult> ExtractAsync(
        string fileId,
        SpectrumExtractionRequest request,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);
        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        var dataset = datasetResult.Value;
        var (width, height) = dataset.Image.Resolve2DDimensions();
        var dispersionBins = request.Axis == DispersionAxis.Horizontal ? width : height;

        var spectrum = new double[dispersionBins];
        var extractResult = SpectrumExtractor.ExtractBoxcar(
            dataset.Pixels, width, height, request.Axis, request.TraceCenters, request.ApertureHalfWidth, spectrum);
        if (extractResult.IsFailure)
        {
            return extractResult.Error.ToProblem();
        }

        double[]? wavelengths = null;
        if (request.DispersionCoefficients is { Length: > 0 })
        {
            wavelengths = new double[dispersionBins];
            var pixelIndices = new double[dispersionBins];
            for (var i = 0; i < dispersionBins; i++)
            {
                pixelIndices[i] = i;
            }

            var wavelengthResult = SpectrumExtractor.ComputeWavelengths(pixelIndices, request.DispersionCoefficients, wavelengths);
            if (wavelengthResult.IsFailure)
            {
                return wavelengthResult.Error.ToProblem();
            }
        }

        return Results.Ok(new SpectrumExtractionResponse(fileId, wavelengths, spectrum));
    }
}
