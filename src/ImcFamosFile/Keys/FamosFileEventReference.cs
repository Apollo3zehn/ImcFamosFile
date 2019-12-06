using System;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Represents a description of which events belong to the component.
    /// </summary>
    public class FamosFileEventReference : FamosFileBase
    {
        #region Fields

        private int _eventInfoIndex;
        private int _offset;
        private int _groupSize = 1;
        private int _gapSize;
        private int _eventCount;

        private FamosFileEventInfo? _eventInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileEventReference"/> class.
        /// </summary>
        public FamosFileEventReference()
        {
            //
        }

        internal FamosFileEventReference(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.EventInfoIndex = this.DeserializeInt32();
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

        /// <summary>
        /// Gets or sets the associated event info containing the actual events.
        /// </summary>
        public FamosFileEventInfo EventInfo
        {
            get
            {
                if (_eventInfo is null)
                    throw new FormatException("An event info instance must be assigned to the event location info's event info property.");

                return _eventInfo;
            }
            set { _eventInfo = value; }
        }

        /// <summary>
        /// Gets or sets the offset of the first event in the event info.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the number of subsequent events in the event info.
        /// </summary>
        public int GroupSize
        {
            get { return _groupSize; }
            set
            {
                if (value != 1)
                    throw new FormatException($"Expected group size = '1', got '{value}'.");

                _groupSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of events to skip.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the total number of events.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating where the trigger time is taken from.
        /// </summary>
        public FamosFileValidNTType ValidNT { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating which data from the event list are used (DeltaX, X0, Z0).
        /// </summary>
        public FamosFileValidCDType ValidCD { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating which data from event list are used (DeltaY, Y0). For primary component.
        /// </summary>
        public FamosFileValidCR1Type ValidCR1 { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating which data from event list are used (DeltaX, X0). For secondary component; is '0' for 1-component data.
        /// </summary>
        public FamosFileValidCR2Type ValidCR2 { get; set; }

        internal int EventInfoIndex
        {
            get { return _eventInfoIndex; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index value > '0', got '{value}'.");

                _eventInfoIndex = value;
            }
        }

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.Cv;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.EventInfoIndex,
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
