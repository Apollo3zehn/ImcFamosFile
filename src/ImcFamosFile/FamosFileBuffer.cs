using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileBuffer : FamosFileBase
    {
        #region Fields

        private int _reference;
        private int _rawDataIndex;
        private int _length;
        private int _offset;
        private int _consumedBytes;

        private FamosFileRawData? _rawData;

        #endregion

        #region Constructors

        public FamosFileBuffer(FamosFileRawData rawData)
        {
            this.RawData = rawData;
        }

        internal FamosFileBuffer(BinaryReader reader) : base(reader)
        {
            this.Reference = this.DeserializeInt32();
            this.RawDataIndex = this.DeserializeInt32();

            this.RawDataOffset = this.DeserializeInt32();
            this.Length = this.DeserializeInt32();
            this.Offset = this.DeserializeInt32();
            this.ConsumedBytes = this.DeserializeInt32();
            this.IsNewEvent = this.DeserializeInt32() == 1;
            this.x0 = this.DeserializeInt32();
            this.TriggerAddTime = this.DeserializeInt32();
        }

        #endregion

        #region Properties

        internal int Reference
        {
            get { return _reference; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected reference value > '0', got '{value}'.");

                _reference = value;
            }
        }

        internal int RawDataIndex
        {
            get { return _rawDataIndex; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected raw data index value > '0', got '{value}'.");

                _rawDataIndex = value;
            }
        }

        public FamosFileRawData RawData
        {
            get
            {
                if (_rawData is null)
                    throw new FormatException("A raw data instance must be assigned to the buffer's raw data property.");

                return _rawData;
            }
            set { _rawData = value; }
        }

        public int RawDataOffset { get; set; }

        public int Length
        {
            get { return _length; }
            set
            {
                if (value < 0)
                    throw new
                        FormatException($"Expected length >= '0', got '{value}'.");

                _length = value;
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

        public int ConsumedBytes
        {
            get { return _consumedBytes; }
            set
            {
                if (value < 0)
                    throw new
                        FormatException($"Expected consumed bytes >= '0', got '{value}'.");

                _consumedBytes = value;
            }
        }

        public int x0 { get; set; }
        public int TriggerAddTime { get; set; }
        public bool IsNewEvent { get; set; }

        public bool IsRingBuffer => this.Offset > 0;

        #endregion

        #region Methods

        internal override void Validate()
        {
            if (this.RawDataOffset + this.Length > this.RawData.Length)
                throw new FormatException("The sum of the raw data offset and the buffer length must be <= raw data length.");

            if (this.Offset >= this.Length)
                throw new FormatException("The value of the buffer's offset property must be < the buffer's length property.");

            if (this.ConsumedBytes > this.Length)
                throw new FormatException("The value of the buffer's consumed bytes property must be <= the buffer's length property.");
        }

        internal object[] GetBufferData()
        {
            return new object[]
            {
                this.Reference,
                this.RawDataIndex,

                this.RawDataOffset,
                this.Length,
                this.Offset,
                this.ConsumedBytes,
                this.IsNewEvent ? 1 : 0,
                this.x0,
                this.TriggerAddTime
            };
        }

        protected override FamosFileKeyType KeyType => throw new NotImplementedException();

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}