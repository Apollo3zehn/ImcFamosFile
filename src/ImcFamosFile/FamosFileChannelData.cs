using System.Collections.Generic;
using System.Diagnostics;

namespace ImcFamosFile
{
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

        public string Name { get; }
        public FamosFileFieldType Type { get; }

        public List<FamosFileComponentData> ComponentsData { get; }

        #endregion
    }
}
