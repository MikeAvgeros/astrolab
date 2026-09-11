namespace AstroLab.Api.Features.Spectroscopy.EquivalentWidth;

/// <summary>Roadmap: calculates the equivalent width of a spectral feature over a supplied wavelength interval. Not yet implemented (HTTP 501).</summary>
public static class EquivalentWidthEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapEquivalentWidthEndpoint()
        {
            group.MapPost("/{fileId}/equivalent-width", CalculateEquivalentWidthAsync)
                .WithSummary("Roadmap: calculates the equivalent width (absorption or emission, per the documented sign convention) over a wavelength interval. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> CalculateEquivalentWidthAsync(string fileId, EquivalentWidthRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "spectroscopy.equivalent_width.not_implemented",
            "Equivalent width calculation is not yet implemented."));
    }
}
