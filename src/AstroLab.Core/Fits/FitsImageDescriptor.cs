using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// The pixel-array shape and physical-scaling metadata for an HDU, derived purely from its
/// <see cref="FitsHeader"/> (<c>BITPIX</c>, <c>NAXISn</c>, <c>BZERO</c>, <c>BSCALE</c>, <c>BLANK</c>).
/// </summary>
public readonly record struct FitsImageDescriptor(
    BitPixType BitPix,
    ImmutableArray<int> NAxes,
    double BZero,
    double BScale,
    long? Blank)
{
    /// <summary>The total number of pixels across all axes, or 0 when the HDU carries no data (<c>NAXIS</c> = 0).</summary>
    public long PixelCount => NAxes.IsDefaultOrEmpty ? 0 : NAxes.Aggregate(1L, (acc, n) => acc * n);

    /// <summary>The size, in bytes, of the raw pixel array as stored on disk.</summary>
    public long DataSizeBytes => BitPix.BytesPerPixel() * PixelCount;

    /// <summary>Applies the <c>BZERO</c>/<c>BSCALE</c> linear transform to convert a raw stored value to its physical value.</summary>
    public double ToPhysical(double rawValue) => rawValue * BScale + BZero;

    /// <summary>
    /// Interprets this descriptor's axes as a 2D raster (width, height) for algorithms that operate
    /// on flattened row-major pixel arrays: a 1D array is treated as a single-row image, and only
    /// the first two axes of a higher-dimensional cube are used.
    /// </summary>
    public (int Width, int Height) Resolve2DDimensions() => NAxes.Length switch
    {
        >= 2 => (NAxes[0], NAxes[1]),
        1 => (NAxes[0], 1),
        _ => (0, 0),
    };

    public static Result<FitsImageDescriptor> FromHeader(FitsHeader header) =>
        header.GetInteger("BITPIX")
            .Bind(ToBitpixType)
            .Bind(bitpix => header.GetInteger("NAXIS")
                .Bind(naxis => ReadAxes(header, (int)naxis))
                .Map(axes =>
                {
                    var bzero = header.GetReal("BZERO").GetValueOrDefault(0.0);
                    var bscale = header.GetReal("BSCALE").GetValueOrDefault(1.0);
                    var blankResult = header.GetInteger("BLANK");
                    var blank = blankResult.IsSuccess ? blankResult.Value : (long?)null;
                    return new FitsImageDescriptor(bitpix, axes, bzero, bscale, blank);
                }));

    private static Result<BitPixType> ToBitpixType(long value) =>
        Enum.IsDefined(typeof(BitPixType), (int)value)
            ? (BitPixType)(int)value
            : Error.Validation("fits.header.invalid_bitpix", $"BITPIX value {value} is not a valid FITS pixel representation.");

    private static Result<ImmutableArray<int>> ReadAxes(FitsHeader header, int naxis)
    {
        if (naxis < 0)
        {
            return Error.Validation("fits.header.invalid_naxis", $"NAXIS must be non-negative, was {naxis}.");
        }

        if (naxis == 0)
        {
            return Result<ImmutableArray<int>>.Success(ImmutableArray<int>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<int>(naxis);
        for (var i = 1; i <= naxis; i++)
        {
            var axisResult = header.GetInteger($"NAXIS{i}");
            if (axisResult.IsFailure)
            {
                return Result<ImmutableArray<int>>.Failure(axisResult.Error);
            }

            builder.Add((int)axisResult.Value);
        }

        return builder.MoveToImmutable();
    }
}
