using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileChannelData
    {
        #region Constructors

        internal FamosFileChannelData(FamosFileDataFieldType type, List<FamosFileComponentData> componentsData)
        {
            this.Type = type;
            this.ComponentsData = componentsData;
        }

        #endregion

        #region Properties

        public FamosFileDataFieldType Type { get; }

        public List<FamosFileComponentData> ComponentsData { get; }

        #endregion
    }
}
