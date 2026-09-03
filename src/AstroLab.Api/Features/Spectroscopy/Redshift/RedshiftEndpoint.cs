namespace AstroLab.Api.Features.Spectroscopy.Redshift;

/// <summary>
/// Roadmap slice: redshift estimation from observed-vs-rest spectral line wavelengths.
/// Request/response contract is final; the estimation algorithm itself is not yet implemented
/// (see spec.md §4.1), so this route always returns HTTP 501.
/// </summary>
public static class RedshiftEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRedshiftEndpoint()
        {
            group.MapPost("/{fileId}/redshift", EstimateRedshift)
                .WithSummary("Estimates redshift from observed-vs-rest spectral line wavelengths. Not yet implemented.");
        }
    }

    private static IResult EstimateRedshift(string fileId, RedshiftEstimationRequest request) =>
        NotImplementedResult.Value("spectroscopy.redshift.not_implemented", "Redshift estimation is not yet implemented.");
}
