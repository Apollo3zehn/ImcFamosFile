namespace ImcFamosFile
{
    /// <summary>
    /// Determines the buffer and pack info alignment mode.
    /// </summary>
    public enum FamosFileAlignmentMode
    {
        /// <summary>
        /// Assigns each buffer to a continuous region in the raw data block. This is the default.
        /// </summary>
        /// <remarks>Layout (C = Component, V = Value): [Raw Block 1 [Buffer 1 [C1_V1 C1_V2 ...]] [Buffer 2 [C1_V1 C1_V2 ...]] [Buffer 2 [C1_V1 C1_V2 ...]] ...]</remarks>
        Continuous = 0,

        /// <summary>
        /// Sorts the data like in an Excel sheet, i.e. row-wise and creates a single big buffer and raw data block for all data.
        /// </summary>
        /// <remarks>Layout (C = Component, V = Value): [Raw Block 1 [Buffer 1 [C1_V1] [C2_V1] [C3_V1] [C1_V2] [C2_V2] [C3_V2] ...]]</remarks>
        Interlaced = 1
    }
}
