using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFilePackInfo
    {
        #region Constructors

        public FamosFilePackInfo(List<FamosFileBuffer> buffers)
        {
            this.Buffers.AddRange(buffers);
        }

        internal FamosFilePackInfo(int bufferReference)
        {
            this.BufferReference = bufferReference;
        }

        #endregion

        #region Properties

        internal int BufferReference { get; private set; }

        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();
        public int ValueSize { get; set; }
        public FamosFileDataType DataType { get; set; }
        public int SignificantBits { get; set; }
        public int Offset { get; set; }
        public int GroupSize { get; set; }
        public int GapSize { get; set; }

        #endregion
    }
}
