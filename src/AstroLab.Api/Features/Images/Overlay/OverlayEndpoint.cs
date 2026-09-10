using AstroLab.Core.Sources;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Overlay;

/// <summary>Renders a staged image to PNG with detected sources overlaid as markers.</summary>
public static class OverlayEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapOverlayEndpoint()
        {
            group.MapGet("/{fileId}/render/overlay", RenderOverlayAsync)
                .WithSummary("Renders a staged image to PNG with detected sources overlaid.");
        }
    }

    private static async Task<IResult> RenderOverlayAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        var request = OverlayRenderRequest.Create(thresholdSigma, minimumArea, maxSources);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var detectionResult = SourceDetector.Detect(dataset.Pixels, width, height, request.ThresholdSigma, request.MinimumArea, request.MaxSources);

        if (detectionResult.IsFailure)
        {
            return detectionResult.Error.ToProblem();
        }

        var renderOptions = RenderOptions.Create(maxDimension: null);

        var renderResult = FitsImageRenderer.Render(dataset.Pixels, width, height, renderOptions);

        return renderResult.ToApiResult(rendered =>
        {
            var overlaid = OverlayRenderer.DrawSourceMarkers(rendered, detectionResult.Value);

            return Results.File(PngRenderer.Encode(overlaid), "image/png");
        });
    }
}
