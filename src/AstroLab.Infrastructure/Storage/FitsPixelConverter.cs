using System.Buffers.Binary;
using System.Runtime.InteropServices;
using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Converts raw, on-disk FITS pixel bytes — always big-endian, per the FITS standard, regardless
/// of host architecture — into host-endian physical <see cref="float"/> values by applying the
/// HDU's <c>BZERO</c>/<c>BSCALE</c> linear transform (via <see cref="FitsImageDescriptor.ToPhysical"/>,
/// a pure Core computation). This is an encoding/format concern, not a scientific one, which is
/// why the byte-order handling itself lives in Infrastructure rather than Core. The result never
/// touches the managed heap, so a multi-gigabyte image never doubles into a managed array
/// alongside its native source buffer; the caller owns the returned buffer and must dispose it.
/// </summary>
public static class FitsPixelConverter
{
    private const int BytesPerFloat = sizeof(float);

    public static UnmanagedFitsBuffer ToFloatBuffer(ReadOnlySpan<byte> raw, FitsImageDescriptor descriptor)
    {
        var count = checked((int)descriptor.PixelCount);

        var bytesPerPixel = descriptor.BitPix.BytesPerPixel();

        if (raw.Length != count * bytesPerPixel)
        {
            throw new ArgumentException(
                $"Raw buffer length ({raw.Length}) does not match expected pixel data size ({count * bytesPerPixel}).",
                nameof(raw));
        }

        var destination = UnmanagedFitsBuffer.Allocate((nuint)checked(count * BytesPerFloat));

        try
        {
            var result = MemoryMarshal.Cast<byte, float>(destination.AsSpan());

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

            return destination;
        }
        catch
        {
            destination.Dispose();

            throw;
        }
    }
}
