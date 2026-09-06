using AstroLab.Core.TimeSeries;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.Detrend;

/// <summary>Removes a long-term trend (linear or moving-median) from a staged light curve.</summary>
public static class DetrendEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDetrendEndpoint()
        {
            group.MapPost("/{fileId}/detrend", DetrendLightCurveAsync)
                .WithSummary("Removes a long-term trend (linear or moving-median) from a staged light curve.");
        }
    }

    private static async Task<IResult> DetrendLightCurveAsync(
        string fileId, DetrendRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        if (lightCurveResult.IsFailure)
        {
            return lightCurveResult.Error.ToProblem();
        }

        var data = lightCurveResult.Value;

        var detrendResult = LightCurveDetrender.Detrend(data.Time, data.Flux, request.Method);

        return detrendResult.ToApiResult(detrended =>
            Results.Ok(DetrendResponse.Create(fileId, [.. data.Time], [.. detrended])));
    }
}
