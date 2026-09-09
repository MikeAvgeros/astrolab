using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Background;

/// <summary>Models the 2D sky background of a staged image on a mesh, reporting a robust background level and noise floor for source detection.</summary>
public static class BackgroundEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapBackgroundEndpoint()
        {
            group.MapGet("/{fileId}/background", ModelBackgroundAsync)
                .WithSummary("Models the 2D sky background of a staged image on a mesh.");
        }
    }

    private static async Task<IResult> ModelBackgroundAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        int meshSizePixels = BackgroundModelRequest.DefaultMeshSizePixels)
    {
        var request = BackgroundModelRequest.Create(meshSizePixels);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var modelResult = ImageBackgroundModeller.Model(dataset.Pixels, width, height, request.MeshSizePixels);

        return modelResult.ToApiResult(model =>
            Results.Ok(BackgroundModelResponse.Create(fileId, model.MeshSizePixels, model.MedianBackground, model.BackgroundRms)));
    }
}
