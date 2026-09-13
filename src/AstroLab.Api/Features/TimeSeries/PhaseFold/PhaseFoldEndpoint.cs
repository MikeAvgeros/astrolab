using System.Collections.Immutable;
using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.PhaseFold;

/// <summary>Folds a staged light curve around a supplied period and reference epoch.</summary>
public static class PhaseFoldEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapPhaseFoldEndpoint()
        {
            group.MapPost("/{fileId}/phase-fold", FoldPhaseAsync)
                .WithSummary("Folds a light curve around a supplied period and reference epoch, preserving original time.");
        }
    }

    private static async Task<IResult> FoldPhaseAsync(
        string fileId, PhaseFoldRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (lightCurveResult.IsFailure)
        {
            return lightCurveResult.Error.ToProblem();
        }

        var data = lightCurveResult.Value;

        var phase = new double[data.Time.Length];

        var foldResult = LightCurvePhaseFolder.Fold(data.Time, data.Flux, request.Period, request.ReferenceEpoch, phase);

        return foldResult.ToApiResult(_ => Results.Ok(
            PhaseFoldResponse.Create(fileId, [.. data.Time], [.. phase], [.. data.Flux])));
    }
}
