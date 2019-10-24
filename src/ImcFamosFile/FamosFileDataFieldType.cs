namespace ImcFamosFile
{
    /// <summary>
    /// Determines the field type.
    /// </summary>
    public enum FamosFileFieldType
    {
        /// <summary>
        /// There are one or more Y-datasets contained in the <see cref="FamosFileField"/>, all sampled with equidistant time. Only components of type <see cref="FamosFileComponentType.Primary"/> are allowed.
        /// </summary>
        MultipleYToSingleEquidistantTime = 1,

        /// <summary>
        /// There are one or more Y-datasets contained in the <see cref="FamosFileField"/>, all sampled with monotonous increasing time. One or more components of type <see cref="FamosFileComponentType.Primary"/> and a single component of type <see cref="FamosFileComponentType.Secondary"/> is allowed.
        /// </summary>
        MultipleYToSingleMonotonousTime = 2,

        /// <summary>
        /// This type is used to represent characteristic curves with one X component and multiple Y-components or vice versa. One or more components of type <see cref="FamosFileComponentType.Primary"/> and a single component of type <see cref="FamosFileComponentType.Secondary"/> is allowed or vice versa.
        /// </summary>
        MultipleYToSingleXOrViceVersa = 3,

        /// <summary>
        /// The <see cref="FamosFileField"/> must contain a component of type <see cref="FamosFileComponentType.Primary"/> (real part) and a component of type <see cref="FamosFileComponentType.Secondary"/> (imaginary part).
        /// </summary>
        ComplexRealImaginary = 4,

        /// <summary>
        /// The <see cref="FamosFileField"/> must contain a component of type <see cref="FamosFileComponentType.Primary"/> (magnitude) and a component of type <see cref="FamosFileComponentType.Secondary"/> (phase).
        /// </summary>
        ComplexMagnitudePhase = 5,

        /// <summary>
        /// The <see cref="FamosFileField"/> must contain a component of type <see cref="FamosFileComponentType.Primary"/> (magnitude in dB) and a component of type <see cref="FamosFileComponentType.Secondary"/> (phase).
        /// </summary>
        ComplexMagnitudeDBPhase = 6
    }
}
