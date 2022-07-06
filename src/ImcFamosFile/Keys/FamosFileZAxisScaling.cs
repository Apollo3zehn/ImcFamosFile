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
            DeltaZ = deltaZ;
        }

        internal FamosFileZAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                DeltaZ = DeserializeReal();
                IsDeltaZCalibrated = DeserializeInt32() == 1;

                Z0 = DeserializeReal();
                IsZ0Calibrated = DeserializeInt32() == 1;

                Unit = DeserializeString();
                SegmentSize = DeserializeInt32();
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

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = (FamosFileZAxisScaling)obj;

            if (other == null)
                return false;

            return DeltaZ.Equals(other.DeltaZ)
                && IsDeltaZCalibrated.Equals(other.IsDeltaZCalibrated)
                && Z0.Equals(other.Z0)
                && IsZ0Calibrated.Equals(other.IsZ0Calibrated)
                && Unit.Equals(other.Unit)
                && SegmentSize.Equals(other.SegmentSize);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(DeltaZ, IsDeltaZCalibrated, Z0, IsZ0Calibrated, Unit, SegmentSize);
        }

        internal FamosFileZAxisScaling Clone()
        {
            return (FamosFileZAxisScaling)MemberwiseClone();
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                DeltaZ,
                IsDeltaZCalibrated ? 1 : 0,
                Z0,
                IsZ0Calibrated ? 1 : 0,
                Unit.Length, Unit,
                SegmentSize
            };

            SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
