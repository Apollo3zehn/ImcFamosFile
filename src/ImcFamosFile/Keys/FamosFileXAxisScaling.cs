using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileXAxisScaling : FamosFileBaseExtended
    {
        #region Fields

        private decimal _deltaX;

        #endregion

        #region Constructors

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

        public bool IsCalibrated { get; set; }
        public string Unit { get; set; } = string.Empty;

        public FamosFileReductionType Reduction { get; set; }
        public bool IsMultiEvents { get; set; }
        public bool SortBuffers { get; set; }

        public decimal X0 { get; set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; set; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CD;

        #endregion

        #region Methods

        public FamosFileXAxisScaling Clone()
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
