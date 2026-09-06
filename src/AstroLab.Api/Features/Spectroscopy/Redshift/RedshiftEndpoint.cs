using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Redshift;

/// <summary>Estimates redshift from paired observed-vs-rest spectral line wavelengths.</summary>
public static class RedshiftEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRedshiftEndpoint()
        {
            group.MapPost("/{fileId}/redshift", EstimateRedshift)
                .WithSummary("Estimates redshift from observed-vs-rest spectral line wavelengths.");
        }
    }

    private static IResult EstimateRedshift(string fileId, RedshiftEstimationRequest request)
    {
        var estimateResult = RedshiftEstimator.Estimate([.. request.ObservedWavelengths], [.. request.RestWavelengths]);

        return estimateResult.ToApiResult(estimate =>
            Results.Ok(RedshiftEstimationResponse.Create(fileId, estimate.Redshift, estimate.Uncertainty)));
    }
}
