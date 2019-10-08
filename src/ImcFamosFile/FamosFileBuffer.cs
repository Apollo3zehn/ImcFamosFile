using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileBuffer : FamosFileBase
    {
        #region Fields

        private int _reference;
        private int _rawDataReference;
        private int _length;
        private int _offset;
        private int _consumedBytes;

        #endregion

        #region Constructors

        public FamosFileBuffer(FamosFileRawData rawData)
        {
            this.RawData = rawData;
        }

        internal FamosFileBuffer(BinaryReader reader) : base(reader)
        {
            this.Reference = this.DeserializeInt32();
            this.RawDataReference = this.DeserializeInt32();

            this.RawDataOffset = this.DeserializeInt32();
            this.Length = this.DeserializeInt32();
            this.Offset = this.DeserializeInt32();
            this.ConsumedBytes = this.DeserializeInt32();
            this.IsNewEvent = this.DeserializeInt32() == 1;
            this.x0 = this.DeserializeInt32();
            this.TriggerAddTime = this.DeserializeInt32();
            this.UserInfo = this.DeserializeKeyPart();
        }

        #endregion

        #region Properties

        internal int Reference
        {
            get { return _reference; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected reference value > '0', got '{value}'.");

                _reference = value;
            }
        }

        internal int RawDataReference
        {
            get { return _rawDataReference; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected raw data reference value > '0', got '{value}'.");

                _rawDataReference = value;
            }
        }

        public FamosFileRawData? RawData { get; set; }
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
        public byte[]? UserInfo { get; set; }
        public bool IsNewEvent { get; set; }

        public bool IsRingBuffer => this.Offset > 0;

        #endregion

        #region Methods

        internal override void Validate()
        {
            if (this.RawData is null)
                throw new FormatException("The buffer's raw data property must be assigned to a raw data instance.");

            if (this.RawDataOffset + this.Length > this.RawData.Length)
                throw new FormatException("The sum of the raw data offset and the buffer length must be <= raw data length.");

            if (this.Offset >= this.Length)
                throw new FormatException("The value of the buffer's offset property must be < the buffer's length property.");

            if (this.ConsumedBytes > this.Length)
                throw new FormatException("The value of the buffer's consumed bytes property must be <= the buffer's length property.");
        }

        #endregion
    }
}