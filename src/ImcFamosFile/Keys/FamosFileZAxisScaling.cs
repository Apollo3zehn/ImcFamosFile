using System;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains information to scale the z-axis.
    /// </summary>
    public class FamosFileZAxisScaling : FamosFileBaseExtended
    {
        #region Fields

        private decimal _deltaZ;
        private int _segmentSize;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileZAxisScaling"/> class.
        /// </summary>
        /// <param name="deltaZ">The distance between two segments.</param>
        public FamosFileZAxisScaling(decimal deltaZ)
        {
            this.DeltaZ = deltaZ;
        }

        internal FamosFileZAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.DeltaZ = this.DeserializeReal();
                this.IsDeltaZCalibrated = this.DeserializeInt32() == 1;

                this.Z0 = this.DeserializeReal();
                this.IsZ0Calibrated = this.DeserializeInt32() == 1;

                this.Unit = this.DeserializeString();
                this.SegmentSize = this.DeserializeInt32();
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the distance between two segments.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a boolean which indicates if DeltaZ is calibrated.
        /// </summary>
        public bool IsDeltaZCalibrated { get; set; }

        /// <summary>
        /// Gets or sets the Z0, i.e. the z-coordinate of the first segment.
        /// </summary>
        public decimal Z0 { get; set; }

        /// <summary>
        /// Gets or sets a boolean which indicates if Z0 is calibrated.
        /// </summary>
        public bool IsZ0Calibrated { get; set; }

        /// <summary>
        /// Gets or sets unit of this axis.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of samples per segment.
        /// </summary>
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

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CZ;

        #endregion

        #region Methods

        internal FamosFileZAxisScaling Clone()
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
                this.IsDeltaZCalibrated ? 1 : 0,
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
