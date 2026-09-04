namespace AstroLab.Api.Features.Images.Align;

/// <summary>
/// Roadmap slice: computing the geometric transform (offset, rotation, scale) needed to register
/// one staged image onto another's pixel grid. Request/response contract is final; the alignment
/// algorithm itself is not yet implemented (see spec.md §6.5), so this route always returns
/// HTTP 501.
/// </summary>
public static class AlignEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapAlignEndpoint()
        {
            group.MapPost("/align", AlignImages)
                .WithSummary("Computes the geometric transform to register one staged image onto another's pixel grid. Not yet implemented.");
        }
    }

    private static IResult AlignImages(ImageAlignRequest request)
    {
        request.Validate();

        return NotImplementedResult.Value("images.align.not_implemented", "Image alignment is not yet implemented.");
    }
}
