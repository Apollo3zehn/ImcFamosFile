namespace ImcFamosFile
{
    /// <summary>
    /// A list of buffers.
    /// </summary>
    public class FamosFileBufferInfo : FamosFileBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileBufferInfo"/> class.
        /// </summary>
        public FamosFileBufferInfo()
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileBufferInfo"/> class with the provided <paramref name="buffers"/>.
        /// </summary>
        /// <param name="buffers">A list of <see cref="FamosFileBuffer"/>.</param>
        public FamosFileBufferInfo(List<FamosFileBuffer> buffers)
        {
            Buffers.AddRange(buffers);
        }

        internal FamosFileBufferInfo(BinaryReader reader) : base(reader)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var bufferCount = DeserializeInt32();

                // This value denotes the minimum size of all buffer's user data. Set it to 0 to avoid errors.
                var userInfoSize = DeserializeInt32();

                for (int i = 0; i < bufferCount; i++)
                {
                    Buffers.Add(DeserializeBuffer());
                }
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of <see cref="FamosFileBuffer"/>.
        /// </summary>
        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();

        #endregion

        #region Methods

        /// <inheritdoc />
        public override void Validate()
        {
            if (Buffers.Count > 1)
                throw new FormatException("Although the format specification allows multiple buffer definitions per '|Cb' key, this implementation supports only a single buffer per component. Please send a sample file to the project maintainer to overcome this limitation in future.");

            foreach (var buffer in Buffers)
            {
                if (buffer.Length > 2 * Math.Pow(10, 9))
                    throw new FormatException("A buffer must not exceed 2 * 10^9 bytes.");

                if (buffer.RawBlockOffset + buffer.Length > buffer.RawBlock.Length)
                    throw new FormatException("The sum of the raw block offset and the buffer length must be <= raw block length.");

                if (buffer.Offset >= buffer.Length)
                    throw new FormatException("The value of the buffer's offset property must be < the buffer's length property.");

                if (buffer.ConsumedBytes > buffer.Length)
                    throw new FormatException("The value of the buffer's consumed bytes property must be <= the buffer's length property.");
            }
        }

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.Cb;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var bufferData = new List<object>
            {
                Buffers.Count,
                0, // User info size, see above for explanation.
            };

            foreach (var buffer in Buffers)
            {
                bufferData.AddRange(buffer.GetBufferData());
            }

            SerializeKey(writer, 1, bufferData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            Buffers = Buffers.OrderBy(buffer => buffer.Reference).ToList();
        }

        private FamosFileBuffer DeserializeBuffer()
        {
            var reference = DeserializeInt32();
            var rawBlockIndex = DeserializeInt32();
            var rawBlockOffset = DeserializeInt32();
            var length = DeserializeInt32();
            var offset = DeserializeInt32();
            var consumedBytes = DeserializeInt32();
            var isNewEvent = DeserializeInt32() == 1;
            var x0 = DeserializeReal();
            var addTime = DeserializeReal();

#warning This may fail when user info byte array contains semicolon.
            var userInfo = DeserializeKeyPart();

            return new FamosFileBuffer(userInfo)
            {
                Reference = reference,
                RawBlockIndex = rawBlockIndex,
                RawBlockOffset = rawBlockOffset,
                Length = length,
                Offset = offset,
                ConsumedBytes = consumedBytes,
                IsNewEvent = isNewEvent,
                X0 = x0,
                AddTime = addTime
            };
        }

        #endregion
    }
}