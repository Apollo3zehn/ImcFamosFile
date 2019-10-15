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
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CR;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
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
