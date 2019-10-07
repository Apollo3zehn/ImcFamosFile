using System;

namespace ImcFamosFile
{
    public class FamosFileBuffer
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

        internal FamosFileBuffer(int reference, int rawDataReference)
        {
            this.Reference = reference;
            this.RawDataReference = rawDataReference;
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

#warning TODO: Validate that CS key is big enough
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

#warning TODO: Validate that offset < length in case of an ring buffer.
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

#warning TODO: Validate that consumed bytes < length.
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

        #endregion
    }
}