namespace FamosFile.NET
{
    public class FamosFileXAxisScaling
    {
        public FamosFileXAxisScaling(int dz, bool isCalibrated, string unit)
        {
            this.dz = dz;
            this.IsCalibrated = isCalibrated;
            this.Unit = unit;
        }

        public int dz { get; private set; }
        public bool IsCalibrated { get; private set; }
        public string Unit { get; private set; }
    }
}
