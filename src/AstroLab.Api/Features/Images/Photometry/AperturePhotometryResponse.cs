namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryResponse
{
    private AperturePhotometryResponse(string fileId, double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        FileId = fileId;

        RawFlux = rawFlux;

        ApertureArea = apertureArea;

        BackgroundPerPixel = backgroundPerPixel;

        NetFlux = netFlux;
    }

    public string FileId { get; }

    public double RawFlux { get; }

    public double ApertureArea { get; }

    public double BackgroundPerPixel { get; }

    public double NetFlux { get; }

    public static AperturePhotometryResponse Create(string fileId, double rawFlux, double apertureArea, double backgroundPerPixel, double netFlux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new AperturePhotometryResponse(fileId, rawFlux, apertureArea, backgroundPerPixel, netFlux);
    }
}
