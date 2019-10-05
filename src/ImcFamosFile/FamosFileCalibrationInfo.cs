namespace ImcFamosFile
{
    public class FamosFileCalibrationInfo
    {
        #region Constructors

        public FamosFileCalibrationInfo()
        {
            this.Unit = string.Empty;
        }

        #endregion

        #region Properties

        public bool ApplyTransformation { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; }

        #endregion
    }
}
