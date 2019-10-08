using System.IO;

namespace ImcFamosFile
{
    public class FamosFileLanguageInfo : FamosFileBase
    {
        #region Constructors

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

        public int CodePage { get; set; }

        public int Language { get; set; }

        #endregion
    }
}
