namespace AstroLab.Api.Features.Images.Compare;

/// <summary>
/// Roadmap slice: comparing and differencing two staged images, e.g. for transient detection.
/// Request/response contract is final; the comparison algorithm itself is not yet implemented
/// (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class CompareEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/compare", CompareImages)
                .WithSummary("Compares and differences two staged images. Not yet implemented.");
        }
    }

    private static IResult CompareImages(ImageCompareRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("images.compare.not_implemented", "Image comparison is not yet implemented.");
    }
}
