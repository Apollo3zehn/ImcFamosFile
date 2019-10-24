using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            this.Events.AddRange(events);
        }

        internal FamosFileEventInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.Index = this.DeserializeInt32();

                var eventCount = this.DeserializeInt32();

                for (int i = 0; i < eventCount; i++)
                {
                    this.Events.Add(this.DeserializeEvent());
                }
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of events.
        /// </summary>
        public List<FamosFileEvent> Events { get; private set; } = new List<FamosFileEvent>();

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CV;

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
                this.Index,
                this.Events.Count,
            };

            foreach (var @event in this.Events)
            {
                eventData.AddRange(@event.GetEventData());
            }

            this.SerializeKey(writer, 1, eventData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            // check if event indices are consistent
            base.CheckIndexConsistency("event", this.Events, current => current.Index);
            this.Events = this.Events.OrderBy(x => x.Index).ToList();
        }

        private FamosFileEvent DeserializeEvent()
        {
            // read data
            var index = this.DeserializeInt32();
            var offsetLo = this.Reader.ReadUInt32();
            var lengthLo = this.Reader.ReadUInt32();
            var time = this.Reader.ReadDouble();
            var amplitudeOffset0 = this.Reader.ReadDouble();
            var amplitudeOffset1 = this.Reader.ReadDouble();
            var x0 = this.Reader.ReadDouble();
            var amplificationFactor0 = this.Reader.ReadDouble();
            var amplificationFactor1 = this.Reader.ReadDouble();
            var deltaX = this.Reader.ReadDouble();
            var offsetHi = this.Reader.ReadUInt32();
            var lengthHi = this.Reader.ReadUInt32();

            var offset = offsetLo + (offsetHi << 32);
            var length = lengthLo + (lengthHi << 32);

            // read comma or semicolon
            this.Reader.ReadByte();

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
