using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Fits.Observation;

/// <summary>Reports observation metadata and provenance for a staged FITS file, keeping values read directly from the FITS header distinct from values AstroLab derives (such as WCS-based pixel scale).</summary>
public static class ObservationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapObservationEndpoint()
        {
            group.MapGet("/{fileId}/observation", GetObservationAsync)
                .WithSummary("Reports observation date/time, target, instrument, filter, exposure, detector, and calibration/provenance metadata, plus AstroLab-derived WCS/pixel-scale information.");
        }
    }

    private static async Task<IResult> GetObservationAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var hdusResult = await datasetReader.ReadAllHdusAsync(fileId, cancellationToken);

        return hdusResult.ToApiResult(hdus => Results.Ok(ObservationResponse.Create(fileId, hdus)));
    }
}
