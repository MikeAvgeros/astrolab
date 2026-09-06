using System.Collections.Immutable;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.TimeSeries.LightCurve;

/// <summary>Extracts a light curve (flux vs. time) from a staged time-series FITS table.</summary>
public static class LightCurveEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLightCurveEndpoint()
        {
            group.MapGet("/{fileId}/light-curve", GetLightCurveAsync)
                .WithSummary("Extracts a light curve (flux vs. time) from a staged time-series FITS table.");
        }
    }

    private static async Task<IResult> GetLightCurveAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var lightCurveResult = await datasetReader.LoadLightCurveAsync(fileId, cancellationToken);

        return lightCurveResult.ToApiResult(data => Results.Ok(
            LightCurveResponse.Create(fileId, data.Time.ToImmutableList(), data.Flux.ToImmutableList())));
    }
}
