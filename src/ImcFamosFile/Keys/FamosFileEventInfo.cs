namespace ImcFamosFile
{
    /// <summary>
    /// Contains a list of events.
    /// </summary>
    public class FamosFileEventInfo : FamosFileBase
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileEventInfo"/> class.
        /// </summary>
        public FamosFileEventInfo()
        {
            //
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileEventInfo"/> class.
        /// </summary>
        /// <param name="events">A list of events.</param>
        public FamosFileEventInfo(List<FamosFileEvent> events)
        {
            Events.AddRange(events);
        }

        internal FamosFileEventInfo(BinaryReader reader) : base(reader)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                Index = DeserializeInt32();

                var eventCount = DeserializeInt32();

                for (int i = 0; i < eventCount; i++)
                {
                    Events.Add(DeserializeEvent());
                }
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of events.
        /// </summary>
        public List<FamosFileEvent> Events { get; private set; } = new List<FamosFileEvent>();

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CV;

        internal int Index
        {
            get { return _index; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index > '0', got '{value}'.");

                _index = value;
            }
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var eventData = new List<object>
            {
                Index,
                Events.Count,
            };

            foreach (var @event in Events)
            {
                eventData.AddRange(@event.GetEventData());
            }

            SerializeKey(writer, 1, eventData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            // check if event indices are consistent
            base.CheckIndexConsistency("event", Events, current => current.Index);
            Events = Events.OrderBy(x => x.Index).ToList();
        }

        private FamosFileEvent DeserializeEvent()
        {
            // read data
            var index = DeserializeInt32();
            var offsetLo = Reader.ReadUInt32();
            var lengthLo = Reader.ReadUInt32();
            var time = Reader.ReadDouble();
            var amplitudeOffset0 = Reader.ReadDouble();
            var amplitudeOffset1 = Reader.ReadDouble();
            var x0 = Reader.ReadDouble();
            var amplificationFactor0 = Reader.ReadDouble();
            var amplificationFactor1 = Reader.ReadDouble();
            var deltaX = Reader.ReadDouble();
            var offsetHi = Reader.ReadUInt32();
            var lengthHi = Reader.ReadUInt32();

            var offset = offsetLo + (offsetHi << 32);
            var length = lengthLo + (lengthHi << 32);

            // read comma or semicolon
            Reader.ReadByte();

            // assign properties
            return new FamosFileEvent()
            {
                Index = index,
                Offset = offset,
                Length = length,
                Time = time,
                AmplitudeOffset0 = amplitudeOffset0,
                AmplitudeOffset1 = amplitudeOffset1,
                X0 = x0,
                AmplificationFactor0 = amplificationFactor0,
                AmplificationFactor1 = amplificationFactor1,
                DeltaX = deltaX
            };
        }

        #endregion
    }
}
