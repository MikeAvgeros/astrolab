namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

public sealed record SurfaceBrightnessResponse
{
    private SurfaceBrightnessResponse(
        string fileId, double surfaceBrightnessMagPerArcsec2, double surfaceBrightnessUncertaintyMagPerArcsec2, double zeroPoint)
    {
        FileId = fileId;
        SurfaceBrightnessMagPerArcsec2 = surfaceBrightnessMagPerArcsec2;
        SurfaceBrightnessUncertaintyMagPerArcsec2 = surfaceBrightnessUncertaintyMagPerArcsec2;
        ZeroPoint = zeroPoint;
    }

    public string FileId { get; }

    public double SurfaceBrightnessMagPerArcsec2 { get; }

    public double SurfaceBrightnessUncertaintyMagPerArcsec2 { get; }
    
    public double ZeroPoint { get; }

    public static SurfaceBrightnessResponse Create(
        string fileId, double surfaceBrightnessMagPerArcsec2, double surfaceBrightnessUncertaintyMagPerArcsec2, double zeroPoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SurfaceBrightnessResponse(fileId, surfaceBrightnessMagPerArcsec2, surfaceBrightnessUncertaintyMagPerArcsec2, zeroPoint);
    }
}
