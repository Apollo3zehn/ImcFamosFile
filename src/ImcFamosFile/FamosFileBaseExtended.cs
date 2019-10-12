using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    public abstract class FamosFileBaseExtended : FamosFileBase
    {
        #region Constructors

        public FamosFileBaseExtended()
        {
            //
        }

        public FamosFileBaseExtended(BinaryReader reader, int codePage) : base(reader)
        {
            this.CodePage = codePage;
        }

        #endregion

        #region Properties

        protected int CodePage { get; set; }

        #endregion

        #region Methods

        protected string DeserializeString()
        {
            var length = this.DeserializeInt32();
            var value = Encoding.GetEncoding(this.CodePage).GetString(this.Reader.ReadBytes(length));

            this.Reader.ReadByte();

            return value;
        }

        protected List<string> DeserializeStringArray()
        {
            var elementCount = this.DeserializeInt32();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => this.DeserializeString()).ToList();
        }

        #endregion
    }
}
