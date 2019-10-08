using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileZAxisScaling : FamosFileBaseExtended
    {
        #region Fields

        private double _dz;
        private int _segmentSize;

        #endregion

        #region Constructors

        public FamosFileZAxisScaling()
        {
            //
        }

        internal FamosFileZAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.dz = this.DeserializeFloat64();
                this.IsDzCalibrated = this.DeserializeInt32() == 1;

                this.z0 = this.DeserializeFloat64();
                this.IsZ0Calibrated = this.DeserializeInt32() == 1;

                this.Unit = this.DeserializeString();
                this.SegmentSize = this.DeserializeInt32();
            });
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
        public string Unit { get; set; } = string.Empty;

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
