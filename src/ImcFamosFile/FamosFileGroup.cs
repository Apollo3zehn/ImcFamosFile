using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileGroup
    {
        public FamosFileGroup(int index)
        {
            this.Index = index;
            this.Name = string.Empty;
            this.Comment = string.Empty;
            this.Texts = new List<FamosFileText>();
            this.SingleValues = new List<FamosFileSingleValue>();

            if (index == 0)
                this.Name = "default";
        }

        public int Index { get; private set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public List<FamosFileText> Texts { get; private set; }
        public List<FamosFileSingleValue> SingleValues { get; private set; }
    }
}
