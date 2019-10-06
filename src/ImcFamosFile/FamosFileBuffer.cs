namespace ImcFamosFile
{
    public class FamosFileBuffer
    {
        #region Constructors

        public FamosFileBuffer(FamosFileRawData rawData)
        {
            this.RawData = rawData;
        }

        internal FamosFileBuffer(int reference, int rawDataReference)
        {
            this.Reference = reference;
            this.RawDataReference = rawDataReference;
        }

        #endregion

        #region Properties

        internal int Reference { get; private set; }
        internal int RawDataReference { get; private set; }

        public FamosFileRawData? RawData { get; set; }
        public int RawDataOffset { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
        public int ConsumedBytes { get; set; }
        public int x0 { get; set; }
        public int TriggerAddTime { get; set; }
        public byte[]? UserInfo { get; set; }
        public bool IsNewEvent { get; set; }

        #endregion
    }
}
