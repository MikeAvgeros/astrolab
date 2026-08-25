namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldCoordinateResponse(string FileId, double RightAscension, double Declination);

/// <summary>Static factory accompanying <see cref="WorldCoordinateResponse"/>. Validates arguments before constructing.</summary>
public static class WorldCoordinateResponseFactory
{
    public static WorldCoordinateResponse Create(string fileId, double rightAscension, double declination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WorldCoordinateResponse(fileId, rightAscension, declination);
    }
}
