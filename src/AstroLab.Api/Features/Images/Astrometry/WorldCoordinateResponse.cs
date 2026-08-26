namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldCoordinateResponse
{
    private WorldCoordinateResponse(string fileId, double rightAscension, double declination)
    {
        FileId = fileId;
        RightAscension = rightAscension;
        Declination = declination;
    }

    public string FileId { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public static WorldCoordinateResponse Create(string fileId, double rightAscension, double declination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WorldCoordinateResponse(fileId, rightAscension, declination);
    }
}
