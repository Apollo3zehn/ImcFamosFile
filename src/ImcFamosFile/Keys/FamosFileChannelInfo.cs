using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileChannel : FamosFileBaseProperty
    {
        #region Fields

        private int _groupIndex;

        #endregion

        #region Constructors

        public FamosFileChannel(string name)
        {
            this.Name = name;
        }

        internal FamosFileChannel(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.GroupIndex = this.DeserializeInt32();
                this.DeserializeInt32(); // reserved parameter
                this.BitIndex = this.DeserializeInt32();
                this.Name = this.DeserializeString();
                this.Comment = this.DeserializeString();
            });
        }

        #endregion

        #region Properties

        public int BitIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        internal int GroupIndex
        {
            get { return _groupIndex; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected group index >= '0', got '{value}'.");

                _groupIndex = value;
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CN;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            var data = new object[]
            {
                this.GroupIndex,
                "0", // reserved parameter
                this.BitIndex,
                this.Name.Length, this.Name,
                this.Comment.Length, this.Comment
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
