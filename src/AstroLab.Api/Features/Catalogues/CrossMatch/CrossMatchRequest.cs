using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchRequest
{
    [JsonConstructor]
    private CrossMatchRequest(string fileId, double radiusArcsec)
    {
        FileId = fileId;
        RadiusArcsec = radiusArcsec;
    }

    public string FileId { get; }

    public double RadiusArcsec { get; }

    public static CrossMatchRequest Create(string fileId, double radiusArcsec)
    {
        var request = new CrossMatchRequest(fileId, radiusArcsec);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(RadiusArcsec);
    }
}
