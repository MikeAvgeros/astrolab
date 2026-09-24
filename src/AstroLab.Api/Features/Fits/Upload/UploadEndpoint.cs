using AstroLab.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace AstroLab.Api.Features.Fits.Upload;

/// <summary>
/// Streams a raw FITS file upload directly to local staging storage, then validates the staged
/// file's header structure and discards it if it is not a conforming FITS file.
/// </summary>
public static class UploadEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapUploadEndpoint()
        {
            group.MapPost("/upload", UploadAsync)
                .WithSummary("Streams a raw FITS file body to local staging storage, rejecting files that are not valid FITS.")
                .DisableAntiforgery();
        }
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ILocalFileStore fileStore,
        StagedFitsValidator stagedFitsValidator,
        IOptions<LocalFileStoreOptions> storageOptions,
        CancellationToken cancellationToken)
    {
        var maxRequestBodySizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        
        if (maxRequestBodySizeFeature is { IsReadOnly: false })
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = storageOptions.Value.MaxUploadSizeBytes;
        }

        var fileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(fileId, request.BodyReader, cancellationToken);

        if (writeResult.IsFailure)
        {
            return writeResult.Error.ToProblem();
        }
        
        var validationResult = await stagedFitsValidator.ValidateOrDiscardAsync(fileId, cancellationToken);

        if (validationResult.IsFailure)
        {
            return validationResult.Error.ToProblem();
        }

        return Results.Created(
            $"/api/fits/{fileId}/header", FitsUploadResponse.Create(fileId, writeResult.Value.SizeBytes));
    }
}
