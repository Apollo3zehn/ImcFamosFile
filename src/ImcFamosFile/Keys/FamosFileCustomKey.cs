using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Custom keys can be used to add additional information to the file.
    /// </summary>
    public class FamosFileCustomKey : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileCustomKey"/> class.
        /// </summary>
        /// <param name="key">The key of the custom key. Must be unique.</param>
        /// <param name="value">The binary data of the custom key.</param>
        public FamosFileCustomKey(string key, byte[] value)
        {
            this.Key = key;
            this.Value = value;
        }

        internal FamosFileCustomKey(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var position = this.Reader.BaseStream.Position;
                this.Key = this.DeserializeString();

                var keyLength = this.Reader.BaseStream.Position - position;
                this.Value = this.DeserializeFixedLength((int)(keySize - keyLength));
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the unique key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the binary data.
        /// </summary>
        public byte[] Value { get; set; } = new byte[0];

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.NU;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.Key.Length, this.Key,
                this.Value
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
