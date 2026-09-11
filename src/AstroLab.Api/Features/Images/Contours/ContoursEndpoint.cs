namespace AstroLab.Api.Features.Images.Contours;

/// <summary>Roadmap: generates contour level geometry from a staged image's pixel data. Not yet implemented (HTTP 501).</summary>
public static class ContoursEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapContoursEndpoint()
        {
            group.MapGet("/{fileId}/contours", GetContoursAsync)
                .WithSummary("Roadmap: generates scientific contour geometry from an image's pixel data, at configurable or automatically calculated levels. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> GetContoursAsync(
        string fileId, double[]? levels, int? levelCount, CancellationToken cancellationToken)
    {
        _ = ContoursRequest.Create(levels, levelCount);

        return Task.FromResult(NotImplementedResult.Value(
            "image.contours.not_implemented",
            "Contour generation is not yet implemented."));
    }
}
