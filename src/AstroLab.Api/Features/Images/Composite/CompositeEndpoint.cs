namespace AstroLab.Api.Features.Images.Composite;

/// <summary>Roadmap: combines up to three staged images into an RGB colour composite. Not yet implemented (HTTP 501).</summary>
public static class CompositeEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapCompositeEndpoint()
        {
            group.MapPost("/composite", CreateCompositeAsync)
                .WithSummary("Roadmap: combines separate red/green/blue staged images into an RGB composite. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> CreateCompositeAsync(CompositeRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "image.composite.not_implemented",
            "RGB image compositing is not yet implemented."));
    }
}
