using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Fits;

/// <summary>Upload and header-inspection endpoints for user-supplied FITS files.</summary>
public static class FitsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapFitsEndpoints()
        {
            var group = app.MapGroup("/api/fits").WithTags("Fits");

            group.MapPost("/upload", UploadAsync)
                .WithSummary("Streams a raw FITS file body directly to local staging storage.")
                .DisableAntiforgery();

            group.MapGet("/{fileId}/header", GetHeaderAsync)
                .WithSummary("Parses and returns the primary header of a staged FITS file.");

            return group;
        }
    }

    private static async Task<IResult> UploadAsync(HttpRequest request, ILocalFileStore fileStore, CancellationToken cancellationToken)
    {
        var fileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(fileId, request.BodyReader, cancellationToken);

        return writeResult.ToApiResult(stored =>
            Results.Created($"/api/fits/{fileId}/header", new FitsUploadResponse(fileId, stored.SizeBytes)));
    }

    private static async Task<IResult> GetHeaderAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var headerResult = await datasetReader.ReadHeaderAsync(fileId, cancellationToken);
        return headerResult.ToApiResult(header => Results.Ok(FitsHeaderResponse.FromHeader(fileId, header)));
    }
}
