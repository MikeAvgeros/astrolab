using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

public readonly record struct FitsImageDescriptor
{
    private const double DefaultBZero = 0.0;
    private const double DefaultBScale = 1.0;
    private const long MaxNaxis = 999;

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
            .Bind(ToBitPixType)
            .Bind(bitpix => header.GetInteger("NAXIS")
                .Bind(naxis => ReadAxes(header, naxis))
                .Bind(axes => ResolveOptionalReal(header, "BZERO", DefaultBZero)
                    .Bind(bzero => ResolveOptionalReal(header, "BSCALE", DefaultBScale)
                        .Bind(bscale => ResolveOptionalBlank(header)
                            .Map(blank => Create(bitpix, axes, bzero, bscale, blank))))));

    private static FitsImageDescriptor Create(BitPixType bitPix, ImmutableArray<int> nAxes, double bZero, double bScale, long? blank) =>
        new(bitPix, nAxes, bZero, bScale, blank);
    
    private static Result<double> ResolveOptionalReal(FitsHeader header, string keyword, double defaultValue)
    {
        var result = header.GetReal(keyword);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        return result.Error.Category == ErrorCategory.NotFound ? Result<double>.Success(defaultValue) : Result<double>.Failure(result.Error);
    }

    private static Result<long?> ResolveOptionalBlank(FitsHeader header)
    {
        var result = header.GetInteger("BLANK");

        if (result.IsSuccess)
        {
            return Result<long?>.Success(result.Value);
        }

        return result.Error.Category == ErrorCategory.NotFound ? Result<long?>.Success(null) : Result<long?>.Failure(result.Error);
    }

    private static Result<BitPixType> ToBitPixType(long value) =>
        value switch
        {
            8 or 16 or 32 or 64 or -32 or -64 => (BitPixType)(int)value,
            _ => Error.Validation("fits.header.invalid_bitpix", $"BITPIX value {value} is not a valid FITS pixel representation."),
        };

    private static Result<ImmutableArray<int>> ReadAxes(FitsHeader header, long naxis)
    {
        if (naxis is < 0 or > MaxNaxis)
        {
            return Error.Validation("fits.header.invalid_naxis", $"NAXIS must be between 0 and {MaxNaxis}, was {naxis}.");
        }

        if (naxis == 0)
        {
            return Result<ImmutableArray<int>>.Success(ImmutableArray<int>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<int>((int)naxis);

        var pixelCount = 1L;

        for (var i = 1; i <= naxis; i++)
        {
            var axisResult = header.GetInteger($"NAXIS{i}");

            if (axisResult.IsFailure)
            {
                return Result<ImmutableArray<int>>.Failure(axisResult.Error);
            }

            if (axisResult.Value is < 0 or > int.MaxValue)
            {
                return Error.Validation(
                    "fits.header.invalid_naxis",
                    $"NAXIS{i} must be between 0 and {int.MaxValue}, was {axisResult.Value}.");
            }

            var axisLength = (int)axisResult.Value;

            try
            {
                pixelCount = checked(pixelCount * axisLength);
            }
            catch (OverflowException)
            {
                return Error.Validation(
                    "fits.header.image_too_large",
                    "The product of the NAXISn values overflows the supported pixel count.");
            }

            builder.Add(axisLength);
        }

        return builder.MoveToImmutable();
    }
}
