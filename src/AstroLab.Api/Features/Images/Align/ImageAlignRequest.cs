using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Align;

public sealed record ImageAlignRequest
{
    [JsonConstructor]
    private ImageAlignRequest(string fileId, string referenceFileId)
    {
        FileId = fileId;
        ReferenceFileId = referenceFileId;
    }

    public string FileId { get; }

    public string ReferenceFileId { get; }

    public static ImageAlignRequest Create(string fileId, string referenceFileId)
    {
        var request = new ImageAlignRequest(fileId, referenceFileId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(ReferenceFileId);
    }
}
