namespace AstroLab.Api.Features.Images.Cutout;

/// <summary>Roadmap: extracts a rectangular (or sky-region) cutout from a staged image. Not yet implemented (HTTP 501).</summary>
public static class CutoutEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCutoutEndpoint()
        {
            group.MapGet("/{fileId}/cutout", GetCutoutAsync)
                .WithSummary("Roadmap: extracts a rectangular pixel region, or a WCS-based sky region, from a staged image. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> GetCutoutAsync(
        string fileId,
        int? x, int? y, int? width, int? height,
        double? rightAscension, double? declination, double? radiusArcseconds,
        CancellationToken cancellationToken)
    {
        _ = CutoutRequest.Create(x, y, width, height, rightAscension, declination, radiusArcseconds);

        return Task.FromResult(NotImplementedResult.Value(
            "image.cutout.not_implemented",
            "Image cutout extraction is not yet implemented."));
    }
}
