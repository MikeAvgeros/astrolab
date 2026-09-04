namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

public sealed record SurfaceBrightnessResponse
{
    private SurfaceBrightnessResponse(string fileId, double surfaceBrightnessMagPerArcsec2)
    {
        FileId = fileId;
        SurfaceBrightnessMagPerArcsec2 = surfaceBrightnessMagPerArcsec2;
    }

    public string FileId { get; }

    public double SurfaceBrightnessMagPerArcsec2 { get; }

    public static SurfaceBrightnessResponse Create(string fileId, double surfaceBrightnessMagPerArcsec2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SurfaceBrightnessResponse(fileId, surfaceBrightnessMagPerArcsec2);
    }
}
