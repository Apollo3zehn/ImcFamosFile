using System;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains the actual data when serialized to the file.
    /// </summary>
    public class FamosFileRawBlock : FamosFileBase
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileRawBlock"/> instance.
        /// </summary>
        public FamosFileRawBlock()
        {
            //
        }

        internal FamosFileRawBlock(BinaryReader reader) : base(reader)
        {
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey((Action<long>)(keySize =>
                {
                    var startPosition = this.Reader.BaseStream.Position;
                    this.Index = this.DeserializeInt32();
                    var endPosition = this.Reader.BaseStream.Position;

                    this.CompressionType = FamosFileCompressionType.Uncompressed;
                    this.Length = keySize - (endPosition - startPosition);
                    this.FileOffset = endPosition;

                    this.Reader.BaseStream.TrySeek(this.Length + 1, SeekOrigin.Current);
                }));
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey((Action<long>)(keySize =>
                {
                    this.Index = this.DeserializeInt32();
                    this.CompressionType = (FamosFileCompressionType)this.DeserializeInt32();
                    this.Length = this.DeserializeInt64();
                    this.FileOffset = this.Reader.BaseStream.Position;

                    this.Reader.BaseStream.TrySeek(this.Length + 1, SeekOrigin.Current);
                }));
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the length of the raw data block in bytes.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the compression type of the data.
        /// </summary>
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

        internal long FileOffset { get; private set; }

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CS;

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
            this.FileOffset = writer.BaseStream.Position - this.Length - 1 - 2;
        }

        #endregion
    }
}
