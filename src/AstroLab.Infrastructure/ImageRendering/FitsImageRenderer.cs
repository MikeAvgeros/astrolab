using AstroLab.Core.Imaging;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Orchestrates the FITS-pixel-array-to-browser-image pipeline: optional auto black/white-point
/// detection, non-linear stretch, color mapping, and PNG encoding. All scientific computation is
/// delegated to <c>AstroLab.Core.Imaging</c>; this class only sequences those pure calls, which is
/// why it lives in the imperative shell rather than Core. It carries no instance state, so every
/// member is static.
/// </summary>
public static class FitsImageRenderer
{
    private const double DefaultBlackPoint = 0.0;
    private const double DefaultWhitePoint = 1.0;
    private const int RgbChannelCount = 3;

    /// <summary>Applies stretch and color mapping, producing an in-memory RGB image without modifying the source pixels.</summary>
    public static Result<RenderedImage> Render(ReadOnlySpan<float> pixels, int width, int height, RenderOptions options)
    {
        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            return Error.Validation(
                "rendering.invalid_image_bounds",
                $"Pixel span length ({pixels.Length}) does not match width x height ({width}x{height}).");
        }

        var workingPixels = pixels;

        var workingWidth = width;

        var workingHeight = height;

        float[]? downsampleBuffer = null;

        if (options.MaxDimension is int maxDimension)
        {
            var factorResult = ImageDownsampler.ComputeFactor(width, height, maxDimension);

            if (factorResult.IsFailure)
            {
                return Result<RenderedImage>.Failure(factorResult.Error);
            }

            var factor = factorResult.Value;

            if (factor > 1)
            {
                var (downsampledWidth, downsampledHeight) = ImageDownsampler.ComputeDownsampledDimensions(width, height, factor);

                downsampleBuffer = new float[downsampledWidth * downsampledHeight];

                var downsampleResult = ImageDownsampler.Downsample(pixels, width, height, factor, downsampleBuffer);

                if (downsampleResult.IsFailure)
                {
                    return Result<RenderedImage>.Failure(downsampleResult.Error);
                }

                workingPixels = downsampleBuffer;

                workingWidth = downsampledWidth;

                workingHeight = downsampledHeight;
            }
        }

        var blackPoint = options.BlackPoint ?? DefaultBlackPoint;

        var whitePoint = options.WhitePoint ?? DefaultWhitePoint;

        if (options.RequiresAutoScale)
        {
            var boundsResult = ImageStatistics.ComputePercentileBounds(workingPixels, options.AutoLowerPercentile, options.AutoUpperPercentile);

            if (boundsResult.IsFailure)
            {
                return Result<RenderedImage>.Failure(boundsResult.Error);
            }

            blackPoint = options.BlackPoint ?? boundsResult.Value.Lower;

            whitePoint = options.WhitePoint ?? boundsResult.Value.Upper;
        }

        var scaleParameters = ScaleParameters.Create(blackPoint, whitePoint, options.Stretch, options.AsinhSoftening);

        var grayscale = new byte[workingPixels.Length];

        var stretchResult = ImageScaler.Stretch(workingPixels, grayscale, scaleParameters);

        if (stretchResult.IsFailure)
        {
            return Result<RenderedImage>.Failure(stretchResult.Error);
        }

        var rgb = new byte[workingPixels.Length * RgbChannelCount];

        var colorResult = ColorMapper.Apply(grayscale, rgb, options.ColorMap);

        if (colorResult.IsFailure)
        {
            return Result<RenderedImage>.Failure(colorResult.Error);
        }

        return RenderedImage.Create(workingWidth, workingHeight, rgb);
    }

    /// <summary>Renders and PNG-encodes a pixel array in one step.</summary>
    public static Result<byte[]> RenderToPng(ReadOnlySpan<float> pixels, int width, int height, RenderOptions options) =>
        Render(pixels, width, height, options).Map(PngRenderer.Encode);
}
