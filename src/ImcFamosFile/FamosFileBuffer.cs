using System;
using System.Collections.ObjectModel;

namespace ImcFamosFile
{
    public class FamosFileBuffer
    {
        #region Fields

        private int _reference;
        private int _rawBlockIndex;
        private int _length;
        private int _offset;
        private int _consumedBytes;

        private FamosFileRawBlock? _rawBlock;

        private byte[] _userInfo;

        #endregion

        #region Constructors

        public FamosFileBuffer()
        {
            _userInfo = new byte[0];
        }

        public FamosFileBuffer(byte[] userInfo)
        {
            _userInfo = userInfo;
        }

        #endregion

        #region Properties

        public FamosFileRawBlock RawBlock
        {
            get
            {
                if (_rawBlock is null)
                    throw new FormatException("A raw block instance must be assigned to the buffer's raw block property.");

                return _rawBlock;
            }
            set { _rawBlock = value; }
        }

        public long RawBlockOffset { get; set; }

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

        internal int RawBlockIndex
        {
            get { return _rawBlockIndex; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected raw block index value > '0', got '{value}'.");

                _rawBlockIndex = value;
            }
        }

        #endregion

        #region Methods

        internal object[] GetBufferData()
        {
            return new object[]
            {
                this.Reference,
                this.RawBlockIndex,
                this.RawBlockOffset,
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