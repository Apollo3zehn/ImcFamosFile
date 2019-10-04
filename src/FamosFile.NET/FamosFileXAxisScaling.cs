namespace FamosFile.NET
{
    public class FamosFileXAxisScaling
    {
        public FamosFileXAxisScaling(double dx, bool isCalibrated, string unit, double x0 = 0, FamosFilePretriggerUsage pretriggerUsage = FamosFilePretriggerUsage.NoPretrigger)
        {
            this.dx = dx;
            this.IsCalibrated = isCalibrated;
            this.Unit = unit;
            this.x0 = x0;
            this.PretriggerUsage = pretriggerUsage;
        }

        public double dx { get; private set; }
        public bool IsCalibrated { get; private set; }
        public string Unit { get; private set; }
        public double x0 { get; private set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; private set; }
    }
}
