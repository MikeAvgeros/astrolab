namespace AstroLab.Api.Features.Images.Align;

public sealed record ImageAlignResponse
{
    private ImageAlignResponse(string fileId, string referenceFileId, double offsetX, double offsetY, double rotationDegrees, double scale)
    {
        FileId = fileId;
        ReferenceFileId = referenceFileId;
        OffsetX = offsetX;
        OffsetY = offsetY;
        RotationDegrees = rotationDegrees;
        Scale = scale;
    }

    public string FileId { get; }

    public string ReferenceFileId { get; }

    public double OffsetX { get; }

    public double OffsetY { get; }

    public double RotationDegrees { get; }

    public double Scale { get; }

    public static ImageAlignResponse Create(string fileId, string referenceFileId, double offsetX, double offsetY, double rotationDegrees, double scale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(referenceFileId);

        return new ImageAlignResponse(fileId, referenceFileId, offsetX, offsetY, rotationDegrees, scale);
    }
}
