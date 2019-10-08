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
                    this.dx = this.DeserializeFloat64();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    this.DeserializeInt32();
                    this.DeserializeInt32();
                    this.DeserializeInt32();
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    this.dx = this.DeserializeInt32();
                    this.IsCalibrated = this.DeserializeInt32() == 1;
                    this.Unit = this.DeserializeString();

                    // some data is not defined in imc document.
                    this.DeserializeKeyPart();
                    this.DeserializeKeyPart();
                    this.DeserializeKeyPart();

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

        public double dx { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double x0 { get; set; }
        public FamosFilePretriggerUsage PretriggerUsage { get; set; }

        #endregion
    }
}
