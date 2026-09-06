using System.Globalization;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

public readonly record struct TimeSeriesTableDescriptor
{
    private const string TotalFieldsKeyword = "TFIELDS";
    private const string RowCountKeyword = "NAXIS2";
    private const string TimeColumnName = "TIME";
    private const string FluxColumnName = "FLUX";
    private const int FirstFieldNumber = 1;
    private const int ScalarRepeatCount = 1;

    private TimeSeriesTableDescriptor(long rowCount, int timeColumnNumber, int fluxColumnNumber)
    {
        RowCount = rowCount;
        TimeColumnNumber = timeColumnNumber;
        FluxColumnNumber = fluxColumnNumber;
    }

    public long RowCount { get; }

    public int TimeColumnNumber { get; }

    public int FluxColumnNumber { get; }

    public static Result<TimeSeriesTableDescriptor> Resolve(HduDescriptor hdu)
    {
        if (hdu.Type is not (HduType.AsciiTable or HduType.BinaryTable))
        {
            return Error.Validation(
                "fits.data.not_a_table", $"HDU {hdu.Index} is a {hdu.Type} HDU, not a table.");
        }

        var fieldCountResult = hdu.Header.GetInteger(TotalFieldsKeyword);

        if (fieldCountResult.IsFailure)
        {
            return Result<TimeSeriesTableDescriptor>.Failure(fieldCountResult.Error);
        }

        var rowCountResult = hdu.Header.GetInteger(RowCountKeyword);

        if (rowCountResult.IsFailure)
        {
            return Result<TimeSeriesTableDescriptor>.Failure(rowCountResult.Error);
        }

        if (rowCountResult.Value < 0)
        {
            return Error.Validation(
                "fits.header.invalid_naxis", $"NAXIS2 must be non-negative, was {rowCountResult.Value}.");
        }

        var timeColumnResult = FindColumn(hdu.Header, (int)fieldCountResult.Value, TimeColumnName);

        if (timeColumnResult.IsFailure)
        {
            return Result<TimeSeriesTableDescriptor>.Failure(timeColumnResult.Error);
        }

        var fluxColumnResult = FindColumn(hdu.Header, (int)fieldCountResult.Value, FluxColumnName);

        if (fluxColumnResult.IsFailure)
        {
            return Result<TimeSeriesTableDescriptor>.Failure(fluxColumnResult.Error);
        }

        return new TimeSeriesTableDescriptor(rowCountResult.Value, timeColumnResult.Value, fluxColumnResult.Value);
    }

    private static Result<int> FindColumn(FitsHeader header, int fieldCount, string columnName)
    {
        for (var field = FirstFieldNumber; field <= fieldCount; field++)
        {
            var nameResult = header.GetString($"TTYPE{field}");

            if (!nameResult.IsSuccess || !string.Equals(nameResult.Value.Trim(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var shapeResult = EnsureScalarColumn(header, field, columnName);

            return shapeResult.IsFailure ? Result<int>.Failure(shapeResult.Error) : field;
        }

        return Error.Validation(
            "fits.data.missing_column", $"Table does not contain a '{columnName}' column (TTYPEn).");
    }

    private static Result<Unit> EnsureScalarColumn(FitsHeader header, int field, string columnName)
    {
        var formResult = header.GetString($"TFORM{field}");

        if (formResult.IsFailure)
        {
            return Error.Validation(
                "fits.header.keyword_missing", $"Column '{columnName}' is missing its TFORM{field} keyword.");
        }

        var repeatResult = ParseFixedRepeatCount(formResult.Value.Trim());

        if (repeatResult.IsFailure)
        {
            return Result<Unit>.Failure(repeatResult.Error);
        }

        if (repeatResult.Value != ScalarRepeatCount)
        {
            return Error.Validation(
                "fits.data.unsupported_column_shape",
                $"Column '{columnName}' has repeat count {repeatResult.Value}; only scalar (repeat = 1) columns are supported.");
        }

        return Result<Unit>.Success(Unit.Value);
    }
    
    private static Result<int> ParseFixedRepeatCount(string tform)
    {
        if (tform.Length == 0)
        {
            return Error.Validation("fits.header.invalid_tform", "TFORM value is empty.");
        }

        var digitCount = 0;

        while (digitCount < tform.Length && char.IsAsciiDigit(tform[digitCount]))
        {
            digitCount++;
        }

        if (digitCount >= tform.Length)
        {
            return Error.Validation("fits.header.invalid_tform", $"TFORM value '{tform}' has no type code.");
        }

        var typeCode = char.ToUpperInvariant(tform[digitCount]);

        if (typeCode is 'P' or 'Q')
        {
            return Error.Validation(
                "fits.data.unsupported_column_shape",
                $"TFORM '{tform}' is a variable-length array column, which is not yet supported.");
        }

        if (typeCode == 'A' || digitCount == 0)
        {
            return ScalarRepeatCount;
        }

        return int.Parse(tform.AsSpan(0, digitCount), NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
