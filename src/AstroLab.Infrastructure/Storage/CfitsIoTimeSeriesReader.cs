using AstroLab.Core.Fits;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Reads a time-series table's TIME/FLUX columns through the native cfitsio C API rather than
/// parsing FITS bytes by hand: robustly decoding an arbitrary numeric column storage type
/// (byte/short/int/float/double) with <c>TSCAL</c>/<c>TZERO</c> applied is exactly the kind of
/// well-trodden, easy-to-get-subtly-wrong binary table logic cfitsio already solves, unlike the
/// fixed, simple image-pixel layout <see cref="FitsPixelDataReader"/> decodes directly. cfitsio
/// manages its own file I/O once given a path, so — unlike the rest of this codebase's FITS
/// readers — this one takes a real filesystem path rather than a stream positioned by the caller.
/// </summary>
public static class CfitsIoTimeSeriesReader
{
    private const double NullValueSubstitute = double.NaN;

    public static Task<Result<LightCurveTableData>> ReadAsync(
        string filePath, int hduNumber, TimeSeriesTableDescriptor descriptor, CancellationToken cancellationToken) =>
        Task.Run(() => Read(filePath, hduNumber, descriptor), cancellationToken);

    private static Result<LightCurveTableData> Read(string filePath, int hduNumber, TimeSeriesTableDescriptor descriptor)
    {
        var openResult = FitsFileHandle.Open(filePath);

        if (openResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(openResult.Error);
        }

        using var handle = openResult.Value;

        _ = NativeMethods.MoveToAbsoluteHdu(handle.Pointer, hduNumber, out _, out var moveStatus);

        if (moveStatus != 0)
        {
            return CfitsIoErrorMapper.ToError("fits.cfitsio.hdu_move_failed", moveStatus);
        }

        var rowCount = checked((int)descriptor.RowCount);

        var timeResult = ReadColumn(handle.Pointer, descriptor.TimeColumnNumber, rowCount);

        if (timeResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(timeResult.Error);
        }

        var fluxResult = ReadColumn(handle.Pointer, descriptor.FluxColumnNumber, rowCount);

        if (fluxResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(fluxResult.Error);
        }

        return LightCurveTableData.Create(timeResult.Value, fluxResult.Value);
    }

    private static Result<double[]> ReadColumn(nint fptr, int columnNumber, int rowCount)
    {
        var values = new double[rowCount];

        NativeMethods.ReadColumnDoubles(
            fptr, columnNumber, firstRow: 1, firstElement: 1, numberOfElements: rowCount,
            nullValue: NullValueSubstitute, values, out _, out var status);

        if (status != 0)
        {
            return CfitsIoErrorMapper.ToError("fits.cfitsio.column_read_failed", status);
        }

        return values;
    }
}
