namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryResponse(string FileId, double RawFlux, double ApertureArea, double BackgroundPerPixel, double NetFlux);

/// <summary>Static factory accompanying <see cref="AperturePhotometryResponse"/>. Validates arguments before constructing.</summary>
public static class AperturePhotometryResponseFactory
{
    public static AperturePhotometryResponse Create(string fileId, double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new AperturePhotometryResponse(fileId, rawFlux, apertureArea, backgroundPerPixel, netFlux);
    }
}
