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
        /// Gets or sets the event offset. REMARKS: Expressed as relative number of samples. E.g., expressed as Tuple-count.
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// Gets or sets the length of the event. REMARKS: Expressed as relative number of samples.
        /// </summary>
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

            using var binaryWriter = new BinaryWriter(stream);
            var offsetLo = (uint)(Offset & 0x00000000FFFFFFFF);
            var offsetHi = (uint)(Offset >> 32);

            var lengthLo = (uint)(Length & 0x00000000FFFFFFFF);
            var lengthHi = (uint)(Length >> 32);

            binaryWriter.Write(offsetLo);
            binaryWriter.Write(lengthLo);
            binaryWriter.Write(Time);
            binaryWriter.Write(AmplitudeOffset0);
            binaryWriter.Write(AmplitudeOffset1);
            binaryWriter.Write(X0);
            binaryWriter.Write(AmplificationFactor0);
            binaryWriter.Write(AmplificationFactor1);
            binaryWriter.Write(DeltaX);
            binaryWriter.Write(offsetHi);
            binaryWriter.Write(lengthHi);

            return new object[] { Index, stream.ToArray() };
        }

        #endregion
    }
}
