using AstroLab.Core.Photometry;

namespace AstroLab.Api.Features.Images.Photometry;

public sealed record AperturePhotometryRequest(
    double CenterX,
    double CenterY,
    double ApertureRadius,
    double AnnulusInnerRadius,
    double AnnulusOuterRadius,
    BackgroundEstimationMethod BackgroundMethod = BackgroundEstimationMethod.Median);
