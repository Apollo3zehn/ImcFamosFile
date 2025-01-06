namespace ImcFamosFile;

/// <summary>
/// Type of an imc FAMOS file key.
/// </summary>
public enum FamosFileKeyType
{
    /// <summary>
    /// Represents an unknown key type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Definition of the file format.
    /// </summary>
    CF = 1,

    /// <summary>
    /// Starts a group of keys.
    /// </summary>
    CK = 2,

    /// <summary>
    /// Definition of the data origin.
    /// </summary>
    NO = 3,

    /// <summary>
    /// Definition of a group.
    /// </summary>
    CB = 4,

    /// <summary>
    /// Definition of a text.
    /// </summary>
    CT = 5,

    /// <summary>
    /// Definition of a data field.
    /// </summary>
    CG = 6,

    /// <summary>
    /// Definition of a x-axis scaling.
    /// </summary>
    CD = 7,

    /// <summary>
    /// Definition of a trigger time.
    /// </summary>
    NT = 8,

    /// <summary>
    /// Definition of a z-axis scaling.
    /// </summary>
    CZ = 9,

    /// <summary>
    /// Definition of a component.
    /// </summary>
    CC = 10,

    /// <summary>
    /// Definition of a pack info.
    /// </summary>
    CP = 11,

    /// <summary>
    /// Definition of list of buffers.
    /// </summary>
    Cb = 12,

    /// <summary>
    /// Definition of calibration info.
    /// </summary>
    CR = 13,

    /// <summary>
    /// Definition of display properties.
    /// </summary>
    ND = 14,

    /// <summary>
    /// Definition of an event reference.
    /// </summary>
    Cv = 15,

    /// <summary>
    /// Definition of an event list.
    /// </summary>
    CV = 16,

    /// <summary>
    /// Definition of the channel name.
    /// </summary>
    CN = 17,

    /// <summary>
    /// Definition of a raw data block.
    /// </summary>
    CS = 18,

    /// <summary>
    /// Definition of a user defined key.
    /// </summary>
    NU = 19,

    /// <summary>
    /// Definition of a single value.
    /// </summary>
    CI = 20,

    /// <summary>
    /// Definition of a list of properties.
    /// </summary>
    Np = 21,

    /// <summary>
    /// Definition of an 'Add-Reference-Key'.
    /// </summary>
    Ca = 22,

    /// <summary>
    /// Definition of a data extracting instructions. imc internally only.
    /// </summary>
    NE = 23,

    /// <summary>
    /// Definition of language info and code page.
    /// </summary>
    NL = 24,

    /// <summary>
    /// Definition of the imc data manager version. imc internally only.
    /// </summary>
    Nv = 25
}
