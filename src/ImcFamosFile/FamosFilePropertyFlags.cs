using System;

namespace ImcFamosFile
{
    /// <summary>
    /// Flags that describes the component access in an editor.
    /// </summary>
    [Flags]
    public enum FamosFilePropertyFlags
    {
        /// <summary>
        /// Hide in editors.
        /// </summary>
        EditorHide = 2,

        /// <summary>
        /// Read-only in editors.
        /// </summary>
        EditorReadOnly = 4,
    }
}
