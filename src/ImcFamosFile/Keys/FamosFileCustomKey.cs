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
            Key = key;
            Value = value;
        }

        internal FamosFileCustomKey(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var position = Reader.BaseStream.Position;
                Key = DeserializeString();

                var keyLength = Reader.BaseStream.Position - position;
                Value = DeserializeFixedLength((int)(keySize - keyLength));
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
                Key.Length, Key,
                Value
            };

            SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
