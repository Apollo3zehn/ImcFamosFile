namespace FamosFile.NET
{
    public class FamosFileZAxisScaling
    {
        public FamosFileZAxisScaling(int dz, bool isDzCalibrated, int z0, bool isZ0Calibrated, string unit, int segmentSize)
        {
            this.dz = dz;
            this.IsDzCalibrated = isDzCalibrated;
            this.z0 = z0;
            this.isZ0Calibrated = isZ0Calibrated;
            this.Unit = unit;
            this.SegmentSize = segmentSize;
        }

        public int dz { get; private set; }
        public bool IsDzCalibrated { get; private set; }
        public int z0 { get; private set; }
        public bool isZ0Calibrated { get; private set; }
        public string Unit { get; private set; }
        public int SegmentSize { get; private set; }
    }
}
