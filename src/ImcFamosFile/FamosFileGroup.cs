using System;
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
            this.ChannelInfos = new List<FamosFileChannelInfo>();
        }

        internal FamosFileGroup(int index) : this()
        {
            this.Index = index;

            if (index <= 0)
                throw new FormatException($"Expected group index >= '1', got '{index}'.");
        }

        #endregion

        #region Properties

        internal int Index { get; private set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public List<FamosFileText> Texts { get; private set; }
        public List<FamosFileSingleValue> SingleValues { get; private set; }
        public List<FamosFileChannelInfo> ChannelInfos { get; private set; }

        #endregion
    }
}
