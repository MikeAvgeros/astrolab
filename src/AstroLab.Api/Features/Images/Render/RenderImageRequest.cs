using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Render;

public sealed record RenderImageRequest(
    StretchMode Stretch = StretchMode.Asinh,
    ColorMap ColorMap = ColorMap.Grayscale,
    double? BlackPoint = null,
    double? WhitePoint = null,
    double LowerPercentile = RenderImageRequest.DefaultLowerPercentile,
    double UpperPercentile = RenderImageRequest.DefaultUpperPercentile,
    double AsinhSoftening = RenderImageRequest.DefaultAsinhSoftening)
{
    private const double DefaultLowerPercentile = 1.0;
    private const double DefaultUpperPercentile = 99.0;
    private const double DefaultAsinhSoftening = 0.1;
}
