using System;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains information to scale the x-axis.
    /// </summary>
    public class FamosFileXAxisScaling : FamosFileBaseExtended
    {
        #region Fields

        private decimal _deltaX;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileXAxisScaling"/> class.
        /// </summary>
        /// <param name="deltaX">The distance between two samples or the parameter.</param>
        public FamosFileXAxisScaling(decimal deltaX)
        {
            this.DeltaX = deltaX;
        }

        internal FamosFileXAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    this.DeltaX = this.DeserializeReal();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    this.Reduction = (FamosFileReductionType)this.DeserializeInt32();
                    this.IsMultiEvents = this.DeserializeInt32() == 1 ? true : false;
                    this.SortBuffers = this.DeserializeInt32() == 1 ? true : false;
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    this.DeltaX = this.DeserializeReal();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    this.Reduction = (FamosFileReductionType)this.DeserializeInt32();
                    this.IsMultiEvents = this.DeserializeInt32() == 1 ? true : false;
                    this.SortBuffers = this.DeserializeInt32() == 1 ? true : false;

                    this.X0 = this.DeserializeReal();
                    this.PretriggerUsage = (FamosFilePretriggerUsage)this.DeserializeInt32();
                });
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the distance between two samples or the parameter.
        /// </summary>
        public decimal DeltaX
        {
            get { return _deltaX; }
            set
            {
                if (value <= 0)
                    throw new FormatException($"Expected deltaX value > '0', got '{value}'.");

                _deltaX = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating if the axis is calibrated.
        /// </summary>
        public bool IsCalibrated { get; set; }

        /// <summary>
        /// Gets or sets the unit of this axis.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data reduction method.
        /// </summary>
        public FamosFileReductionType Reduction { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating if the data recording has been triggered multiple times, i.e. there should be event data defined.
        /// </summary>
        public bool IsMultiEvents { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating if the buffers of the component should be sorted. 
        /// </summary>
        public bool SortBuffers { get; set; }

        /// <summary>
        /// Gets or sets the X0, i.e. the x-coordinate of the first value for equistant data (pre-trigger time) or the starting frequency, respectively. REMARKS: Meaning depends on value of <see cref="PretriggerUsage"/> property.
        /// </summary>
        public decimal X0 { get; set; }

        /// <summary>
        /// Gets or sets the pretrigger usage.
        /// </summary>
        public FamosFilePretriggerUsage PretriggerUsage { get; set; }

        [HideFromApi]
        internal protected override FamosFileKeyType KeyType => FamosFileKeyType.CD;

        #endregion

        #region Methods

        internal FamosFileXAxisScaling Clone()
        {
            return (FamosFileXAxisScaling)this.MemberwiseClone();
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.DeltaX,
                this.IsCalibrated ? 1 : 0,
                this.Unit.Length, this.Unit,
                (int)this.Reduction,
                this.IsMultiEvents ? 1 : 0,
                this.SortBuffers ? 1 : 0,
                this.X0,
                (int)this.PretriggerUsage
            };

            this.SerializeKey(writer, 2, data);
        }

        #endregion
    }
}
