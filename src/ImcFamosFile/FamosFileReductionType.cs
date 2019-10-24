namespace ImcFamosFile
{
    /// <summary>
    /// Determines the data reduction procedure.
    /// </summary>
    public enum FamosFileReductionType
    {
        /// <summary>
        /// The data are not subject to reduction.
        /// </summary>
        NoReduction = 0,

        /// <summary>
        /// Depends on certain conditions.
        /// </summary>
        ConditionalStorage = 1,

        /// <summary>
        /// imc linear approximation.
        /// </summary>
        ImcLineApproximation = 2,

        /// <summary>
        /// User defined method.
        /// </summary>
        UserDefined = 3
    }
}
