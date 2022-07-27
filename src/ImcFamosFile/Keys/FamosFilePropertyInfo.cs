using System.Text;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains a list of properties.
    /// </summary>
    public class FamosFilePropertyInfo : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFilePropertyInfo"/> class.
        /// </summary>
        public FamosFilePropertyInfo()
        {
            //
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFilePropertyInfo"/> class.
        /// </summary>
        /// <param name="properties">A list of properties.</param>
        public FamosFilePropertyInfo(List<FamosFileProperty> properties)
        {
            Properties.AddRange(properties);
        }

        internal FamosFilePropertyInfo(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var startPosition = Reader.BaseStream.Position;

                while (Reader.BaseStream.Position - startPosition < keySize)
                {
                    var property = DeserializeProperty();
                    Properties.Add(property);
                }
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of properties.
        /// </summary>
        public List<FamosFileProperty> Properties { get; } = new List<FamosFileProperty>();

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.Np;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var propertyData = new List<object>();

            foreach (var property in Properties)
            {
                propertyData.AddRange(property.GetPropertyData());
            }

            SerializeKey(writer, 1, propertyData.ToArray());
        }

        private FamosFileProperty DeserializeProperty()
        {
            var name = Encoding
                .GetEncoding(CodePage)
                .GetString(DeserializePropertyKey());

            _ = Reader.ReadByte();

            var value = Encoding
                .GetEncoding(CodePage)
                .GetString(DeserializePropertyValue());

            var type = (FamosFilePropertyType)int.Parse(Encoding.ASCII.GetString(Reader.ReadBytes(2)));
            var flags = (FamosFilePropertyFlags)int.Parse(Encoding.ASCII.GetString(Reader.ReadBytes(2)));

            _ = Reader.ReadByte();

            return new FamosFileProperty(name, value, type, flags);
        }

        #endregion
    }
}