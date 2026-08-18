using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// Classifies a staged FITS file's scientific data type from its already-parsed HDU metadata, and
/// gates an analysis operation on the classification matching what that analysis expects. This is
/// the "Identify Data Type" + validation step of the FITS processing pipeline (see spec.md §4.4):
/// every analysis entry point calls <see cref="EnsureKind"/> before touching pixel data, so running
/// the wrong kind of analysis against a file surfaces as a normal <see cref="Result{TValue}"/>
/// failure rather than nonsensical output.
/// </summary>
public static class FitsDatasetClassifier
{
    private const string TimeColumnName = "TIME";
    private const string TotalFieldsKeyword = "TFIELDS";
    private const string DispersionAxisKeyword = "DISPAXIS";
    private const int FirstFieldNumber = 1;
    private const int NoFields = 0;
    private const int SingleAxisDimension = 1;

    private static readonly string[] SpectralCTypePrefixes = ["WAVE", "FREQ", "ENER", "AWAV", "VELO"];

    /// <summary>Classifies a file from the full ordered list of its HDU descriptors.</summary>
    public static FitsDatasetKind Classify(IReadOnlyList<HduDescriptor> hdus)
    {
        if (HasTimeColumn(hdus))
        {
            return FitsDatasetKind.TimeSeries;
        }

        var imageHdu = FindFirstImageHdu(hdus);
        if (imageHdu is { } descriptor)
        {
            return IsSpectrum(descriptor) ? FitsDatasetKind.Spectrum : FitsDatasetKind.Image;
        }

        return HasTable(hdus) ? FitsDatasetKind.Table : FitsDatasetKind.Unknown;
    }

    /// <summary>
    /// Whether <paramref name="hdu"/> carries non-empty pixel data — the same "is this a candidate
    /// data HDU" predicate <see cref="Classify"/> uses internally, exposed so
    /// <c>AstroLab.Infrastructure.Storage.FitsDatasetReader</c> can locate the exact same HDU it
    /// classifies rather than re-deriving the rule independently (and risking the two diverging).
    /// </summary>
    public static bool HasPixelData(HduDescriptor hdu) => hdu.Image is { PixelCount: > 0 };

    /// <summary>
    /// Classifies <paramref name="hdus"/> and succeeds only when the result matches
    /// <paramref name="required"/>; otherwise fails with a validation <see cref="Error"/> naming
    /// both the required and the actual kind.
    /// </summary>
    public static Result<FitsDatasetKind> EnsureKind(IReadOnlyList<HduDescriptor> hdus, FitsDatasetKind required)
    {
        var actual = Classify(hdus);
        return actual == required
            ? Result<FitsDatasetKind>.Success(actual)
            : Error.Validation(
                "fits.data.unsupported_type",
                $"This FITS file was identified as {actual}, but this analysis requires {required} data.");
    }

    private static bool HasTimeColumn(IReadOnlyList<HduDescriptor> hdus)
    {
        for (var i = 0; i < hdus.Count; i++)
        {
            var hdu = hdus[i];
            if (hdu.Type is not (HduType.AsciiTable or HduType.BinaryTable))
            {
                continue;
            }

            var fieldCount = hdu.Header.GetInteger(TotalFieldsKeyword).GetValueOrDefault(NoFields);
            for (var field = FirstFieldNumber; field <= fieldCount; field++)
            {
                var nameResult = hdu.Header.GetString($"TTYPE{field}");
                if (nameResult.IsSuccess && string.Equals(nameResult.Value.Trim(), TimeColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="hdu"/> — the specific HDU a caller would actually load pixel data
    /// from — is itself marked as spectral, either by dimensionality (a bare 1D array) or by its
    /// own <c>DISPAXIS</c>/<c>CTYPEn</c> header cards. Deliberately scoped to this one HDU rather
    /// than scanning every HDU in the file: an unrelated extension's spectral markers must never
    /// reclassify a *different* HDU's plain image data as a spectrum (see spec.md §4.4).
    /// </summary>
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
            for (var prefixIndex = 0; prefixIndex < SpectralCTypePrefixes.Length; prefixIndex++)
            {
                if (value.StartsWith(SpectralCTypePrefixes[prefixIndex], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static HduDescriptor? FindFirstImageHdu(IReadOnlyList<HduDescriptor> hdus)
    {
        for (var i = 0; i < hdus.Count; i++)
        {
            if (HasPixelData(hdus[i]))
            {
                return hdus[i];
            }
        }

        return null;
    }

    private static bool HasTable(IReadOnlyList<HduDescriptor> hdus)
    {
        for (var i = 0; i < hdus.Count; i++)
        {
            if (hdus[i].Type is HduType.AsciiTable or HduType.BinaryTable)
            {
                return true;
            }
        }

        return false;
    }
}
