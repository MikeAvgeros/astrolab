using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Archives.Download;

/// <summary>
/// Downloads a dataset from an upstream archive (ESO or MAST), stages it to local storage, and
/// discards it if the archive returned something that is not a conforming FITS file.
/// </summary>
public static class DownloadEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDownloadEndpoint()
        {
            group.MapPost("/download", DownloadAsync)
                .WithSummary("Downloads a dataset from an upstream archive and stages it to local storage, rejecting non-FITS content.");
        }
    }

    private static async Task<IResult> DownloadAsync(
        DownloadRequest request,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        ILocalFileStore fileStore,
        StagedFitsValidator stagedFitsValidator,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var client = ArchiveClientResolver.Resolve(request.Archive!.Value, esoClient, mastClient);

        var downloadResult = await client.DownloadAsync(request.DatasetId, cancellationToken);

        if (downloadResult.IsFailure)
        {
            return downloadResult.Error.ToProblem();
        }

        await using var download = downloadResult.Value;

        var fileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(fileId, download.Content, cancellationToken);

        if (writeResult.IsFailure)
        {
            return writeResult.Error.ToProblem();
        }

        var validationResult = await stagedFitsValidator.ValidateOrDiscardAsync(fileId, cancellationToken);

        if (validationResult.IsFailure)
        {
            var error = validationResult.Error;

            return Error.Infrastructure(
                    "archive.download_invalid_fits",
                    $"The {request.Archive.Value} archive returned content that is not a valid FITS file ({error.Code}: {error.Message})")
                .ToProblem();
        }

        return Results.Created(
            $"/api/fits/{fileId}/header", DownloadResponse.Create(fileId, request.Archive.Value, writeResult.Value.SizeBytes));
    }
}
