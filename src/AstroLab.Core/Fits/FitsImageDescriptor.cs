using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

public readonly record struct FitsImageDescriptor
{
    private const double DefaultBZero = 0.0;
    private const double DefaultBScale = 1.0;

    private FitsImageDescriptor(BitPixType bitPix, ImmutableArray<int> nAxes, double bZero, double bScale, long? blank)
    {
        BitPix = bitPix;
        NAxes = nAxes;
        BZero = bZero;
        BScale = bScale;
        Blank = blank;
    }

    public BitPixType BitPix { get; }

    public ImmutableArray<int> NAxes { get; }

    public double BZero { get; }

    public double BScale { get; }

    public long? Blank { get; }

    public long PixelCount => NAxes.IsDefaultOrEmpty ? 0 : NAxes.Aggregate(1L, (acc, n) => acc * n);

    public long DataSizeBytes => BitPix.BytesPerPixel() * PixelCount;

    public double ToPhysical(double rawValue) => rawValue * BScale + BZero;

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
                    var bzero = header.GetReal("BZERO").GetValueOrDefault(DefaultBZero);

                    var bscale = header.GetReal("BSCALE").GetValueOrDefault(DefaultBScale);

                    var blankResult = header.GetInteger("BLANK");

                    var blank = blankResult.IsSuccess ? blankResult.Value : (long?)null;

                    return Create(bitpix, axes, bzero, bscale, blank);

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

    public static FitsImageDescriptor Create(BitPixType bitPix, ImmutableArray<int> nAxes, double bZero, double bScale, long? blank) =>
        new(bitPix, nAxes, bZero, bScale, blank);
}
