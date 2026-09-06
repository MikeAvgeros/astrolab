namespace AstroLab.Infrastructure.Fits;

/// <summary>cfitsio's own <c>TBYTE</c>/<c>TSHORT</c>/... datatype codes, used to tell bindings such as <see cref="NativeMethods.ReadPixels"/> how to interpret the pixel/element buffer they read into.</summary>
internal enum CfitsIoDataType
{
    Byte = 11,
    SignedByte = 12,
    Logical = 14,
    String = 16,
    UShort = 20,
    Short = 21,
    UInt = 30,
    Int = 31,
    ULong = 40,
    Long = 41,
    Float = 42,
    LongLong = 81,
    Double = 82,
    Complex = 83,
    DoubleComplex = 163
}
