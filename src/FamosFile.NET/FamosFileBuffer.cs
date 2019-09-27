namespace FamosFile.NET
{
    public class FamosFileBuffer
    {
        #region Properties

        public int BufferReference { get; set; }
        public int IndexSampleKey { get; set; }
        public int OffsetBufferInSamplesKey { get; set; }
        public int BufferSize { get; set; }
        public int OffsetFirstSampleInBuffer { get; set; }
        public int BufferFilledBytes { get; set; }
        public int x0 { get; set; }
        public int AddTime { get; set; }
        public int UserInformation { get; set; }
        public bool NewEvent { get; set; }

        #endregion
    }
}
