using AstroLab.Core.Astrometry;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record OrientationResponse
{
    private OrientationResponse(string fileId, double rotationDegrees, bool isMirrored)
    {
        FileId = fileId;
        RotationDegrees = rotationDegrees;
        IsMirrored = isMirrored;
    }

    public string FileId { get; }

    public double RotationDegrees { get; }

    public bool IsMirrored { get; }

    public static OrientationResponse Create(string fileId, Wcs wcs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new OrientationResponse(fileId, wcs.RotationDegrees, wcs.IsMirrored);
    }
}
