namespace AstroLab.Core.Fits;

/// <summary>
/// The FITS <c>BITPIX</c> keyword values, identifying the physical representation of pixel
/// data on disk. Values match the FITS standard exactly (negative values denote IEEE floats).
/// </summary>
public enum BitPixType
{
    Byte = 8,
    Int16 = 16,
    Int32 = 32,
    Int64 = 64,
    Float32 = -32,
    Float64 = -64,
}
