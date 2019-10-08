using System.IO;

namespace ImcFamosFile
{
    public class FamosFileCalibrationInfo : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileCalibrationInfo()
        {
            //
        }

        public FamosFileCalibrationInfo(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                ApplyTransformation = this.DeserializeInt32() == 1;
                Factor = this.DeserializeFloat64();
                Offset = this.DeserializeFloat64();
                IsCalibrated = this.DeserializeInt32() == 1;
                Unit = this.DeserializeString();
            });
        }

        #endregion

        #region Properties

        public bool ApplyTransformation { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public bool IsCalibrated { get; set; }
        public string Unit { get; set; } = string.Empty;

        #endregion
    }
}
