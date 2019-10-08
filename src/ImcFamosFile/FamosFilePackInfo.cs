﻿using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFilePackInfo : FamosFileBase
    {
        #region Fields

        private int _bufferReference;
        private int _valueSize;
        private int _significantBits;
        private int _offset;
        private int _groupSize;
        private int _gapSize;

        #endregion

        #region Constructors

        public FamosFilePackInfo(List<FamosFileBuffer> buffers)
        {
            this.Buffers.AddRange(buffers);
        }

        internal FamosFilePackInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.BufferReference = this.DeserializeInt32();
                this.ValueSize = this.DeserializeInt32();
                this.DataType = (FamosFileDataType)this.DeserializeInt32();
                this.SignificantBits = this.DeserializeInt32();
                this.Offset = Math.Max(this.DeserializeInt32(), this.DeserializeInt32());
                this.GroupSize = this.DeserializeInt32();
                this.GapSize = this.DeserializeInt32();
            });
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

        public int SignificantBits
        {
            get { return _significantBits; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected reference value >= '0', got '{value}'.");

                _significantBits = value;
            }
        }

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

        #region Methods

        internal override void Validate()
        {
            if (this.SignificantBits > this.ValueSize * 8)
                throw new FormatException("The value of the pack info's significant bits property must be <= the buffer's value size property multiplied by 8.");
        }

        #endregion
    }
}
