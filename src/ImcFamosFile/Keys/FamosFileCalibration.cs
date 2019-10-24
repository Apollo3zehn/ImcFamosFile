using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains calibration data.
    /// </summary>
    public class FamosFileCalibration : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileCalibration"/> class.
        /// </summary>
        public FamosFileCalibration()
        {
            //
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileCalibration"/> class.
        /// </summary>
        /// <param name="applyTransformation">If set to true, FAMOS will apply the provided <paramref name="factor"/> and <paramref name="offset"/> to the data (only for integer raw data).</param>
        /// <param name="factor">The calibration factor.</param>
        /// <param name="offset">The calibration offset.</param>
        /// <param name="isCalibrated">Indicates if the scale is calibrated.</param>
        /// <param name="unit">Specifies the unit of this axis.</param>
        public FamosFileCalibration(bool applyTransformation, decimal factor, decimal offset, bool isCalibrated, string unit)
        {
            this.ApplyTransformation = applyTransformation;
            this.Factor = factor;
            this.Offset = offset;
            this.IsCalibrated = isCalibrated;
            this.Unit = unit;
        }

        internal FamosFileCalibration(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                ApplyTransformation = this.DeserializeInt32() == 1;
                Factor = this.DeserializeReal();
                Offset = this.DeserializeReal();
                IsCalibrated = this.DeserializeInt32() == 1;
                Unit = this.DeserializeString();
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating if FAMOS should apply the provided factor and offset values. Only for integer raw data.
        /// </summary>
        public bool ApplyTransformation { get; set; }

        /// <summary>
        /// Gets or sets the calibration factor.
        /// </summary>
        public decimal Factor { get; set; } = 1;

        /// <summary>
        /// Gets or sets the calibration offset.
        /// </summary>
        public decimal Offset { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating if the scale is calibrated.
        /// </summary>
        public bool IsCalibrated { get; set; }

        /// <summary>
        /// Gets or sets the unit of this axis.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        [HideFromApi]
        internal protected override FamosFileKeyType KeyType => FamosFileKeyType.CR;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.ApplyTransformation ? 1 : 0,
                this.Factor,
                this.Offset,
                this.IsCalibrated ? 1 : 0,
                this.Unit.Length, this.Unit
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
