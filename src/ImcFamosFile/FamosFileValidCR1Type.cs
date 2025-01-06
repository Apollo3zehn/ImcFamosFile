namespace ImcFamosFile;

/// <summary>
/// Specifies which data to use.
/// </summary>
[Flags]
public enum FamosFileValidCR1Type
{
    /// <summary>
    /// Use DeltaY from event structure.
    /// </summary>
    DeltaY = 0,

    /// <summary>
    /// Use Y0 from event structure.
    /// </summary>
    Y0 = 1
}
