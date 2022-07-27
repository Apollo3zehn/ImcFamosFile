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
            DeltaX = deltaX;
        }

        internal FamosFileXAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            var keyVersion = DeserializeInt32();

            if (keyVersion == 1)
            {
                DeserializeKey(keySize =>
                {
                    DeltaX = DeserializeReal();
                    IsCalibrated = DeserializeInt32() == 1;
                    Unit = DeserializeString();

                    Reduction = (FamosFileReductionType)DeserializeInt32();
                    IsMultiEvents = DeserializeInt32() == 1 ? true : false;
                    SortBuffers = DeserializeInt32() == 1 ? true : false;
                });
            }
            else if (keyVersion == 2)
            {
                DeserializeKey(keySize =>
                {
                    DeltaX = DeserializeReal();
                    IsCalibrated = DeserializeInt32() == 1;
                    Unit = DeserializeString();

                    Reduction = (FamosFileReductionType)DeserializeInt32();
                    IsMultiEvents = DeserializeInt32() == 1 ? true : false;
                    SortBuffers = DeserializeInt32() == 1 ? true : false;

                    X0 = DeserializeReal();
                    PretriggerUsage = (FamosFilePretriggerUsage)DeserializeInt32();
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

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CD;

        #endregion

        #region Methods

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = (FamosFileXAxisScaling)obj;

            if (other == null)
                return false;

            return DeltaX.Equals(other.DeltaX)
                && IsCalibrated.Equals(other.IsCalibrated)
                && Unit.Equals(other.Unit)
                && Reduction.Equals(other.Reduction)
                && IsMultiEvents.Equals(other.IsMultiEvents)
                && SortBuffers.Equals(other.SortBuffers)
                && X0.Equals(other.X0)
                && PretriggerUsage.Equals(other.PretriggerUsage);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(DeltaX, IsCalibrated, Unit, Reduction, IsMultiEvents, SortBuffers, X0, PretriggerUsage);
        }

        internal FamosFileXAxisScaling Clone()
        {
            return (FamosFileXAxisScaling)MemberwiseClone();
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                DeltaX,
                IsCalibrated ? 1 : 0,
                Unit.Length, Unit,
                (int)Reduction,
                IsMultiEvents ? 1 : 0,
                SortBuffers ? 1 : 0,
                X0,
                (int)PretriggerUsage
            };

            SerializeKey(writer, 2, data);
        }

        #endregion
    }
}
