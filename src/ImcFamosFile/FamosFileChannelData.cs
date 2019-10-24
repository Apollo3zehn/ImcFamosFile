using System.Collections.Generic;
using System.Diagnostics;

namespace ImcFamosFile
{
    /// <summary>
    /// Channel wraps a list of <see cref="ComponentsData"/>, depending on the type of data loaded.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public class FamosFileChannelData
    {
        #region Constructors

        internal FamosFileChannelData(string name, FamosFileFieldType type, List<FamosFileComponentData> componentsData)
        {
            this.Name = name;
            this.Type = type;
            this.ComponentsData = componentsData;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the data field.
        /// </summary>
        public FamosFileFieldType Type { get; }

        /// <summary>
        /// Gets a list of <see cref="FamosFileComponentData"/>.
        /// </summary>
        public List<FamosFileComponentData> ComponentsData { get; }

        #endregion
    }
}
