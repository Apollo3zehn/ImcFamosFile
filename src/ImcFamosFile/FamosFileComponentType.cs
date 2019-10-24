namespace ImcFamosFile
{
    /// <summary>
    /// Determines the type of the component.
    /// </summary>
    public enum FamosFileComponentType
    {
        /// <summary>
        /// Depending on the data field type: single value, real part, magnitude, magnitude in dB, 'y' of XY or timestamp (ASCII).
        /// </summary>
        Primary = 1,

        /// <summary>
        /// Depending on the data field type: imaginary part, phase or 'x' of XY.
        /// </summary>
        Secondary = 2  
    }
}
