using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// Classifies a staged FITS file's scientific data type from its already-parsed HDU metadata, and
/// gates an analysis operation on the classification matching what that analysis expects. This is
/// the "Identify Data Type" + validation step of the FITS processing pipeline (see spec.md §5.4):
/// every analysis entry point calls <see cref="EnsureKind"/> before touching pixel data, so running
/// the wrong kind of analysis against a file surfaces as a normal <see cref="Result{TValue}"/>
/// failure rather than nonsensical output.
/// </summary>
public static class FitsDatasetClassifier
{
    private const string TimeColumnName = "TIME";
    private const string FluxColumnName = "FLUX";
    private const string TotalFieldsKeyword = "TFIELDS";
    private const string DispersionAxisKeyword = "DISPAXIS";
    private const int FirstFieldNumber = 1;
    private const int NoFields = 0;
    private const int SingleAxisDimension = 1;
    private const int MinimumTimeSeriesFieldCount = 2;

    private static readonly string[] SpectralCTypePrefixes = ["WAVE", "FREQ", "ENER", "AWAV", "VELO"];

    public static FitsDatasetKind Classify(IReadOnlyList<HduDescriptor> hdus)
    {
        var imageHdu = FindFirstImageHdu(hdus);

        if (imageHdu is { } descriptor)
        {
            return IsSpectrum(descriptor) ? FitsDatasetKind.Spectrum : FitsDatasetKind.Image;
        }

        if (HasTimeSeriesData(hdus))
        {
            return FitsDatasetKind.TimeSeries;
        }

        return HasTable(hdus) ? FitsDatasetKind.Table : FitsDatasetKind.Unknown;
    }
    
    public static bool MatchesKind(HduDescriptor hdu, FitsDatasetKind capability) => capability switch
    {
        FitsDatasetKind.Spectrum => HasPixelData(hdu) && IsSpectrum(hdu),
        FitsDatasetKind.Image => HasPixelData(hdu) && !IsSpectrum(hdu),
        FitsDatasetKind.TimeSeries => IsTimeSeriesTable(hdu),
        FitsDatasetKind.Table => IsTable(hdu),
        _ => false
    };

    public static Result<FitsDatasetKind> EnsureKind(IReadOnlyList<HduDescriptor> hdus, FitsDatasetKind required)
    {
        if (HasCapability(hdus, required))
        {
            return Result<FitsDatasetKind>.Success(required);
        }

        var actual = Classify(hdus);

        return Error.Validation(
            "fits.data.unsupported_type",
            $"This FITS file was identified as {actual}, but this analysis requires {required} data.");
    }
    
    private static bool HasPixelData(HduDescriptor hdu) => hdu.Image is { PixelCount: > 0 };

    private static bool HasCapability(IReadOnlyList<HduDescriptor> hdus, FitsDatasetKind capability) => capability switch
    {
        FitsDatasetKind.Image or FitsDatasetKind.Spectrum => FindMatchingHdu(hdus, capability) is not null,
        FitsDatasetKind.TimeSeries => HasTimeSeriesData(hdus),
        FitsDatasetKind.Table => HasTable(hdus),
        _ => false
    };
    
    private static HduDescriptor? FindMatchingHdu(IReadOnlyList<HduDescriptor> hdus, FitsDatasetKind capability)
    {
        foreach (var hdu in hdus)
        {
            if (MatchesKind(hdu, capability))
            {
                return hdu;
            }
        }

        return null;
    }

    private static bool HasTimeSeriesData(IReadOnlyList<HduDescriptor> hdus)
    {
        foreach (var hdu in hdus)
        {
            if (IsTimeSeriesTable(hdu))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTimeSeriesTable(HduDescriptor hdu)
    {
        if (hdu.Type is not (HduType.AsciiTable or HduType.BinaryTable))
        {
            return false;
        }

        var fieldCount = hdu.Header.GetInteger(TotalFieldsKeyword).GetValueOrDefault(NoFields);

        if (fieldCount < MinimumTimeSeriesFieldCount)
        {
            return false;
        }

        return HasColumn(hdu.Header, fieldCount, TimeColumnName) && HasColumn(hdu.Header, fieldCount, FluxColumnName);
    }

    private static bool HasColumn(FitsHeader header, long fieldCount, string columnName)
    {
        for (var field = FirstFieldNumber; field <= fieldCount; field++)
        {
            var nameResult = header.GetString($"TTYPE{field}");

            if (nameResult.IsSuccess && string.Equals(nameResult.Value.Trim(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSpectrum(HduDescriptor hdu)
    {
        if (hdu.Image!.Value.NAxes.Length == SingleAxisDimension)
        {
            return true;
        }

        if (hdu.Header.Get(DispersionAxisKeyword).IsSuccess)
        {
            return true;
        }

        var axisCount = hdu.Image?.NAxes.Length ?? NoFields;

        for (var axis = FirstFieldNumber; axis <= axisCount; axis++)
        {
            var ctypeResult = hdu.Header.GetString($"CTYPE{axis}");

            if (ctypeResult.IsFailure)
            {
                continue;
            }

            var value = ctypeResult.Value.Trim();

            if (SpectralCTypePrefixes.Any(spectralCTypePrefix => 
                    value.StartsWith(spectralCTypePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static HduDescriptor? FindFirstImageHdu(IReadOnlyList<HduDescriptor> hdus)
    {
        foreach (var hdu in hdus)
        {
            if (HasPixelData(hdu))
            {
                return hdu;
            }
        }

        return null;
    }

    private static bool HasTable(IReadOnlyList<HduDescriptor> hdus)
    {
        foreach (var hdu in hdus)
        {
            if (IsTable(hdu))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTable(HduDescriptor hdu) => hdu.Type is HduType.AsciiTable or HduType.BinaryTable;
}
