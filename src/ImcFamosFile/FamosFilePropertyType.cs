namespace ImcFamosFile
{
    /// <summary>
    /// The data type of a property.
    /// </summary>
    public enum FamosFilePropertyType
    {
        /// <summary>
        /// A string.
        /// </summary>
        String = 0,

        /// <summary>
        /// A signed integer.
        /// </summary>
        Integer = 1,

        /// <summary>
        /// A floating point number.
        /// </summary>
        Real = 2,

        /// <summary>
        /// A time stamp in DM format.
        /// </summary>
        TimeStampInDMFormat = 3,

        /// <summary>
        /// An enumeration.
        /// </summary>
        Enumeration = 4,

        /// <summary>
        /// A boolean.
        /// </summary>
        Boolean = 5
    }
}
