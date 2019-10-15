using System;

namespace ImcFamosFile
{
    [Flags]
    public enum FamosFilePropertyFlags
    {
        None = 0,
        EditorHide = 2,
        EditorReadOnly = 4,
    }
}
