using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Composite;

public sealed record CompositeRequest
{
    [JsonConstructor]
    private CompositeRequest(string redFileId, string greenFileId, string blueFileId)
    {
        RedFileId = redFileId;
        GreenFileId = greenFileId;
        BlueFileId = blueFileId;
    }

    public string RedFileId { get; }

    public string GreenFileId { get; }

    public string BlueFileId { get; }

    public static CompositeRequest Create(string redFileId, string greenFileId, string blueFileId)
    {
        var request = new CompositeRequest(redFileId, greenFileId, blueFileId);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(RedFileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(GreenFileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(BlueFileId);
    }
}
