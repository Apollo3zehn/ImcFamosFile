namespace ImcFamosFile
{
    public class FamosFileXAxisScaling
    {
        #region Constructors

        public FamosFileXAxisScaling()
        {
            this.Unit = string.Empty;
        }

        public FamosFileXAxisScaling(double dx, bool isCalibrated, string unit, double x0 = 0, FamosFilePretriggerUsage pretriggerUsage = FamosFilePretriggerUsage.NoPretrigger) : this()
        {
            this.dx = dx;
            this.IsCalibrated = isCalibrated;
            this.Unit = unit;
            this.x0 = x0;
            this.PretriggerUsage = pretriggerUsage;
        }

        #endregion

        #region Properties

        public double dx { get; private set; }
        public bool IsCalibrated { get; private set; }
        public string Unit { get; private set; }
        public double x0 { get; private set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; private set; }

        #endregion
    }
}
