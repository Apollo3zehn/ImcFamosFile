using System.Diagnostics;
using System.Text;

namespace ImcFamosFile
{
    /// <summary>
    /// Extended base for imc FAMOS file keys to additionally handle strings.
    /// </summary>
    public abstract class FamosFileBaseExtended : FamosFileBase
    {
        #region Constructors

        private protected FamosFileBaseExtended()
        {
            //
        }

        private protected FamosFileBaseExtended(BinaryReader reader, int codePage) : base(reader)
        {
            CodePage = codePage;
        }

        #endregion

        #region Properties

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private protected int CodePage { get; set; }

        #endregion

        #region Deserialization

        private protected string DeserializeString()
        {
            var length = DeserializeInt32();
            var value = Encoding.GetEncoding(CodePage).GetString(Reader.ReadBytes(length));

            // read comma or semicolon
            Reader.ReadByte();

            return value;
        }

        private protected List<string> DeserializeStringArray()
        {
            var elementCount = DeserializeInt32();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => DeserializeString()).ToList();
        }

        #endregion
    }
}
