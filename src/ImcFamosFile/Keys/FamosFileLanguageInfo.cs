using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains information about the language and string encoding.
    /// </summary>
    public class FamosFileLanguageInfo : FamosFileBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileLanguageInfo"/> class.
        /// </summary>
        public FamosFileLanguageInfo()
        {
            //
        }

        internal FamosFileLanguageInfo(BinaryReader reader) : base(reader)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                CodePage = DeserializeInt32();
                Language = DeserializeHex();
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the code page of the data to write. Default is 1252, which is 'Microsoft-1252 / Western European / ANSI'.
        /// </summary>
        public int CodePage { get; set; } = 1252;

        /// <summary>
        /// Gets or sets the language code, see "MSDN Language Codes".
        /// </summary>
        public int Language { get; set; }

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.NL;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                CodePage,
                $"0x{Language.ToString("X4")}"
            };

            SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
