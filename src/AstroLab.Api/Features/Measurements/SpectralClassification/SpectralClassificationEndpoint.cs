using AstroLab.Api.Features.Spectroscopy;
using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Measurements.SpectralClassification;

/// <summary>
/// Estimates a coarse spectral classification (OBAFGKM) from the absorption/emission-line density
/// of a 1D spectrum collapsed from the full spatial extent of a staged spectroscopic frame (no
/// trace/aperture is requested here, unlike <c>Extract</c>). The response is always a model-derived
/// estimate, never a precise spectral subtype (see spec.md §6.5).
/// </summary>
public static class SpectralClassificationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSpectralClassificationEndpoint()
        {
            group.MapGet("/{fileId}/spectral-classification", ClassifySpectrumAsync)
                .WithSummary("Estimates a coarse spectral classification from a staged spectrum's absorption/emission line density.");
        }
    }

    private static async Task<IResult> ClassifySpectrumAsync(
        string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
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

        var classifyResult = SpectralTypeClassifier.Classify(extractResult.Value);

        return classifyResult.ToApiResult(estimate =>
            Results.Ok(SpectralClassificationResponse.Create(fileId, estimate.SpectralType, estimate.Confidence)));
    }
}
