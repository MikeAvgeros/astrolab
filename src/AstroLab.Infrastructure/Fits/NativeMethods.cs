using System.Runtime.InteropServices;

namespace AstroLab.Infrastructure.Fits;

/// <summary>
/// Source-generated P/Invoke bindings for the subset of the <c>cfitsio</c> C API
/// (https://heasarc.gsfc.nasa.gov/fitsio/) required to open FITS files, navigate HDUs, read
/// headers, and read pixel data. Every entry point below targets the library's real exported
/// symbol name — the "friendly" names in <c>fitsio.h</c> (<c>fits_open_file</c>, <c>fits_close_file</c>,
/// ...) are C preprocessor macros, not link-time symbols, so binding to them directly is not possible.
/// </summary>
/// <remarks>
/// This class contains no logic beyond the native declarations themselves: callers are expected
/// to go through <see cref="UnmanagedFitsBuffer"/> and higher-level Infrastructure services rather
/// than invoking these methods directly. Per the architecture, native interop must never be
/// referenced from <c>AstroLab.Core</c>.
/// <para>
/// Portability note: cfitsio's C API represents axis lengths and pixel coordinates with the C
/// <c>long</c> type, whose width is platform-dependent (4 bytes under Windows' LLP64 model, 8
/// bytes under Unix's LP64 model). Parameters standing in for a native <c>long</c>/<c>long*</c> are
/// marshaled via <see cref="CLong"/> (and arrays of it), the BCL type designed specifically to
/// match the platform's actual C <c>long</c> width — never a bare <see cref="long"/>, which would
/// silently mismatch layout on an LLP64 (Windows) build of the native library.
/// </para>
/// <para>
/// HDU numbers, header record numbers, and pixel coordinates passed to these bindings are all
/// 1-based, matching cfitsio's own convention rather than .NET's.
/// </para>
/// </remarks>
internal static partial class NativeMethods
{
    private const string LibraryName = "cfitsio";

    [LibraryImport(LibraryName, EntryPoint = "ffopen", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int OpenFile(out nint fptr, string filename, int iomode, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffinit", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int CreateFile(out nint fptr, string filename, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffclos")]
    internal static partial int CloseFile(nint fptr, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffgerr")]
    internal static partial void GetErrorStatus(int status, Span<byte> errorText);

    [LibraryImport(LibraryName, EntryPoint = "ffgmsg")]
    internal static partial int PopErrorMessage(Span<byte> errorMessage);

    [LibraryImport(LibraryName, EntryPoint = "ffcmsg")]
    internal static partial void ClearErrorMessages();

    [LibraryImport(LibraryName, EntryPoint = "ffmahd")]
    internal static partial int MoveToAbsoluteHdu(nint fptr, int hduNumber, out int hduType, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffthdu")]
    internal static partial int GetNumberOfHdus(nint fptr, out int numberOfHdus, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffgipr")]
    internal static partial int GetImageParameters(
        nint fptr, int maxDimensions, out int bitpix, out int naxis, [Out] CLong[] naxes, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffghsp")]
    internal static partial int GetHeaderSpace(nint fptr, out int existingKeywords, out int moreKeywordsAllowed, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffgrec")]
    internal static partial int ReadHeaderRecord(nint fptr, int recordNumber, Span<byte> card, out int status);

    [LibraryImport(LibraryName, EntryPoint = "ffgpxv")]
    internal static partial int ReadPixels(
        nint fptr,
        int dataType,
        CLong[] firstPixel,
        CLong numberOfElements,
        nint nullValue,
        nint outputArray,
        out int anyNull,
        out int status);
}
