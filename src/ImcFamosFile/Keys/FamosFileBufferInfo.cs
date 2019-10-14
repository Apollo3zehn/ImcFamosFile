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

        internal FamosFileBufferInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var bufferCount = this.DeserializeInt32();
                var userInfoSize = this.DeserializeInt32();

                for (int i = 0; i < bufferCount; i++)
                {
                    this.Buffers.Add(this.DeserializeBuffer());
                }

                this.UserInfo = this.DeserializeKeyPart();

                if (this.UserInfo.Length != userInfoSize)
                    throw new FormatException("The given size of the user info does not match the actual size.");
            });
        }

        #endregion

        #region Properties

#warning TODO: Probably the user info is repeated for each buffer. But why is there only a single user info length in this key? The user info is probably trigger related.
        public byte[] UserInfo { get; set; } = new byte[0];

        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();

        #endregion

        #region Methods

        internal override void Validate()
        {
            foreach (var buffer in this.Buffers)
            {
                if (buffer.RawDataOffset + buffer.Length > buffer.RawData.Length)
                    throw new FormatException("The sum of the raw data offset and the buffer length must be <= raw data length.");

                if (buffer.Offset >= buffer.Length)
                    throw new FormatException("The value of the buffer's offset property must be < the buffer's length property.");

                if (buffer.ConsumedBytes > buffer.Length)
                    throw new FormatException("The value of the buffer's consumed bytes property must be <= the buffer's length property.");
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.Cb;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var bufferData = new List<object>
            {
                this.Buffers.Count,
                this.UserInfo.Length,
            };

            foreach (var buffer in this.Buffers)
            {
                bufferData.AddRange(buffer.GetBufferData());
            }

            bufferData.Add(this.UserInfo);

            this.SerializeKey(writer, 1, bufferData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            this.Buffers = this.Buffers.OrderBy(buffer => buffer.Reference).ToList();
        }

        private FamosFileBuffer DeserializeBuffer()
        {
            return new FamosFileBuffer()
            {
                Reference = this.DeserializeInt32(),
                RawDataIndex = this.DeserializeInt32(),
                RawDataOffset = this.DeserializeInt32(),
                Length = this.DeserializeInt32(),
                Offset = this.DeserializeInt32(),
                ConsumedBytes = this.DeserializeInt32(),
                IsNewEvent = this.DeserializeInt32() == 1,
                x0 = this.DeserializeInt32(),
                TriggerAddTime = this.DeserializeInt32()
            };
        }

        #endregion
    }
}