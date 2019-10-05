namespace ImcFamosFile
{
    public class FamosFileBuffer
    {
        #region Properties

        public int Reference { get; set; }
        public int CsKeyReference { get; set; }
        public int CsKeyOffset { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
        public int ConsumedBytes { get; set; }
        public int x0 { get; set; }
        public int TriggerAddTime { get; set; }
        public byte[] UserInfo { get; set; }
        public bool IsNewEvent { get; set; }

        #endregion
    }
}
