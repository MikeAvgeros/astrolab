namespace AstroLab.Api.Features.Measurements.SpectralClassification;

/// <summary>
/// Roadmap slice: estimating a spectral classification (e.g. OBAFGKM type) from a staged
/// spectrum's overall shape and features. Response contract is final; the classification
/// algorithm itself is not yet implemented (see spec.md §4.1), so this route always returns
/// HTTP 501. The response is always a model-derived estimate, never a direct measurement.
/// </summary>
public static class SpectralClassificationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSpectralClassificationEndpoint()
        {
            group.MapGet("/{fileId}/spectral-classification", ClassifySpectrum)
                .WithSummary("Estimates a spectral classification from a staged spectrum. Not yet implemented.");
        }
    }

    private static IResult ClassifySpectrum(string fileId) =>
        NotImplementedResult.Value("measurements.spectralclassification.not_implemented", "Spectral classification is not yet implemented.");
}
