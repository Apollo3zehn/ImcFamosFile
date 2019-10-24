namespace ImcFamosFile
{
    /// <summary>
    /// Determines the type of the compression.
    /// </summary>
    public enum FamosFileCompressionType
    {
        /// <summary>
        /// The data are uncompressed.
        /// </summary>
        Uncompressed = 0,

        /// <summary>
        /// The data are compressed using the ZLIB algorithm.
        /// </summary>
        ZLIB = 1
    }
}