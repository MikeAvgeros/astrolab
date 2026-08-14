using System.Buffers.Binary;
using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Converts raw, on-disk FITS pixel bytes — always big-endian, per the FITS standard, regardless
/// of host architecture — into host-endian physical <see cref="float"/> values by applying the
/// HDU's <c>BZERO</c>/<c>BSCALE</c> linear transform (via <see cref="FitsImageDescriptor.ToPhysical"/>,
/// a pure Core computation). This is an encoding/format concern, not a scientific one, which is
/// why the byte-order handling itself lives in Infrastructure rather than Core.
/// </summary>
public static class FitsPixelConverter
{
    public static float[] ToFloatArray(ReadOnlySpan<byte> raw, FitsImageDescriptor descriptor)
    {
        var count = checked((int)descriptor.PixelCount);
        var bytesPerPixel = descriptor.BitPix.BytesPerPixel();
        if (raw.Length != count * bytesPerPixel)
        {
            throw new ArgumentException(
                $"Raw buffer length ({raw.Length}) does not match expected pixel data size ({count * bytesPerPixel}).",
                nameof(raw));
        }

        var result = new float[count];
        for (var i = 0; i < count; i++)
        {
            var pixelBytes = raw.Slice(i * bytesPerPixel, bytesPerPixel);
            double rawValue = descriptor.BitPix switch
            {
                BitPixType.Byte => pixelBytes[0],
                BitPixType.Int16 => BinaryPrimitives.ReadInt16BigEndian(pixelBytes),
                BitPixType.Int32 => BinaryPrimitives.ReadInt32BigEndian(pixelBytes),
                BitPixType.Int64 => BinaryPrimitives.ReadInt64BigEndian(pixelBytes),
                BitPixType.Float32 => BinaryPrimitives.ReadSingleBigEndian(pixelBytes),
                BitPixType.Float64 => BinaryPrimitives.ReadDoubleBigEndian(pixelBytes),
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor), $"Unsupported BITPIX value: {descriptor.BitPix}"),
            };

            result[i] = (float)descriptor.ToPhysical(rawValue);
        }

        return result;
    }
}
