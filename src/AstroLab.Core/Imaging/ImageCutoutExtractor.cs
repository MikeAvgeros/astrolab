using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Pure sub-region extraction from a 2D pixel buffer: copies a caller-specified rectangular window
/// out of a full image's pixel data, row by row. Purely a data-selection operation — no scientific
/// transformation is applied to the copied values.
/// </summary>
public static class ImageCutoutExtractor
{
    public static Result<Unit> ExtractRegion(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        int x,
        int y,
        int cutoutWidth,
        int cutoutHeight,
        Span<float> destination)
    {
        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            return Error.Validation(
                "imaging.cutout.invalid_image_bounds",
                $"Pixel span length ({pixels.Length}) does not match width x height ({width}x{height}).");
        }

        if (cutoutWidth <= 0 || cutoutHeight <= 0)
        {
            return Error.Validation("imaging.cutout.invalid_region_size", "cutoutWidth and cutoutHeight must be positive.");
        }

        if (destination.Length != cutoutWidth * cutoutHeight)
        {
            return Error.Validation(
                "imaging.cutout.destination_length_mismatch",
                $"destination length ({destination.Length}) must equal cutoutWidth x cutoutHeight ({cutoutWidth}x{cutoutHeight}).");
        }

        if (x < 0 || y < 0 || x + cutoutWidth > width || y + cutoutHeight > height)
        {
            return Error.Validation(
                "imaging.cutout.out_of_bounds",
                $"Requested region (x={x}, y={y}, width={cutoutWidth}, height={cutoutHeight}) lies outside the source image ({width}x{height}).");
        }

        for (var row = 0; row < cutoutHeight; row++)
        {
            var sourceOffset = (y + row) * width + x;

            var destinationOffset = row * cutoutWidth;

            pixels.Slice(sourceOffset, cutoutWidth).CopyTo(destination.Slice(destinationOffset, cutoutWidth));
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
