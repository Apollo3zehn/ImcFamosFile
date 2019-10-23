using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileZAxisScaling : FamosFileBaseExtended
    {
        #region Fields

        private decimal _deltaZ;
        private int _segmentSize;

        #endregion

        #region Constructors

        public FamosFileZAxisScaling(decimal deltaZ)
        {
            this.DeltaZ = deltaZ;
        }

        internal FamosFileZAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.DeltaZ = this.DeserializeReal();
                this.IsDzCalibrated = this.DeserializeInt32() == 1;

                this.Z0 = this.DeserializeReal();
                this.IsZ0Calibrated = this.DeserializeInt32() == 1;

                this.Unit = this.DeserializeString();
                this.SegmentSize = this.DeserializeInt32();
            });
        }

        #endregion

        #region Properties

        public decimal DeltaZ
        {
            get { return _deltaZ; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected deltaZ value > '0', got '{value}'.");

                _deltaZ = value;
            }
        }

        public bool IsDzCalibrated { get; set; }
        public decimal Z0 { get; set; }
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

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CZ;

        #endregion

        #region Methods

        public FamosFileZAxisScaling Clone()
        {
            return (FamosFileZAxisScaling)this.MemberwiseClone();
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.DeltaZ,
                this.IsDzCalibrated ? 1 : 0,
                this.Z0,
                this.IsZ0Calibrated ? 1 : 0,
                this.Unit.Length, this.Unit,
                this.SegmentSize
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
