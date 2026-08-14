namespace AstroLab.Infrastructure.CFITSIO;

/// <summary>
/// The <c>TXXXX</c> datatype codes defined by <c>fitsio.h</c>, used to tell cfitsio how to
/// interpret/convert the buffer passed to functions such as <c>ffgpxv</c> (read pixels).
/// </summary>
public enum CfitsioDataType
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
    DoubleComplex = 163,
}
