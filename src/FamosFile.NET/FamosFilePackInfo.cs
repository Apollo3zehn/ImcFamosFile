namespace FamosFile.NET
{
    public class FamosFilePackInfo
    {
        #region Properties

        public int BufferReference { get; set; }
        public int ValueSize { get; set; }
        public FamosFileDataType DataType { get; set; }
        public int SignificantBits { get; set; }
        public int Offset { get; set; }
        public int GroupSize { get; set; }
        public int GapSize { get; set; }

        #endregion
    }
}
