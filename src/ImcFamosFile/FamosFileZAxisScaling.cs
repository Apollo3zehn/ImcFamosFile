using System;

namespace ImcFamosFile
{
    public class FamosFileZAxisScaling
    {
        #region Fields

        private double _dz;
        private int _segmentSize;

        #endregion

        #region Constructors

        public FamosFileZAxisScaling()
        {
            this.Unit = string.Empty;
        }

        #endregion

        #region Properties

        public double dz
        {
            get { return _dz; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected dz value > '0', got '{value}'.");

                _dz = value;
            }
        }

        public bool IsDzCalibrated { get; set; }
        public double z0 { get; set; }
        public bool IsZ0Calibrated { get; set; }
        public string Unit { get; set; }

        public int SegmentSize
        {
            get { return _segmentSize; }
            set
            {
                if (value < 0)
                    throw new 
                        FormatException($"Expected segment size value >= '0', got '{value}'.");

                _segmentSize = value;
            }
        }

        #endregion
    }
}
