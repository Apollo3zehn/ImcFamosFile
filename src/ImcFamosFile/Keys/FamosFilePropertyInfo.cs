using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
            this.Properties.AddRange(properties);
        }

        internal FamosFilePropertyInfo(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var startPosition = this.Reader.BaseStream.Position;

                while (this.Reader.BaseStream.Position - startPosition < keySize)
                {
                    var property = this.DeserializeProperty();
                    this.Properties.Add(property);
                }
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of properties.
        /// </summary>
        public List<FamosFileProperty> Properties { get; } = new List<FamosFileProperty>();

        [HideFromApi]
        internal protected override FamosFileKeyType KeyType => FamosFileKeyType.Np;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Regex MatchProperty { get; } = new Regex("\"(.*?)\"\\s\"(.*?)\"\\s([0-9])\\s([0-9])");

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var propertyData = new List<object>();

            foreach (var property in this.Properties)
            {
                propertyData.AddRange(property.GetPropertyData());
            }

            this.SerializeKey(writer, 1, propertyData.ToArray());
        }

        private FamosFileProperty DeserializeProperty()
        {
            var bytes = this.DeserializeKeyPart();
            var rawValue = Encoding.GetEncoding(this.CodePage).GetString(bytes);
            var result = this.MatchProperty.Match(rawValue);

            var name = result.Groups[1].Value;
            var value = result.Groups[2].Value;
            var type = (FamosFilePropertyType)int.Parse(result.Groups[3].Value);
            var flags = (FamosFilePropertyFlags)int.Parse(result.Groups[4].Value);

            return new FamosFileProperty(name, value, type, flags);
        }

        #endregion
    }
}