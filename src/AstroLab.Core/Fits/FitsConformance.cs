using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// Pure FITS 4.0 conformance rules that go beyond what is needed to read a header, used to reject
/// input that is not a FITS file before it is accepted into staging storage.
/// </summary>
public static class FitsConformance
{
    private const string SimpleKeyword = "SIMPLE";
    
    public static Result<Unit> ValidatePrimaryHeader(FitsHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        if (header.Count == 0 || header[0].Name != SimpleKeyword)
        {
            return Error.Validation(
                "fits.header.missing_simple", "The primary header does not begin with the mandatory SIMPLE keyword.");
        }

        var simple = header[0].Value;

        if (simple.Kind != FitsValueKind.Logical || !simple.AsLogical)
        {
            return Error.Validation(
                "fits.header.nonconforming", "The primary header declares SIMPLE = F, so the file does not conform to the FITS standard.");
        }

        return Unit.Value;
    }
}
