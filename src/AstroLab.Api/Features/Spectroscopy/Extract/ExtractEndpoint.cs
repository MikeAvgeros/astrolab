using System.Collections.Immutable;
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
        request = SpectrumExtractionRequest.Create(request.Axis, request.TraceCenters, request.ApertureHalfWidth, request.DispersionCoefficients);

        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var dispersionBins = request.Axis == DispersionAxis.Horizontal ? width : height;

        var spectrum = new double[dispersionBins];

        var traceCenters = request.TraceCenters.ToArray();

        var extractResult = SpectrumExtractor.ExtractBoxcar(
            dataset.Pixels, width, height, request.Axis, traceCenters, request.ApertureHalfWidth, spectrum);

        if (extractResult.IsFailure)
        {
            return extractResult.Error.ToProblem();
        }

        ImmutableList<double>? wavelengths = null;

        if (request.DispersionCoefficients is { Count: > 0 } dispersionCoefficients)
        {
            var wavelengthBuffer = new double[dispersionBins];

            var pixelIndices = new double[dispersionBins];

            for (var i = 0; i < dispersionBins; i++)
            {
                pixelIndices[i] = i;
            }

            var wavelengthResult = SpectrumExtractor.ComputeWavelengths(pixelIndices, [.. dispersionCoefficients], wavelengthBuffer);

            if (wavelengthResult.IsFailure)
            {
                return wavelengthResult.Error.ToProblem();
            }

            wavelengths = [.. wavelengthBuffer];
        }

        return Results.Ok(SpectrumExtractionResponse.Create(fileId, wavelengths, [.. spectrum]));
    }
}
