using AstroLab.Core.Astrometry;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WcsMetadataResponse
{
    private WcsMetadataResponse(
        string fileId, string? coordinateSystem, WcsProjection projection,
        double referencePixelX, double referencePixelY,
        double referenceRightAscension, double referenceDeclination,
        double pixelScaleXDegrees, double pixelScaleYDegrees,
        double rotationDegrees)
    {
        FileId = fileId;
        CoordinateSystem = coordinateSystem;
        Projection = projection;
        ReferencePixelX = referencePixelX;
        ReferencePixelY = referencePixelY;
        ReferenceRightAscension = referenceRightAscension;
        ReferenceDeclination = referenceDeclination;
        PixelScaleXDegrees = pixelScaleXDegrees;
        PixelScaleYDegrees = pixelScaleYDegrees;
        RotationDegrees = rotationDegrees;
    }

    public string FileId { get; }

    /// <summary>The <c>RADESYS</c> keyword value (e.g. <c>"ICRS"</c>), or <see langword="null"/> when the header does not declare one.</summary>
    public string? CoordinateSystem { get; }

    public WcsProjection Projection { get; }

    public double ReferencePixelX { get; }

    public double ReferencePixelY { get; }

    public double ReferenceRightAscension { get; }

    public double ReferenceDeclination { get; }

    public double PixelScaleXDegrees { get; }

    public double PixelScaleYDegrees { get; }

    /// <summary>Rotation of the pixel X axis relative to celestial north, in degrees, derived from the WCS linear transform.</summary>
    public double RotationDegrees { get; }

    public static WcsMetadataResponse Create(
        string fileId, string? coordinateSystem, WcsProjection projection,
        double referencePixelX, double referencePixelY,
        double referenceRightAscension, double referenceDeclination,
        double pixelScaleXDegrees, double pixelScaleYDegrees,
        double rotationDegrees)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WcsMetadataResponse(
            fileId, coordinateSystem, projection, referencePixelX, referencePixelY,
            referenceRightAscension, referenceDeclination, pixelScaleXDegrees, pixelScaleYDegrees, rotationDegrees);
    }

    public static WcsMetadataResponse FromWcs(string fileId, Wcs wcs) => Create(
        fileId, wcs.RadeSys, wcs.Projection,
        wcs.ReferencePixelX, wcs.ReferencePixelY,
        wcs.ReferenceRightAscension, wcs.ReferenceDeclination,
        wcs.PixelScaleXDegrees, wcs.PixelScaleYDegrees, wcs.RotationDegrees);
}
