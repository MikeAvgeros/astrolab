using AstroLab.Core.Astrometry;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelScaleResponse
{
    private PixelScaleResponse(
        string fileId, double pixelScaleXArcsecPerPixel, double pixelScaleYArcsecPerPixel, double pixelScaleXDegrees, double pixelScaleYDegrees)
    {
        FileId = fileId;
        PixelScaleXArcsecPerPixel = pixelScaleXArcsecPerPixel;
        PixelScaleYArcsecPerPixel = pixelScaleYArcsecPerPixel;
        PixelScaleXDegrees = pixelScaleXDegrees;
        PixelScaleYDegrees = pixelScaleYDegrees;
    }

    public string FileId { get; }

    public double PixelScaleXArcsecPerPixel { get; }

    public double PixelScaleYArcsecPerPixel { get; }

    public double PixelScaleXDegrees { get; }

    public double PixelScaleYDegrees { get; }

    public static PixelScaleResponse Create(string fileId, Wcs wcs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PixelScaleResponse(fileId, wcs.PixelScaleXArcsecPerPixel, wcs.PixelScaleYArcsecPerPixel, wcs.PixelScaleXDegrees, wcs.PixelScaleYDegrees);
    }
}
