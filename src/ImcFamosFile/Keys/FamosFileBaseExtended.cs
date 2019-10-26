using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    /// <summary>
    /// Extended base for imc FAMOS file keys to additionally handle strings.
    /// </summary>
    public abstract class FamosFileBaseExtended : FamosFileBase
    {
        #region Constructors

        [HideFromApi]
        internal protected FamosFileBaseExtended()
        {
            //
        }

        [HideFromApi]
        internal protected FamosFileBaseExtended(BinaryReader reader, int codePage) : base(reader)
        {
            this.CodePage = codePage;
        }

        #endregion

        #region Properties

        [HideFromApi]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal protected int CodePage { get; set; }

        #endregion

        #region Deserialization

        [HideFromApi]
        internal protected string DeserializeString()
        {
            var length = this.DeserializeInt32();
            var value = Encoding.GetEncoding(this.CodePage).GetString(this.Reader.ReadBytes(length));

            // read comma or semicolon
            this.Reader.ReadByte();

            return value;
        }

        [HideFromApi]
        internal protected List<string> DeserializeStringArray()
        {
            var elementCount = this.DeserializeInt32();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => this.DeserializeString()).ToList();
        }

        #endregion
    }
}
