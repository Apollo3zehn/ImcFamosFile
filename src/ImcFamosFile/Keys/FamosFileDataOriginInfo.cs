using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains information about the data origin.
    /// </summary>
    public class FamosFileDataOriginInfo : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileDataOriginInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the file producer (author or manufacturer).</param>
        /// <param name="origin">The data origin.</param>
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

        /// <summary>
        /// Gets or sets the name of the file producer (author or manufacturer).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data origin.
        /// </summary>
        public FamosFileDataOrigin DataOrigin { get; set; } = FamosFileDataOrigin.Original;

        [HideFromApi]
        internal protected override FamosFileKeyType KeyType => FamosFileKeyType.NO;

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
