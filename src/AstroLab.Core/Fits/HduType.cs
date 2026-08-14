namespace AstroLab.Core.Fits;

/// <summary>The kind of Header/Data Unit, derived from the <c>SIMPLE</c> / <c>XTENSION</c> keywords.</summary>
public enum HduType
{
    Primary,
    Image,
    AsciiTable,
    BinaryTable,
    Unknown,
}
