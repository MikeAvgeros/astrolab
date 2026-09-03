using System.Text.Json.Serialization;
using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadRequest
{
    [JsonConstructor]
    private DownloadRequest(ArchiveSource archive, string datasetId)
    {
        Archive = archive;
        DatasetId = datasetId;
    }

    public ArchiveSource Archive { get; }

    public string DatasetId { get; }

    public static DownloadRequest Create(ArchiveSource archive, string datasetId)
    {
        var request = new DownloadRequest(archive, datasetId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(DatasetId);
    }
}
