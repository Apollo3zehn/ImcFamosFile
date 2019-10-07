using System;
using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFilePackInfo
    {
        #region Fields

        private int _bufferReference;
        private int _valueSize;
        private int _offset;
        private int _groupSize;
        private int _gapSize;

        #endregion

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

        internal int BufferReference
        {
            get { return _bufferReference; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected buffer reference > '0', got '{value}'.");

                _bufferReference = value;
            }
        }

        public List<FamosFileBuffer> Buffers { get; } = new List<FamosFileBuffer>();

        public int ValueSize
        {
            get { return _valueSize; }
            set
            {
                if (!(1 <= value && value < 8))
                    throw new
                        FormatException($"Expected value size '1..8', got '{value}'.");

                _valueSize = value;
            }
        }

        public FamosFileDataType DataType { get; set; }

#warning: TODO: Validate Significant Bits
        public int SignificantBits { get; set; }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (value < 0)
                    throw new
                        FormatException($"Expected offset >= '0', got '{value}'.");

                _offset = value;
            }
        }

        public int GroupSize
        {
            get { return _groupSize; }
            set
            {
                if (value < 1)
                    throw new
                        FormatException($"Expected group size >= '1', got '{value}'.");

                _groupSize = value;
            }
        }


        public int GapSize
        {
            get { return _gapSize; }
            set
            {
                if (value < 0)
                    throw new
                        FormatException($"Expected gap size >= '0', got '{value}'.");

                _gapSize = value;
            }
        }

        #endregion
    }
}
