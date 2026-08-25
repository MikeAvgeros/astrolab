namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchRequest(string FileId, double RadiusArcsec);

/// <summary>Static factory accompanying <see cref="CrossMatchRequest"/>. Validates arguments before constructing.</summary>
public static class CrossMatchRequestFactory
{
    public static CrossMatchRequest Create(string fileId, double radiusArcsec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusArcsec);

        return new CrossMatchRequest(fileId, radiusArcsec);
    }
}
