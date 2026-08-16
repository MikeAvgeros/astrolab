using AstroLab.Infrastructure.Archives;
using AstroLab.Infrastructure.ESO;
using AstroLab.Infrastructure.MAST;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Observations;

/// <summary>Archive metadata search and dataset download endpoints for ESO and MAST.</summary>
public static class ObservationsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapObservationsEndpoints()
        {
            var group = app.MapGroup("/api/observations").WithTags("Observations");

            group.MapGet("/search", SearchAsync)
                .WithSummary("Searches an upstream archive's observation catalogue.");

            group.MapPost("/download", DownloadAsync)
                .WithSummary("Downloads a dataset from an upstream archive and stages it to local storage.");

            return group;
        }
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] ObservationSearchRequest request,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        CancellationToken cancellationToken)
    {
        var query = new ArchiveSearchQuery(request.Target, request.Instrument, MaxResults: request.MaxResults);
        var client = ResolveClient(request.Archive, esoClient, mastClient);

        var result = await client.SearchAsync(query, cancellationToken);
        return result.ToApiResult(observations => Results.Ok(ObservationSearchResponse.FromObservations(observations)));
    }

    private static async Task<IResult> DownloadAsync(
        DownloadRequest request,
        IEsoArchiveClient esoClient,
        IMastArchiveClient mastClient,
        ILocalFileStore fileStore,
        CancellationToken cancellationToken)
    {
        var client = ResolveClient(request.Archive, esoClient, mastClient);

        var downloadResult = await client.DownloadAsync(request.DatasetId, cancellationToken);
        if (downloadResult.IsFailure)
        {
            return downloadResult.Error.ToProblem();
        }

        await using var download = downloadResult.Value;
        var fileId = fileStore.CreateStagingKey("fits");
        var writeResult = await fileStore.WriteAsync(fileId, download.Content, cancellationToken);

        return writeResult.ToApiResult(stored =>
            Results.Created($"/api/fits/{fileId}/header", new DownloadResponse(fileId, request.Archive, stored.SizeBytes)));
    }

    private static IArchiveClient ResolveClient(ArchiveSource archive, IEsoArchiveClient esoClient, IMastArchiveClient mastClient) => archive switch
    {
        ArchiveSource.Eso => esoClient,
        ArchiveSource.Mast => mastClient,
        _ => throw new ArgumentOutOfRangeException(nameof(archive), archive, "Unknown archive source."),
    };
}
