using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Fits.Inspect;

/// <summary>Parses and returns HDU/header metadata for a staged FITS file.</summary>
public static class InspectEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapInspectEndpoint()
        {
            group.MapGet("/{fileId}/header", GetHeaderAsync)
                .WithSummary("Parses every HDU of a staged FITS file, classifies its scientific data type, and returns its metadata.");
        }
    }

    private static async Task<IResult> GetHeaderAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var hdusResult = await datasetReader.ReadAllHdusAsync(fileId, cancellationToken);
        return hdusResult.ToApiResult(hdus => Results.Ok(FitsHeaderResponse.FromInspection(fileId, hdus)));
    }
}
