using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileBufferInfo : FamosFileBase
    {
        #region Constructors

        public FamosFileBufferInfo()
        {
            //
        }

        public FamosFileBufferInfo(List<FamosFileBuffer> buffers)
        {
            this.Buffers.AddRange(buffers);
        }

        internal FamosFileBufferInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var bufferCount = this.DeserializeInt32();

                // This value denotes the minimum size of all buffer's user data. Set it to 0 to avoid errors.
                var userInfoSize = this.DeserializeInt32();

                for (int i = 0; i < bufferCount; i++)
                {
                    this.Buffers.Add(this.DeserializeBuffer());
                }
            });
        }

        #endregion

        #region Properties

        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();

        #endregion

        #region Methods

        public override void Validate()
        {
            if (this.Buffers.Count > 1)
                throw new FormatException("Although the format specification allows multiple buffer definitions per '|Cb' key, this implementation supports only a single buffer per component. Please send a sample file to the project maintainer to overcome this limitation in future.");

            foreach (var buffer in this.Buffers)
            {
                if (buffer.Length > Math.Pow(10, 9))
                    throw new FormatException("A buffer must not exceed 10^9 bytes.");

                if (buffer.RawBlockOffset + buffer.Length > buffer.RawBlock.Length)
                    throw new FormatException("The sum of the raw block offset and the buffer length must be <= raw block length.");

                if (buffer.Offset >= buffer.Length)
                    throw new FormatException("The value of the buffer's offset property must be < the buffer's length property.");

                if (buffer.ConsumedBytes > buffer.Length)
                    throw new FormatException("The value of the buffer's consumed bytes property must be <= the buffer's length property.");
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.Cb;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var bufferData = new List<object>
            {
                this.Buffers.Count,
                0, // User info size, see above for explanation.
            };

            foreach (var buffer in this.Buffers)
            {
                bufferData.AddRange(buffer.GetBufferData());
            }

            this.SerializeKey(writer, 1, bufferData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            this.Buffers = this.Buffers.OrderBy(buffer => buffer.Reference).ToList();
        }

        private FamosFileBuffer DeserializeBuffer()
        {
            var reference = this.DeserializeInt32();
            var rawBlockIndex = this.DeserializeInt32();
            var rawBlockOffset = this.DeserializeInt32();
            var length = this.DeserializeInt32();
            var offset = this.DeserializeInt32();
            var consumedBytes = this.DeserializeInt32();
            var isNewEvent = this.DeserializeInt32() == 1;
            var x0 = this.DeserializeInt32();
            var triggerAddTime = this.DeserializeInt32();
            var userInfo = this.DeserializeKeyPart();

            return new FamosFileBuffer(userInfo)
            {
                Reference = reference,
                RawBlockIndex = rawBlockIndex,
                RawBlockOffset = rawBlockOffset,
                Length = length,
                Offset = offset,
                ConsumedBytes = consumedBytes,
                IsNewEvent = isNewEvent,
                x0 = x0,
                TriggerAddTime = triggerAddTime
            };
        }

        #endregion
    }
}