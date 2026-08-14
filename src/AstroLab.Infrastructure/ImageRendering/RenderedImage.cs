namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>An in-memory, interleaved 8-bit RGB image produced by <see cref="FitsImageRenderer"/>.</summary>
/// <param name="Width">Image width, in pixels.</param>
/// <param name="Height">Image height, in pixels.</param>
/// <param name="Rgb">Row-major pixel data, 3 bytes (R, G, B) per pixel; length must equal <c>Width * Height * 3</c>.</param>
public readonly record struct RenderedImage(int Width, int Height, byte[] Rgb);
