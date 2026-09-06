using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Fits;

/// <summary>
/// Owns a cfitsio <c>fitsfile*</c> obtained via <see cref="NativeMethods.OpenFile"/>. This is the
/// sole owner of the handle it wraps: there is no copy constructor or sharing mechanism, so
/// ownership is always unambiguous, mirroring <see cref="UnmanagedFitsBuffer"/>'s disposal
/// guarantees for the other kind of native resource FITS reading relies on.
/// </summary>
/// <remarks>
/// Disposal is deterministic via <see cref="IDisposable"/> and idempotent — calling
/// <see cref="Dispose"/> more than once is safe. A finalizer acts as a last-resort safety net for
/// callers that forget to dispose, but code should always dispose explicitly (ideally via
/// <c>using</c>) so the underlying file descriptor cfitsio holds is released promptly.
/// </remarks>
public sealed class FitsFileHandle : IDisposable
{
    private nint _pointer;
    private int _disposed;

    private FitsFileHandle(nint pointer)
    {
        _pointer = pointer;
    }

    internal nint Pointer
    {
        get
        {
            ThrowIfDisposed();

            return _pointer;
        }
    }

    public static Result<FitsFileHandle> Open(string path)
    {
        _ = NativeMethods.OpenFile(out var fptr, path, (int)CfitsIoMode.ReadOnly, out var status);

        if (status != 0)
        {
            return CfitsIoErrorMapper.ToError("fits.cfitsio.open_failed", status);
        }

        return new FitsFileHandle(fptr);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(FitsFileHandle));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        NativeMethods.CloseFile(_pointer, out _);

        GC.SuppressFinalize(this);
    }

    ~FitsFileHandle()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeMethods.CloseFile(_pointer, out _);
        }
    }
}
