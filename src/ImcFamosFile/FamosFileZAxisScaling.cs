namespace ImcFamosFile
{
    public class FamosFileZAxisScaling
    {
        #region Constructors

        public FamosFileZAxisScaling()
        {
            this.Unit = string.Empty;
        }

        public FamosFileZAxisScaling(double dz, bool isDzCalibrated, double z0, bool isZ0Calibrated, string unit, int segmentSize) : this()
        {
            this.dz = dz;
            this.IsDzCalibrated = isDzCalibrated;
            this.z0 = z0;
            this.isZ0Calibrated = isZ0Calibrated;
            this.Unit = unit;
            this.SegmentSize = segmentSize;
        }

        #endregion

        #region Properties

        public double dz { get; private set; }
        public bool IsDzCalibrated { get; private set; }
        public double z0 { get; private set; }
        public bool isZ0Calibrated { get; private set; }
        public string Unit { get; private set; }
        public int SegmentSize { get; private set; }

        #endregion
    }
}
