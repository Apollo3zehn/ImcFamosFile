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
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.CodePage = this.DeserializeInt32();
                this.Language = this.DeserializeHex();
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
                this.CodePage,
                $"0x{this.Language.ToString("X4")}"
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
