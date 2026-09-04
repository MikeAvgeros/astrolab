using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Compare;

public sealed record ImageCompareRequest
{
    [JsonConstructor]
    private ImageCompareRequest(string fileId, string comparisonFileId)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public static ImageCompareRequest Create(string fileId, string comparisonFileId)
    {
        var request = new ImageCompareRequest(fileId, comparisonFileId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(ComparisonFileId);
    }
}
