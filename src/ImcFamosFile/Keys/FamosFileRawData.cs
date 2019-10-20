using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileRawData : FamosFileBase
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        public FamosFileRawData()
        {
            //
        }

        internal FamosFileRawData(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.Index = this.DeserializeInt32();

                this.Length = keySize;
                this.FileOffset = this.Reader.BaseStream.Position;

                this.Reader.BaseStream.Seek(keySize, SeekOrigin.Current);
            });
        }

        #endregion

        #region Properties

        public long Length { get; set; }

        internal int Index
        {
            get { return _index; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index > '0', got '{value}'.");

                _index = value;
            }
        }

        internal long FileOffset { get; private set; }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CS;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            this.SerializeKey(writer, 1, this.Length);
        }

        #endregion
    }
}
