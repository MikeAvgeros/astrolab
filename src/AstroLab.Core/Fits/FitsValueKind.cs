namespace AstroLab.Core.Fits;

/// <summary>The syntactic category of a parsed FITS header value.</summary>
public enum FitsValueKind
{
    /// <summary>No value is present (blank, COMMENT, HISTORY, or END cards).</summary>
    None,
    String,
    Integer,
    Real,
    Logical,

    /// <summary>Recognisable as a value field but not one of the standard scalar types (e.g. complex).</summary>
    Undefined,
}
