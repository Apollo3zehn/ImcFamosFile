namespace ImcFamosFile
{
    public class FamosFileXAxisScaling
    {
        #region Constructors

        public FamosFileXAxisScaling()
        {
            this.Unit = string.Empty;
        }

        #endregion

        #region Properties

        public double dx { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; }
        public double x0 { get; set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; set; }

        #endregion
    }
}
