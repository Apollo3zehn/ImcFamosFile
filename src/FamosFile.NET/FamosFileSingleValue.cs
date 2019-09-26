namespace FamosFile.NET
{
    public class FamosFileSingleValue
    {
        #region Constructors

        public FamosFileSingleValue(byte[] value)
        {
            this.Value = value;

            this.Name = string.Empty;
            this.Unit = string.Empty;
            this.Comment = string.Empty;
        }

        #endregion

        #region Properties

        public FamosFileDataType DataType { get; set; }
        public string Name { get; set; }
        public byte[] Value { get; set; }
        public string Unit { get; set; }
        public string Comment { get; set; }
        public double Time { get; set; }

        #endregion
    }
}
