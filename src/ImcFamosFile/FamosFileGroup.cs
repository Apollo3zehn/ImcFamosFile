using System;
using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileGroup
    {
        #region Fields

        private int _index;

        #endregion

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
        }

        #endregion

        #region Properties

        internal int Index
        {
            get { return _index; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index > '0', got '{value}'.");

                _index = value;
            }
        }

        public string Name { get; set; }
        public string Comment { get; set; }
        public List<FamosFileText> Texts { get; private set; }
        public List<FamosFileSingleValue> SingleValues { get; private set; }
        public List<FamosFileChannelInfo> ChannelInfos { get; private set; }

        #endregion
    }
}
