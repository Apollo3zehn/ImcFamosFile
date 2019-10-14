using System.IO;

namespace ImcFamosFile
{
    public class FamosFileCustomKey : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileCustomKey()
        {
            //
        }

        internal FamosFileCustomKey(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.Key = this.DeserializeString();
                this.Value = this.DeserializeKeyPart();
            });
        }

        #endregion

        #region Properties

        public string Key { get; set; } = string.Empty;
        public byte[] Value { get; set; } = new byte[0];
        protected override FamosFileKeyType KeyType => FamosFileKeyType.NU;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var data = new object[]
            {
                this.Key,
                this.Value
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
