using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileGroup : FamosFileBaseExtended
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        public FamosFileGroup()
        {
            //
        }

        internal FamosFileGroup(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.Index = this.DeserializeInt32();

                this.Name = this.DeserializeString();
                this.Comment = this.DeserializeString();
            });
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

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<FamosFileText> Texts { get; private set; } = new List<FamosFileText>();
        public List<FamosFileSingleValue> SingleValues { get; private set; } = new List<FamosFileSingleValue>();
        public List<FamosFileChannelInfo> ChannelInfos { get; private set; } = new List<FamosFileChannelInfo>();

        #endregion
    }
}
