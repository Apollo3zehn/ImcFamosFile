namespace ImcFamosFile;

/// <summary>
/// Specifies the trigger time source.
/// </summary>
public enum FamosFileValidNTType
{
    /// <summary>
    /// Use trigger time from trigger time instance.
    /// </summary>
    TriggerTime = 0,

    /// <summary>
    /// Use trigger time from event info instance.
    /// </summary>
    EventInfo = 1,
}
