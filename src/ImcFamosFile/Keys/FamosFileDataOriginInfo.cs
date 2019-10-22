using System.IO;

namespace ImcFamosFile
{
    public class FamosFileDataOriginInfo : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileDataOriginInfo(string name, FamosFileDataOrigin origin)
        {
            this.Name = name;
            this.DataOrigin = origin;
        }

        internal FamosFileDataOriginInfo(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.DataOrigin = (FamosFileDataOrigin)this.DeserializeInt32();
                this.Name = this.DeserializeString();
                this.Comment = this.DeserializeString();
            });
        }

        #endregion

        #region Properties

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public FamosFileDataOrigin DataOrigin { get; set; } = FamosFileDataOrigin.Original;
        protected override FamosFileKeyType KeyType => FamosFileKeyType.NO;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                (int)this.DataOrigin,
                this.Name.Length, this.Name,
                this.Comment.Length, this.Comment
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
