using System.Runtime.InteropServices;
using System.Threading;

namespace AstroLab.Infrastructure.CFITSIO;

/// <summary>
/// Owns a fixed-size, off-GC-heap buffer of raw FITS pixel bytes, allocated via
/// <see cref="NativeMemory"/>. This is the sole owner of the native allocation it wraps: there is
/// no copy constructor or sharing mechanism, so ownership is always unambiguous.
/// </summary>
/// <remarks>
/// Disposal is deterministic via <see cref="IDisposable"/> and idempotent — calling
/// <see cref="Dispose"/> more than once is safe and will never double-free. A finalizer acts as a
/// last-resort safety net for callers that forget to dispose, but code should always dispose
/// explicitly (ideally via <c>using</c>) since native pixel buffers can be gigabytes in size.
/// </remarks>
public sealed unsafe class UnmanagedFitsBuffer : IDisposable
{
    private byte* _pointer;
    private int _disposed;

    private UnmanagedFitsBuffer(byte* pointer, nuint lengthBytes)
    {
        _pointer = pointer;
        LengthBytes = lengthBytes;
    }

    /// <summary>The size of the allocation, in bytes.</summary>
    public nuint LengthBytes { get; }

    /// <summary>Allocates a new zero-initialized native buffer of the requested size.</summary>
    public static UnmanagedFitsBuffer Allocate(nuint lengthBytes)
    {
        if (lengthBytes == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthBytes), "Buffer length must be greater than zero.");
        }

        byte* pointer;
        try
        {
            pointer = (byte*)NativeMemory.AllocZeroed(lengthBytes);
        }
        catch (OutOfMemoryException ex)
        {
            throw new InvalidOperationException(
                $"Failed to allocate {lengthBytes:N0} bytes of native memory for a FITS pixel buffer.", ex);
        }

        return new UnmanagedFitsBuffer(pointer, lengthBytes);
    }

    /// <summary>Copies <paramref name="source"/> into the buffer at <paramref name="destinationOffset"/> bytes, without ever materializing the full buffer as a managed array.</summary>
    public void CopyFrom(ReadOnlySpan<byte> source, nuint destinationOffset = 0)
    {
        ThrowIfDisposed();
        if (destinationOffset > LengthBytes || (ulong)source.Length > LengthBytes - destinationOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source), "Source span does not fit within the buffer at the given offset.");
        }

        source.CopyTo(new Span<byte>(_pointer + destinationOffset, source.Length));
    }

    /// <summary>A span over the entire buffer. Only valid for buffers up to <see cref="int.MaxValue"/> bytes.</summary>
    public Span<byte> AsSpan()
    {
        ThrowIfDisposed();
        if (LengthBytes > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Buffer of {LengthBytes:N0} bytes exceeds Span<T>'s 2 GiB addressable length; use AsSpan(offset, length) to access it in slices.");
        }

        return new Span<byte>(_pointer, (int)LengthBytes);
    }

    /// <summary>A span over a byte-addressed slice of the buffer, for buffers too large to view in a single <see cref="Span{T}"/>.</summary>
    public Span<byte> AsSpan(nuint offset, int length)
    {
        ThrowIfDisposed();
        if (length < 0 || offset > LengthBytes || (ulong)length > LengthBytes - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Requested slice is out of bounds.");
        }

        return new Span<byte>(_pointer + offset, length);
    }

    /// <summary>Reinterprets the buffer as 32-bit floating-point pixels. <see cref="LengthBytes"/> must be a multiple of <c>sizeof(float)</c>.</summary>
    public ReadOnlySpan<float> AsFloatSpan()
    {
        ThrowIfDisposed();
        if (LengthBytes % sizeof(float) != 0)
        {
            throw new InvalidOperationException($"Buffer length {LengthBytes:N0} is not a multiple of sizeof(float).");
        }

        if (LengthBytes / sizeof(float) > int.MaxValue)
        {
            throw new InvalidOperationException("Buffer is too large to view as a single Span<float>.");
        }

        return new ReadOnlySpan<float>(_pointer, (int)(LengthBytes / sizeof(float)));
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(UnmanagedFitsBuffer));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        NativeMemory.Free(_pointer);
        _pointer = null;
        GC.SuppressFinalize(this);
    }

    ~UnmanagedFitsBuffer()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeMemory.Free(_pointer);
        }
    }
}
