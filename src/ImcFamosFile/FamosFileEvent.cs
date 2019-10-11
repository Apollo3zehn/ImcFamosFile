using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileEvent : FamosFileBase
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        public FamosFileEvent()
        {
            //
        }

        internal FamosFileEvent(BinaryReader reader) : base(reader)
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
            this.Index = index;

            this.Offset = offset;
            this.Length = length;
            this.Time = time;
            this.AmplitudeOffset0 = amplitudeOffset0;
            this.AmplitudeOffset1 = amplitudeOffset1;
            this.x0 = x0;
            this.AmplificationFactor0 = amplificationFactor0;
            this.AmplificationFactor1 = amplificationFactor1;
            this.dx = dx;
        }

        #endregion

        #region Properties

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

        public ulong Offset { get; set; }
        public ulong Length { get; set; }
        public double Time { get; set; }
        public double AmplitudeOffset0 { get; set; }
        public double AmplitudeOffset1 { get; set; }
        public double x0 { get; set; }
        public double AmplificationFactor0 { get; set; }
        public double AmplificationFactor1 { get; set; }
        public double dx { get; set; }

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
