using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileEventInfo : FamosFileBase
    {
        #region Constructors

        public FamosFileEventInfo()
        {
            //
        }

        internal FamosFileEventInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var eventCount = this.DeserializeInt32();

                for (int i = 0; i < eventCount; i++)
                {
                    this.Events.Add(this.DeserializeEvent());
                }
            });
        }

        #endregion

        #region Properties

        public List<FamosFileEvent> Events { get; private set; } = new List<FamosFileEvent>();
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CV;

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
#warning TODO: Since events are not associated to any event location info, the indices should not be modified.
            // update event indices
            foreach (var @event in this.Events)
            {
                @event.Index = this.Events.IndexOf(@event) + 1;
            }
        }

        internal override void Serialize(StreamWriter writer)
        {
            var eventData = new List<object>
            {
                this.Events.Count
            };

            foreach (var @event in this.Events)
            {
                eventData.Add(@event.Index);
                eventData.Add(@event.GetEventData());
            }

            this.SerializeKey(writer, 1, eventData.ToArray());
        }

        internal override void AfterDeserialize()
        {
            // check if event indices are consistent
            foreach (var @event in this.Events)
            {
                var expected = @event.Index;
                var actual = this.Events.IndexOf(@event) + 1;

                if (expected != actual)
                    throw new FormatException($"The event indices are not consistent. Expected '{expected}', got '{actual}'.");
            }

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
            var dx = this.Reader.ReadDouble();
            var offsetHi = this.Reader.ReadUInt32();
            var lengthHi = this.Reader.ReadUInt32();

            var offset = offsetLo + (offsetHi << 32);
            var length = lengthLo + (lengthHi << 32);

            // assign properties
            return new FamosFileEvent()
            {
                Index = index,
                Offset = offset,
                Length = length,
                Time = time,
                AmplitudeOffset0 = amplitudeOffset0,
                AmplitudeOffset1 = amplitudeOffset1,
                x0 = x0,
                AmplificationFactor0 = amplificationFactor0,
                AmplificationFactor1 = amplificationFactor1,
                dx = dx
            };
        }

        #endregion
    }
}
