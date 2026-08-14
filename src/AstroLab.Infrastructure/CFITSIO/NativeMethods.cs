using System.Runtime.InteropServices;

namespace AstroLab.Infrastructure.CFITSIO;

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
/// bytes under Unix's LP64 model). These bindings marshal that type as a 64-bit <see cref="long"/>,
/// which is correct for LP64 (Linux/macOS) builds of cfitsio; an LLP64 (Windows) build of the
/// native library would require the corresponding parameters to be marshaled as <see cref="int"/> instead.
/// </para>
/// </remarks>
internal static partial class NativeMethods
{
    private const string LibraryName = "cfitsio";

    /// <summary>Opens an existing FITS file (<c>ffopen</c>, aliased as <c>fits_open_file</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffopen", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int OpenFile(out nint fptr, string filename, int iomode, out int status);

    /// <summary>Creates and opens a new FITS file (<c>ffinit</c>, aliased as <c>fits_create_file</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffinit", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int CreateFile(out nint fptr, string filename, out int status);

    /// <summary>Closes a FITS file, flushing any pending writes (<c>ffclos</c>, aliased as <c>fits_close_file</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffclos")]
    internal static partial int CloseFile(nint fptr, out int status);

    /// <summary>Translates a cfitsio status code into a short (≤30 char) human-readable message (<c>ffgerr</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffgerr")]
    internal static partial void GetErrorStatus(int status, Span<byte> errorText);

    /// <summary>Pops the most recent message from cfitsio's internal error stack (<c>ffgmsg</c>). Returns 0 when the stack is empty.</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffgmsg")]
    internal static partial int PopErrorMessage(Span<byte> errorMessage);

    /// <summary>Clears cfitsio's internal error message stack (<c>ffcmsg</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffcmsg")]
    internal static partial void ClearErrorMessages();

    /// <summary>Moves to an absolute HDU number, 1-based (<c>ffmahd</c>, aliased as <c>fits_movabs_hdu</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffmahd")]
    internal static partial int MoveToAbsoluteHdu(nint fptr, int hduNumber, out int hduType, out int status);

    /// <summary>Returns the total number of HDUs in the file (<c>ffthdu</c>, aliased as <c>fits_get_num_hdus</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffthdu")]
    internal static partial int GetNumberOfHdus(nint fptr, out int numberOfHdus, out int status);

    /// <summary>
    /// Reads the image dimensionality/shape of the current HDU (<c>ffgipr</c>, aliased as
    /// <c>fits_get_img_param</c>). <paramref name="naxes"/> must have at least <paramref name="maxDimensions"/> elements.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "ffgipr")]
    internal static partial int GetImageParameters(
        nint fptr, int maxDimensions, out int bitpix, out int naxis, [Out] long[] naxes, out int status);

    /// <summary>Returns the number of existing header keywords and free slots (<c>ffghsp</c>, aliased as <c>fits_get_hdrspace</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffghsp")]
    internal static partial int GetHeaderSpace(nint fptr, out int existingKeywords, out int moreKeywordsAllowed, out int status);

    /// <summary>Reads header record number <paramref name="recordNumber"/> (1-based) as an 80-column card (<c>ffgrec</c>, aliased as <c>fits_read_record</c>).</summary>
    [LibraryImport(LibraryName, EntryPoint = "ffgrec")]
    internal static partial int ReadHeaderRecord(nint fptr, int recordNumber, Span<byte> card, out int status);

    /// <summary>
    /// Reads a contiguous run of pixels from the current image HDU, starting at 1-based pixel
    /// coordinates <paramref name="firstPixel"/>, converting them to <paramref name="dataType"/>
    /// (<c>ffgpxv</c>, aliased as <c>fits_read_pix</c>). <paramref name="anyNull"/> is set to
    /// non-zero if any undefined pixels were encountered.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "ffgpxv")]
    internal static partial int ReadPixels(
        nint fptr,
        int dataType,
        long[] firstPixel,
        long numberOfElements,
        nint nullValue,
        nint outputArray,
        out int anyNull,
        out int status);
}
