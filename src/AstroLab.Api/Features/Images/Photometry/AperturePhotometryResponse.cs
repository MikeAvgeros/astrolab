namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryResponse(
    string FileId, double RawFlux, double ApertureArea, double BackgroundPerPixel, double NetFlux);
