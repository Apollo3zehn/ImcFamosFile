namespace ImcFamosFile;

/// <summary>
/// Specifies which data to use.
/// </summary>
[Flags]
public enum FamosFileValidCDType
{
    /// <summary>
    /// Use DeltaX from event list, else from x-axis scaling.
    /// </summary>
    DeltaX = 0,

    /// <summary>
    /// Use X0 from event list, else from buffer.
    /// </summary>
    X0 = 1,

    /// <summary>
    /// Use Z0 from X0 from event list, else from z-axis scaling.
    /// </summary>
    Z0X0 = 2
}
