﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// Gets the list of <see cref="FamosFileBuffer"/>.
        /// </summary>
        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();

        #endregion

        #region Methods

        /// <inheritdoc />
        public override void Validate()
        {
            if (this.Buffers.Count > 1)
                throw new FormatException("Although the format specification allows multiple buffer definitions per '|Cb' key, this implementation supports only a single buffer per component. Please send a sample file to the project maintainer to overcome this limitation in future.");

            foreach (var buffer in this.Buffers)
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
            var x0 = this.DeserializeReal();
            var addTime = this.DeserializeReal();

#warning This may fail when user info byte array contains semicolon.
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
                X0 = x0,
                AddTime = addTime
            };
        }

        #endregion
    }
}