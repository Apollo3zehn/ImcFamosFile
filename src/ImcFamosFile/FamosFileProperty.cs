using System;
using System.Globalization;

namespace ImcFamosFile
{
    public class FamosFileProperty
    {
        #region Constructors

        public FamosFileProperty(string name, string value, FamosFilePropertyType type, FamosFilePropertyFlags flags)
        {
            this.Name = name;
            this.Value = value;
            this.Type = type;
            this.Flags = flags;

            this.Validate();
        }

        #endregion

        #region Properties

        public string Name { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public FamosFilePropertyType Type { get; private set; }
        public FamosFilePropertyFlags Flags { get; private set; }

        #endregion

        #region Methods

        internal object[] GetPropertyData()
        {
            return new object[]
            {
                $"\"{this.Name}\" \"{this.Value}\" {(int)this.Type} {(int)this.Flags}"
            };
        }

        internal void Validate()
        {
            if (string.IsNullOrEmpty(this.Value))
                return;

            var numberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

            switch (this.Type)
            {
                case FamosFilePropertyType.String:
                    break;

                case FamosFilePropertyType.Integer:
                    if (!int.TryParse(this.Value, numberStyle, CultureInfo.InvariantCulture, out int resultInt))
                        throw new FormatException($"The property value '{this.Value}' is not an integer number.");

                    break;

                case FamosFilePropertyType.Reell:
                    if (!double.TryParse(this.Value, numberStyle, CultureInfo.InvariantCulture, out double resultDouble))
                        throw new FormatException($"The property value '{this.Value}' is not a real numnber.");

                    break;

                case FamosFilePropertyType.TimeStampInDMFormat:
#warning TODO: Validate this and the following.
                    break;

                case FamosFilePropertyType.Enumeration:
#warning TODO: Validate this and the following.
                    break;

                case FamosFilePropertyType.Boolean:
                    if (this.Value == "0" || this.Value == "1")
                        throw new FormatException($"A boolean property value must be equal to '0' (false) or '1' (true).");
                    break;

                default:
                    throw new FormatException($"Invalid property type. Got type '{this.Type}'.");
            }
        }

        #endregion
    }
}