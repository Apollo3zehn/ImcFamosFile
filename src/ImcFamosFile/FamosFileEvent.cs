using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Describes a single event.
    /// </summary>
    public class FamosFileEvent
    {
        #region Properties

        /// <summary>
        /// Gets or sets the event index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the event offset.
        /// </summary>
        /// <remarks>Expressed as relative number of samples. E.g., expressed as Tuple-count.</remarks>
        public ulong Offset { get; set; }

        /// <summary>
        /// Gets or sets the length of the event.
        /// </summary>
        /// <remarks>Expressed as relative number of samples</remarks>
        public ulong Length { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets amplitude offset 1.
        /// </summary>
        public double AmplitudeOffset0 { get; set; }

        /// <summary>
        /// Gets or sets amplitude offset 2.
        /// </summary>
        public double AmplitudeOffset1 { get; set; }

        /// <summary>
        /// Gets or sets the X0.
        /// </summary>
        public double X0 { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor 1.
        /// </summary>
        public double AmplificationFactor0 { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor 2.
        /// </summary>
        public double AmplificationFactor1 { get; set; }

        /// <summary>
        /// Gets or sets the distance between two samples.
        /// </summary>
        public double DeltaX { get; set; }

        #endregion

        #region Methods

        internal object[] GetEventData()
        {
            var stream = new MemoryStream();

            using (var binaryWriter = new BinaryWriter(stream))
            {
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
        }

        #endregion
    }
}
