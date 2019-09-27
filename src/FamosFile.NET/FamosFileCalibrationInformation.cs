namespace FamosFile.NET
{
    public class FamosFileCalibrationInformation
    {
        #region Properties

        public bool ApplyTransformation { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; }

        #endregion
    }
}
