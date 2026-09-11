namespace AstroLab.Api.Features.Images.ApertureCorrection;

/// <summary>Roadmap: corrects an aperture flux measurement using a supplied correction factor. Not yet implemented (HTTP 501).</summary>
public static class ApertureCorrectionEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapApertureCorrectionEndpoint()
        {
            group.MapPost("/{fileId}/photometry/aperture-correction", ApplyApertureCorrectionAsync)
                .WithSummary("Roadmap: applies an aperture correction to a measured flux and propagates uncertainty where available. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> ApplyApertureCorrectionAsync(
        string fileId, ApertureCorrectionRequest request, CancellationToken cancellationToken)
    {
        request.Validate();

        return Task.FromResult(NotImplementedResult.Value(
            "photometry.aperture_correction.not_implemented",
            "Aperture correction is not yet implemented."));
    }
}
