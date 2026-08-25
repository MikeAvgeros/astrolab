using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// A fully-loaded primary-HDU image: its descriptor and physical (BZERO/BSCALE-applied) pixel
/// values. Owns the native buffer backing <see cref="Pixels"/>, so a caller MUST dispose it
/// (ideally via <c>using</c>) once it has finished reading pixel data.
/// </summary>
public sealed class FitsDataset : IDisposable
{
    private readonly UnmanagedFitsBuffer _pixelBuffer;

    public FitsDataset(HduDescriptor hdu, FitsImageDescriptor image, UnmanagedFitsBuffer pixelBuffer)
    {
        ArgumentNullException.ThrowIfNull(pixelBuffer);

        Hdu = hdu;

        Image = image;

        _pixelBuffer = pixelBuffer;
    }

    public HduDescriptor Hdu { get; }

    public FitsImageDescriptor Image { get; }

    /// <summary>The physical pixel values, viewed directly over native memory — never copied into a managed array.</summary>
    public ReadOnlySpan<float> Pixels => _pixelBuffer.AsFloatSpan();

    public void Dispose() => _pixelBuffer.Dispose();
}
