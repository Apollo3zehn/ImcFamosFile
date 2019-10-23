using System.IO;

namespace ImcFamosFile
{
    public class FamosFileEvent
    {
        #region Properties

        public int Index { get; set; }
        public ulong Offset { get; set; }
        public ulong Length { get; set; }
        public double Time { get; set; }
        public double AmplitudeOffset0 { get; set; }
        public double AmplitudeOffset1 { get; set; }
        public double X0 { get; set; }
        public double AmplificationFactor0 { get; set; }
        public double AmplificationFactor1 { get; set; }
        public double DeltaX { get; set; }

        #endregion

        #region Methods

        internal object[] GetEventData()
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            var offsetLo = (uint)(this.Offset & 0x00000000FFFFFFFF);
            var offsetHi = (uint)(this.Offset >> 32);

            var lengthLo = (uint)(this.Length & 0x00000000FFFFFFFF);
            var lengthHi = (uint)(this.Length >> 32);

            binaryWriter.Write(offsetLo);
            binaryWriter.Write(lengthLo);
            binaryWriter.Write(this.Time);
            binaryWriter.Write(this.AmplitudeOffset0);
            binaryWriter.Write(this.AmplitudeOffset1);
            binaryWriter.Write(this.X0);
            binaryWriter.Write(this.AmplificationFactor0);
            binaryWriter.Write(this.AmplificationFactor1);
            binaryWriter.Write(this.DeltaX);
            binaryWriter.Write(offsetHi);
            binaryWriter.Write(lengthHi);

            return new object[] { this.Index, stream.ToArray() };
        }

        #endregion
    }
}
