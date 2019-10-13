using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileEvent
    {
        #region Fields

        private int _index;

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

        #region Methods

        internal byte[] GetEventData()
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            var offsetLo = this.Offset & 0x00000000FFFFFFFF;
            var offsetHi = this.Offset >> 32;

            var lengthLo = this.Length & 0x00000000FFFFFFFF;
            var lengthHi = this.Length >> 32;

            binaryWriter.Write(offsetLo);
            binaryWriter.Write(lengthLo);
            binaryWriter.Write(this.Time);
            binaryWriter.Write(this.AmplitudeOffset0);
            binaryWriter.Write(this.AmplitudeOffset1);
            binaryWriter.Write(this.x0);
            binaryWriter.Write(this.AmplificationFactor0);
            binaryWriter.Write(this.AmplificationFactor1);
            binaryWriter.Write(this.dx);
            binaryWriter.Write(offsetHi);
            binaryWriter.Write(lengthHi);

            return stream.ToArray();
        }

        #endregion
    }
}
