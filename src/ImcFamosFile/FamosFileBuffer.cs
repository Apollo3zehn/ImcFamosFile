using System;
using System.Collections.ObjectModel;

namespace ImcFamosFile
{
    public class FamosFileBuffer
    {
        #region Fields

        private int _reference;
        private int _rawDataIndex;
        private int _length;
        private int _offset;
        private int _consumedBytes;

        private FamosFileRawData? _rawData;

        private byte[] _userInfo;

        #endregion

        #region Constructors

        public FamosFileBuffer(byte[] userInfo)
        {
            _userInfo = userInfo;
        }

        #endregion

        #region Properties

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

        public long RawDataOffset { get; set; }

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
        public ReadOnlyCollection<byte> UserInfo => Array.AsReadOnly(_userInfo);
        public bool IsRingBuffer => this.Offset > 0;

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

        #endregion

        #region Methods

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
                this.TriggerAddTime,
                _userInfo
            };
        }

        #endregion
    }
}