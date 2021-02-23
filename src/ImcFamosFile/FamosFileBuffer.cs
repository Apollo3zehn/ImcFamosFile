using System;
using System.Collections.ObjectModel;

namespace ImcFamosFile
{
    /// <summary>
    /// A buffer describes the length and position of component data within the file. 
    /// </summary>
    public class FamosFileBuffer
    {
        #region Fields

        private int _reference;
        private int _rawBlockIndex;
        private int _length;
        private int _offset;
        private int _consumedBytes;

        private FamosFileRawBlock? _rawBlock;

        private readonly byte[] _userInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="FamosFileBuffer"/> class.
        /// </summary>
        public FamosFileBuffer()
        {
            _userInfo = new byte[0];
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FamosFileBuffer"/> class.
        /// </summary>
        /// <param name="userInfo">The binary user info.</param>
        public FamosFileBuffer(byte[] userInfo)
        {
            _userInfo = userInfo;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the associated raw block.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the offset of the buffer in the raw block in bytes.
        /// </summary>
        public long RawBlockOffset { get; set; }

        /// <summary>
        /// Gets or sets the length of the buffer in bytes.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the offset of the first sample in the buffer. REMARKS: If > '0', the buffer is a ring buffer.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the number of valid bytes in the buffer.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the X0 of the first sample in the buffer.
        /// </summary>
        public decimal X0 { get; set; }

        /// <summary>
        /// Gets or sets the add time, which is used with multiple trigger events to determine the absolute trigger time. 
        /// </summary>
        public decimal AddTime { get; set; }

        /// <summary>
        /// Gets or sets a boolean which indicates a new event of measurement data.
        /// </summary>
        public bool IsNewEvent { get; set; }

        /// <summary>
        /// Gets or sets binary user info for the creator of the file.
        /// </summary>
        public ReadOnlyCollection<byte> UserInfo => Array.AsReadOnly(_userInfo);

        /// <summary>
        /// Gets a boolean indicating if this buffer is a ring buffer.
        /// </summary>
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
                this.X0,
                this.AddTime,
                _userInfo
            };
        }

        #endregion
    }
}