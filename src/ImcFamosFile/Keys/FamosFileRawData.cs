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
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    var startPosition = this.Reader.BaseStream.Position;
                    this.Index = this.DeserializeInt32();
                    var endPosition = this.Reader.BaseStream.Position;

                    this.CompressionType = FamosFileCompressionType.Uncompressed;
                    this.Length = keySize - (endPosition - startPosition);
                    this.FileReadOffset = endPosition;

                    this.Reader.BaseStream.Seek(this.Length + 1, SeekOrigin.Current);
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    this.Index = this.DeserializeInt32();
                    this.CompressionType = (FamosFileCompressionType)this.DeserializeInt32();
                    this.Length = this.DeserializeInt64();
                    this.FileReadOffset = this.Reader.BaseStream.Position;

                    this.Reader.BaseStream.Seek(this.Length + 1, SeekOrigin.Current);
                });
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }
        }

        #endregion

        #region Properties

        public long Length { get; set; }
        public FamosFileCompressionType CompressionType { get; set; }

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

        internal long FileReadOffset { get; private set; }

        internal long FileWriteOffset { get; private set; }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CS;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.Index,
                (int)this.CompressionType,
                this.Length,
                new FamosFilePlaceHolder() { Length = this.Length }
            };

            this.SerializeKey(writer, 2, data, addLineBreak: true); // --> -2 characters
            this.FileWriteOffset = writer.BaseStream.Position - this.Length - 1 - 2;
        }

        #endregion
    }
}
