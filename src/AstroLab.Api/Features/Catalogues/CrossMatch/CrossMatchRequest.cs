using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchRequest
{
    [JsonConstructor]
    private CrossMatchRequest(string fileId, ImmutableList<string> catalogueIds, double radiusArcsec)
    {
        FileId = fileId;
        CatalogueIds = catalogueIds;
        RadiusArcsec = radiusArcsec;
    }

    public string FileId { get; }

    public ImmutableList<string> CatalogueIds { get; }

    public double RadiusArcsec { get; }

    public static CrossMatchRequest Create(string fileId, ImmutableList<string> catalogueIds, double radiusArcsec)
    {
        var request = new CrossMatchRequest(fileId, catalogueIds, radiusArcsec);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileId);

        if (CatalogueIds is null || CatalogueIds.Count == 0 || CatalogueIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("catalogueIds must contain at least one non-empty catalogue identifier.", nameof(CatalogueIds));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(RadiusArcsec);
    }
}
