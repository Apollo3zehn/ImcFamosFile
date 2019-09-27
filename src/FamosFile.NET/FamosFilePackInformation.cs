namespace FamosFile.NET
{
    public class FamosFilePackInformation
    {
        #region Properties

        public int BufferReference { get; set; }
        public int ByteCountPerValue { get; set; }
        public FamosFileDataType DataType { get; set; }
        public int SignificantBitsCount { get; set; }
        public int FirstValueOffset { get; set; }
        public int SubsequentValueCount { get; set; }
        public int ValueDistanceByteCount { get; set; }

        #endregion
    }
}
