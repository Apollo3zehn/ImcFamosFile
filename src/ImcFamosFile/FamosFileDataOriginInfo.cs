using System.IO;

namespace ImcFamosFile
{
    public class FamosFileDataOriginInfo : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileDataOriginInfo()
        {
            //
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

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var data = string.Join(',', new object[]
            {
                (int)this.DataOrigin,
                this.Name.Length, this.Name,
                this.Comment.Length, this.Comment
            });

            this.SerializeKey(writer, FamosFileKeyType.NO, 1, data);
        }

        #endregion
    }
}
