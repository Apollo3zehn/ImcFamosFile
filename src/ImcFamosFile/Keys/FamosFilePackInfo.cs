using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFilePackInfo : FamosFileBase
    {
        #region Fields

        private int _bufferReference;
        private int _significantBits;
        private int _mask;
        private int _offset;
        private int _groupSize = 1;
        private int _gapSize = 0;

        #endregion

        #region Constructors

        public FamosFilePackInfo(FamosFileDataType dataType, List<FamosFileBuffer> buffers)
        {
            this.DataType = dataType;
            this.SignificantBits = this.ValueSize * 8;

            this.Buffers.AddRange(buffers);
        }

        internal FamosFilePackInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.BufferReference = this.DeserializeInt32();
                this.DeserializeInt32(); // value size will be calculated from data type
                this.DataType = (FamosFileDataType)this.DeserializeInt32();
                this.SignificantBits = this.DeserializeInt32();
                this.Mask = this.DeserializeInt32();
                this.Offset = this.DeserializeInt32();
                this.GroupSize = this.DeserializeInt32();
                this.ByteGapSize = this.DeserializeInt32();
            });
        }

        #endregion

        #region Properties

        public List<FamosFileBuffer> Buffers { get; } = new List<FamosFileBuffer>();

        public FamosFileDataType DataType { get; set; }

        public int SignificantBits
        {
            get { return _significantBits; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected significant bits value >= '0', got '{value}'.");

                _significantBits = value;
            }
        }

        public int Mask
        {
            get { return _mask; }
            set
            {
                if (!(0 <= value && value <= 65534))
                    throw new FormatException($"Expected mask value '0..65534', got '{value}'.");

                _mask = value;
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

        public int ByteGapSize
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

        public bool IsContiguous => this.ByteGapSize == 0;

        public int ByteGroupSize => this.ValueSize * this.GroupSize;

        internal int BufferReference
        {
            get { return _bufferReference; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected buffer reference > '0', got '{value}'.");

                _bufferReference = value;
            }
        }

        internal int ValueSize
        {
            get
            {
                return this.DataType switch
                {
                    FamosFileDataType.UInt8 => 1,
                    FamosFileDataType.Int8 => 1,
                    FamosFileDataType.UInt16 => 2,
                    FamosFileDataType.Int16 => 2,
                    FamosFileDataType.UInt32 => 4,
                    FamosFileDataType.Int32 => 4,
                    FamosFileDataType.Float32 => 4,
                    FamosFileDataType.Float64 => 8,
                    FamosFileDataType.Digital16Bit => 2,
                    FamosFileDataType.UInt48 => 6,
                    _ => throw new FormatException("The data type is invalid.")
                };
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CP;

        #endregion

        #region Methods

        public override void Validate()
        {
            var sizeIsInvalid = false;

            switch (this.DataType)
            {
                case FamosFileDataType.UInt8:
                case FamosFileDataType.Int8:
                    sizeIsInvalid = this.ValueSize != 1;
                    break;

                case FamosFileDataType.UInt16:
                case FamosFileDataType.Int16:
                    sizeIsInvalid = this.ValueSize != 2;
                    break;

                case FamosFileDataType.UInt32:
                case FamosFileDataType.Int32:
                case FamosFileDataType.Float32:
                    sizeIsInvalid = this.ValueSize != 4;
                    break;

                case FamosFileDataType.Float64:
                    sizeIsInvalid = this.ValueSize != 8;
                    break;

                case FamosFileDataType.ImcDevicesTransitionalRecording:
                    break;

                case FamosFileDataType.AsciiTimeStamp:
                    break;

                case FamosFileDataType.Digital16Bit:
                    sizeIsInvalid = this.ValueSize != 8;
                    break;

                case FamosFileDataType.UInt48:
                    sizeIsInvalid = this.ValueSize != 6;
                    break;

                default:
                    break;
            }

            if (sizeIsInvalid)
                throw new FormatException("The value of the pack info's value size must match the selected data type.");

            if (this.SignificantBits > this.ValueSize * 8)
                throw new FormatException("The value of the pack info's significant bits property must be <= the buffer's value size property multiplied by 8.");
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.BufferReference,
                this.ValueSize,
                (int)this.DataType,
                this.SignificantBits,
                this.Mask,
                this.Offset,
                this.GroupSize,
                this.ByteGapSize
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
