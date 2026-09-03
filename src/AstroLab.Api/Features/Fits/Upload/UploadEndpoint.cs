using AstroLab.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace AstroLab.Api.Features.Fits.Upload;

/// <summary>Streams a raw FITS file upload directly to local staging storage.</summary>
public static class UploadEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapUploadEndpoint()
        {
            group.MapPost("/upload", UploadAsync)
                .WithSummary("Streams a raw FITS file body directly to local staging storage.")
                .DisableAntiforgery();
        }
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request, ILocalFileStore fileStore, IOptions<LocalFileStoreOptions> storageOptions, CancellationToken cancellationToken)
    {
        var maxRequestBodySizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        
        if (maxRequestBodySizeFeature is { IsReadOnly: false })
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = storageOptions.Value.MaxUploadSizeBytes;
        }

        var fileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(fileId, request.BodyReader, cancellationToken);

        return writeResult.ToApiResult(stored =>
            Results.Created($"/api/fits/{fileId}/header", FitsUploadResponse.Create(fileId, stored.SizeBytes)));
    }
}
