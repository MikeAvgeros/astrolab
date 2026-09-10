using AstroLab.Core.Result;
using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Redshift;

/// <summary>
/// Estimates redshift either from paired observed-vs-rest spectral line wavelengths, or (when a
/// full observed spectrum and rest-frame template are supplied) by cross-correlating the template
/// against the observed spectrum over a trial redshift range.
/// </summary>
public static class RedshiftEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapRedshiftEndpoint()
        {
            group.MapPost("/{fileId}/redshift", EstimateRedshift)
                .WithSummary(
                    "Estimates redshift from observed-vs-rest line wavelengths, or by cross-correlating an observed spectrum against a rest-frame template.");
        }
    }

    private static IResult EstimateRedshift(string fileId, RedshiftEstimationRequest request)
    {
        if (request.HasTemplateInputs)
        {
            return EstimateByCrossCorrelation(fileId, request);
        }

        if (request.ObservedWavelengths is null || request.RestWavelengths is null)
        {
            return Error.Validation(
                "spectroscopy.redshift.missing_inputs",
                "Either ObservedWavelengths/RestWavelengths, or ObservedSpectrumWavelengths/ObservedFlux/TemplateWavelengths/TemplateFlux, must be supplied.")
                .ToProblem();
        }

        var estimateResult = RedshiftEstimator.Estimate([.. request.ObservedWavelengths], [.. request.RestWavelengths]);

        return estimateResult.ToApiResult(estimate =>
            Results.Ok(RedshiftEstimationResponse.Create(fileId, estimate.Redshift, estimate.Uncertainty, "line_pairs")));
    }

    private static IResult EstimateByCrossCorrelation(string fileId, RedshiftEstimationRequest request)
    {
        if (request.MinRedshift is not { } minRedshift || request.MaxRedshift is not { } maxRedshift)
        {
            return Error.Validation(
                "spectroscopy.redshift.missing_range", "MinRedshift and MaxRedshift are required for the cross-correlation method.")
                .ToProblem();
        }

        var correlateResult = SpectrumCrossCorrelator.CorrelateAgainstTemplate(
            [.. request.ObservedSpectrumWavelengths!],
            [.. request.ObservedFlux!],
            [.. request.TemplateWavelengths!],
            [.. request.TemplateFlux!],
            minRedshift,
            maxRedshift);

        return correlateResult.ToApiResult(correlate =>
        {
            var gridResolution = (maxRedshift - minRedshift) / (SpectrumCrossCorrelator.DefaultRedshiftGridSize - 1);

            return Results.Ok(RedshiftEstimationResponse.Create(fileId, correlate.Redshift, gridResolution / 2.0, "cross_correlation"));
        });
    }
}
