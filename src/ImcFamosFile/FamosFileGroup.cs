using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileGroup
    {
        #region Constructors

        public FamosFileGroup()
        {
            this.Name = string.Empty;
            this.Comment = string.Empty;
            this.Texts = new List<FamosFileText>();
            this.SingleValues = new List<FamosFileSingleValue>();
        }

        public FamosFileGroup(int index) : this()
        {
            this.Index = index;

            if (index == 0)
                this.Name = "default";
        }

        #endregion

        #region Properties

        public int Index { get; private set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public List<FamosFileText> Texts { get; private set; }
        public List<FamosFileSingleValue> SingleValues { get; private set; }

        #endregion
    }
}
