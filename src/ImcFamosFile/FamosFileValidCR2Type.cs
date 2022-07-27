namespace ImcFamosFile
{
    /// <summary>
    /// Specifies which data to use.
    /// </summary>
    [Flags]
    public enum FamosFileValidCR2Type
    {
        /// <summary>
        /// Use DeltaX from event structure.
        /// </summary>
        DeltaX = 0,

        /// <summary>
        /// Use X0 from event structure.
        /// </summary>
        X0 = 1
    }
}
