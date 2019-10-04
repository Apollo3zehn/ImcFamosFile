namespace FamosFile.NET
{
    public class FamosFileSingleValue
    {
        #region Constructors

        public FamosFileSingleValue(double value)
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
        public double Value { get; set; }
        public string Unit { get; set; }
        public string Comment { get; set; }
        public double Time { get; set; }

        #endregion
    }
}
