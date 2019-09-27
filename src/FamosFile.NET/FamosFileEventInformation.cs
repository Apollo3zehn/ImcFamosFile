namespace FamosFile.NET
{
    public class FamosFileEventInformation
    {
        #region Properties

        public int IndexEventListKey { get; set; }
        public int OffsetInEventList { get; set; }
        public int SubsequentEventCount { get; set; }
        public int EventDistance { get; set; }
        public int EventCount { get; set; }
        public FamosFileValidNTType ValidNT { get; set; }
        public FamosFileValidCDType ValidCD { get; set; }
        public FamosFileValidCR1Type ValidCR1 { get; set; }
        public FamosFileValidCR2Type ValidCR2 { get; set; }

        #endregion
    }
}
