using System.Globalization;

namespace ImcFamosFile;

/// <summary>
/// A named property of certain type.
/// </summary>
public class FamosFileProperty
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, int value, FamosFilePropertyFlags flags = 0)
        : this(name, value.ToString(CultureInfo.InvariantCulture), FamosFilePropertyType.Integer, flags)
    {
        //
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, double value, FamosFilePropertyFlags flags = 0)
        : this(name, value.ToString(CultureInfo.InvariantCulture), FamosFilePropertyType.Real, flags)
    {
        //
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, bool value, FamosFilePropertyFlags flags = 0)
        : this(name, value ? "1" : "0", FamosFilePropertyType.Boolean, flags)
    {
        //
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, string value, FamosFilePropertyFlags flags = 0)
        : this(name, value, FamosFilePropertyType.String, flags)
    {
        //
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, DateTime value, FamosFilePropertyFlags flags = 0)
    {
        var epoch = new DateTime(1980, 01, 01);

        Name = name;
        Value = (value - epoch).TotalSeconds.ToString(CultureInfo.InvariantCulture);
        Type = FamosFilePropertyType.TimeStampInDMFormat;
        Flags = flags;

        Validate();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileProperty"/>.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value as string.</param>
    /// <param name="type">The value's data type.</param>
    /// <param name="flags">Flags.</param>
    public FamosFileProperty(string name, string value, FamosFilePropertyType type, FamosFilePropertyFlags flags = 0)
    {
        Name = name;
        Value = value;
        Type = type;
        Flags = flags;

        Validate();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the value as string.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the value's data type.
    /// </summary>
    public FamosFilePropertyType Type { get; private set; }

    /// <summary>
    /// Gets the value's flags.
    /// </summary>
    public FamosFilePropertyFlags Flags { get; private set; }

    #endregion

    #region Methods

    internal object[] GetPropertyData()
    {
        return new object[]
        {
            $"\"{Name}\" \"{Value.Replace("\"", "\"\"")}\" {(int)Type} {(int)Flags}"
        };
    }

    internal void Validate()
    {
        if (string.IsNullOrEmpty(Value))
            return;

        var numberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

        switch (Type)
        {
            case FamosFilePropertyType.String:
                break;

            case FamosFilePropertyType.Integer:
                if (!int.TryParse(Value, numberStyle, CultureInfo.InvariantCulture, out int _))
                    throw new FormatException($"The property value '{Value}' is not an integer number.");

                break;

            case FamosFilePropertyType.Real:
                if (!double.TryParse(Value, numberStyle, CultureInfo.InvariantCulture, out double _))
                    throw new FormatException($"The property value '{Value}' is not a real number.");

                break;

            case FamosFilePropertyType.TimeStampInDMFormat:
                if (!double.TryParse(Value, numberStyle, CultureInfo.InvariantCulture, out double _))
                    throw new FormatException($"The property value '{Value}' is not a time stamp in correct format (seconds since 01.01.1980).");

                break;

            case FamosFilePropertyType.Enumeration:
                break;

            case FamosFilePropertyType.Boolean:
                if (Value == "0" || Value == "1")
                    throw new FormatException($"A boolean property value must be equal to '0' (false) or '1' (true).");
                break;

            default:
                throw new FormatException($"Invalid property type. Got type '{Type}'.");
        }
    }

    #endregion
}