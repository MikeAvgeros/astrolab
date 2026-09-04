namespace AstroLab.Api.Features.Images.Background;

/// <summary>
/// Roadmap slice: modelling the 2D sky background of a staged image on a mesh, rather than a
/// single global estimate. Request/response contract is final; the modelling algorithm itself is
/// not yet implemented (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class BackgroundEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapBackgroundEndpoint()
        {
            group.MapGet("/{fileId}/background", ModelBackground)
                .WithSummary("Models the 2D sky background of a staged image on a mesh. Not yet implemented.");
        }
    }

    private static IResult ModelBackground(string fileId, int meshSizePixels = BackgroundModelRequest.DefaultMeshSizePixels)
    {
        _ = BackgroundModelRequest.Create(meshSizePixels);

        return NotImplementedResult.Value("images.background.not_implemented", "Image background modelling is not yet implemented.");
    }
}
