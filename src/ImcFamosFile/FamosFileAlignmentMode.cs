namespace ImcFamosFile
{
    /// <summary>
    /// Determines the buffer and pack info alignment mode.
    /// </summary>
    /// <example>
    /// B = Buffer, C = Channel, V = Value
    /// <code>
    /// // Continuous layout:
    /// [Raw Block 1 [B1 [C1_V1 C1_V2 ...]] [B2 [C1_V1 C1_V2 ...]] [B2 [C1_V1 C1_V2 ...]] ...]
    /// </code>
    /// <code>
    /// // Interlaced layout:
    /// [Raw Block 1 [B1 [C1_V1] [C2_V1] [C3_V1] [C1_V2] [C2_V2] [C3_V2] ...]]
    /// </code>
    /// </example>
    public enum FamosFileAlignmentMode
    {
        /// <summary>
        /// Assigns each buffer to a continuous region in the raw data block. This is the default.
        /// </summary>
        Continuous = 0,

        /// <summary>
        /// Sorts the data like in an Excel sheet, i.e. row-wise and creates a single big buffer and raw data block for all data.
        /// </summary>
        Interlaced = 1
    }
}
