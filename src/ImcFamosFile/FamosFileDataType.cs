namespace ImcFamosFile;

/// <summary>
/// Data type of an imc FAMOS file component.
/// </summary>
public enum FamosFileDataType
{
    /// <summary>
    /// A single byte unsigned interger.
    /// </summary>
    UInt8 = 1,

    /// <summary>
    /// A single byte signed integer.
    /// </summary>
    Int8 = 2,

    /// <summary>
    /// A two-byte unsigned integer.
    /// </summary>
    UInt16 = 3,

    /// <summary>
    /// A two-byte signed integer.
    /// </summary>
    Int16 = 4,

    /// <summary>
    /// A four-byte unsigned integer.
    /// </summary>
    UInt32 = 5,

    /// <summary>
    /// A four-byte signed integer.
    /// </summary>
    Int32 = 6,

    /// <summary>
    /// A four-byte floating point number.
    /// </summary>
    Float32 = 7,

    /// <summary>
    /// An eight-byte floating point number.
    /// </summary>
    Float64 = 8,

    /// <summary>
    /// Unknown.
    /// </summary>
    ImcDevicesTransitionalRecording = 9,

    /// <summary>
    /// A timestamp in Ascii format.
    /// </summary>
    AsciiTimeStamp = 10,

    /// <summary>
    /// A two-byte (16-bit) digital data type.
    /// </summary>
    Digital16Bit = 11,

    /// <summary>
    /// A six-byte unsigned integer.
    /// </summary>
    UInt48 = 13
}
