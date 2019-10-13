using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileEventLocationInfo : FamosFileBase
    {
        #region Fields

        private int _offset;
        private int _groupSize;
        private int _gapSize;
        private int _eventCount;

        #endregion

        #region Constructors

        public FamosFileEventLocationInfo()
        {
            //
        }

        internal FamosFileEventLocationInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.FirstEventIndex = this.DeserializeInt32();
                this.Offset = this.DeserializeInt32();
                this.GroupSize = this.DeserializeInt32();
                this.GapSize = this.DeserializeInt32();
                this.EventCount = this.DeserializeInt32();

                this.ValidNT = (FamosFileValidNTType)this.DeserializeInt32();
                this.ValidCD = (FamosFileValidCDType)this.DeserializeInt32();
                this.ValidCR1 = (FamosFileValidCR1Type)this.DeserializeInt32();
                this.ValidCR2 = (FamosFileValidCR2Type)this.DeserializeInt32();
            });
        }

        #endregion

        #region Properties

        public int FirstEventIndex { get; set; }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected offset >= '0', got '{value}'.");

                _offset = value;
            }
        }

        public int GroupSize
        {
            get { return _groupSize; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected group size >= '1', got '{value}'.");

                _groupSize = value;
            }
        }

        public int GapSize
        {
            get { return _gapSize; }
            set
            {
                if (!(0 <= value && value <= 1000))
                    throw new FormatException($"Expected gap size '0..1000', got '{value}'.");

                _gapSize = value;
            }
        }

        public int EventCount
        {
            get { return _eventCount; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected offset >= '0', got '{value}'.");

                _eventCount = value;
            }
        }

        public FamosFileValidNTType ValidNT { get; set; }
        public FamosFileValidCDType ValidCD { get; set; }
        public FamosFileValidCR1Type ValidCR1 { get; set; }
        public FamosFileValidCR2Type ValidCR2 { get; set; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.Cv;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var data = new object[]
            {
#warning TODO: Check if this event index is related to EventInfo index. Then check general usage of these indices.
                this.FirstEventIndex,
                this.Offset,
                this.GroupSize,
                this.GapSize,
                this.EventCount,

                (int)this.ValidNT,
                (int)this.ValidCD,
                (int)this.ValidCR1,
                (int)this.ValidCR2
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
