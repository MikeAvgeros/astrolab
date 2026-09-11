namespace AstroLab.Api.Features.Fits.Quality;

/// <summary>Roadmap: reports cross-cutting data-quality statistics (invalid pixel counts, saturation, dynamic range, usable-pixel fraction) for a staged FITS dataset. Not yet implemented (HTTP 501).</summary>
public static class QualityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapQualityEndpoint()
        {
            group.MapGet("/{fileId}/quality", GetQualityAsync)
                .WithSummary("Roadmap: reports data-quality statistics for a staged FITS dataset, distinguishing not-present, not-measurable, and measured-as-zero. Not yet implemented (HTTP 501).");
        }
    }

    private static Task<IResult> GetQualityAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(NotImplementedResult.Value(
            "fits.quality.not_implemented",
            "Data-quality analysis is not yet implemented."));
}
