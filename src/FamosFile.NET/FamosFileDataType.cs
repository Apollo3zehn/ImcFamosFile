namespace FamosFile.NET
{
    /// <summary>
    /// Data type of a FamosFile variable.
    /// </summary>
    public enum FamosFileDataType
    {
        UnsignedByte = 1,
        SignedByte = 2,
        UnsignedShort = 3,
        SignedShort = 4,
        UnsignedLong = 5,
        SignedLong = 6,
        Float = 7,
        Double = 8,
        LSB_in_2byte_Word_digital = 11,
        Six_byte_unsigned_long = 13
    }
}
