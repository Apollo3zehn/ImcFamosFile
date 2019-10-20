using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileChannelData
    {
        #region Constructors

        internal FamosFileChannelData(string name, FamosFileDataFieldType type, List<FamosFileComponentData> componentsData)
        {
            this.Name = name;
            this.Type = type;
            this.ComponentsData = componentsData;
        }

        #endregion

        #region Properties

        public string Name { get; }
        public FamosFileDataFieldType Type { get; }

        public List<FamosFileComponentData> ComponentsData { get; }

        #endregion
    }
}
