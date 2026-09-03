using AstroLab.Infrastructure.Archives;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Archives.Download;

/// <summary>Downloads a dataset from an upstream archive (ESO or MAST) and stages it to local storage.</summary>
public static class DownloadEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapDownloadEndpoint()
        {
            group.MapPost("/download", DownloadAsync)
                .WithSummary("Downloads a dataset from an upstream archive and stages it to local storage.");
        }
    }

    private static async Task<IResult> DownloadAsync(
        DownloadRequest request,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        ILocalFileStore fileStore,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var client = ArchiveClientResolver.Resolve(request.Archive, esoClient, mastClient);

        var downloadResult = await client.DownloadAsync(request.DatasetId, cancellationToken);

        if (downloadResult.IsFailure)
        {
            return downloadResult.Error.ToProblem();
        }

        await using var download = downloadResult.Value;

        var fileId = fileStore.CreateStagingKey("fits");

        var writeResult = await fileStore.WriteAsync(fileId, download.Content, cancellationToken);

        return writeResult.ToApiResult(stored =>
            Results.Created($"/api/fits/{fileId}/header", DownloadResponse.Create(fileId, request.Archive, stored.SizeBytes)));
    }
}
