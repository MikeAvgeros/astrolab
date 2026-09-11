using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Measurements.GalaxyMorphology;

/// <summary>
/// Estimates a galaxy's size, ellipticity, and coarse morphological type from the detected source
/// nearest a requested pixel position in a staged image, using a concentration-index proxy for a
/// Sersic profile fit (see <c>GalaxyMorphologyAnalyzer</c>). The response is always a model-derived
/// estimate, never a direct measurement (see spec.md §6.5).
/// </summary>
public static class GalaxyMorphologyEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapGalaxyMorphologyEndpoint()
        {
            group.MapGet("/{fileId}/galaxy-morphology", EstimateMorphologyAsync)
                .WithSummary("Estimates a galaxy's size, ellipticity, and coarse morphological type from the source nearest a pixel position.");
        }
    }

    private static async Task<IResult> EstimateMorphologyAsync(
        string fileId, double centerX, double centerY, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var request = GalaxyMorphologyRequest.Create(centerX, centerY);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var estimateResult = GalaxyMorphologyAnalyzer.Analyze(dataset.Pixels, width, height, request.CenterX, request.CenterY);

        return estimateResult.ToApiResult(estimate => Results.Ok(GalaxyMorphologyResponse.Create(
            fileId, estimate.EffectiveRadiusPixels, estimate.Ellipticity, estimate.MorphologicalType)));
    }
}
