namespace AstroLab.Api.Features.Images.Stack;

/// <summary>
/// Roadmap slice: combining multiple staged images of the same field into a single stacked image
/// via mean, median, or sum combination. Request/response contract is final; the stacking
/// algorithm itself is not yet implemented (see spec.md §6.5), so this route always returns
/// HTTP 501.
/// </summary>
public static class StackEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapStackEndpoint()
        {
            group.MapPost("/stack", StackImages)
                .WithSummary("Combines multiple staged images into a single stacked image. Not yet implemented.");
        }
    }

    private static IResult StackImages(ImageStackRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("images.stack.not_implemented", "Image stacking is not yet implemented.");
    }
}
