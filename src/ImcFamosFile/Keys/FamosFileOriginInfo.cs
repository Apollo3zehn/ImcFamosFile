namespace ImcFamosFile
{
    /// <summary>
    /// Contains information about the data origin.
    /// </summary>
    public class FamosFileOriginInfo : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileOriginInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the file producer (author or manufacturer).</param>
        /// <param name="origin">The data origin.</param>
        public FamosFileOriginInfo(string name, FamosFileOrigin origin)
        {
            Name = name;
            Origin = origin;
        }

        internal FamosFileOriginInfo(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                Origin = (FamosFileOrigin)DeserializeInt32();
                Name = DeserializeString();
                Comment = DeserializeString();
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
        public FamosFileOrigin Origin { get; set; } = FamosFileOrigin.Original;

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.NO;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                (int)Origin,
                Name.Length, Name,
                Comment.Length, Comment
            };

            SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
