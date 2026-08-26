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
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        return new CrossMatchRequest(fileId, radiusArcsec);
    }
}
