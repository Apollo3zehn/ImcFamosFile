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
                var startPosition = this.Reader.BaseStream.Position;
                this.Index = this.DeserializeInt32();
                var endPosition = this.Reader.BaseStream.Position;

                this.Length = keySize - (endPosition - startPosition);
                this.FileOffset = endPosition;

                this.Reader.BaseStream.Seek(this.Length + 1, SeekOrigin.Current);
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
            var data = new object[]
            {
                this.Index,
                new FamosFilePlaceHolder() { Length = this.Length }
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
