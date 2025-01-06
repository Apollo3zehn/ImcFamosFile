namespace ImcFamosFile;

/// <summary>
/// Determines how the pretrigger of the buffer of the x-axis scaling instance is used.
/// </summary>
public enum FamosFilePretriggerUsage
{
    /// <summary>
    /// No X0 is used (e.g. for rainflow matrix).
    /// </summary>
    NoPretrigger = 0,

    /// <summary>
    /// X0 of buffer is used. This is the normal case for self-written data.
    /// </summary>
    CbToX0 = 1,

    /// <summary>
    /// X0 of this instance used.
    /// </summary>
    CD2ToX0 = 2,

    /// <summary>
    /// X0 of this instance used as z0.
    /// </summary>
    X0toZ0 = 3,

    /// <summary>
    /// X0 of this instance serves as integer offset for the time track with scalable factor and offset.
    /// </summary>
    AsciiTimeOffset = 4
}
