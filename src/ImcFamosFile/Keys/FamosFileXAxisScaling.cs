using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileXAxisScaling : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileXAxisScaling()
        {
            //
        }

        internal FamosFileXAxisScaling(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    this.dx = this.DeserializeReal();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    this.Reduction = this.DeserializeInt32();
                    this.IsMultiEvents = this.DeserializeInt32();
                    this.SortBuffers = this.DeserializeInt32();
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    this.dx = this.DeserializeInt32();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    this.Reduction = this.DeserializeInt32();
                    this.IsMultiEvents = this.DeserializeInt32();
                    this.SortBuffers = this.DeserializeInt32();

                    this.x0 = this.DeserializeInt32();
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

        public decimal dx { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; } = string.Empty;

        private int Reduction { get; set; }
        private int IsMultiEvents { get; set; }
        private int SortBuffers { get; set; }

        public decimal x0 { get; set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; set; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CD;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.dx,
                this.IsCalibrated ? 1 : 0,
                this.Unit.Length, this.Unit,
                this.Reduction,
                this.IsMultiEvents,
                this.SortBuffers,
                this.x0,
                (int)this.PretriggerUsage
            };

            this.SerializeKey(writer, 2, data);
        }

        #endregion
    }
}
