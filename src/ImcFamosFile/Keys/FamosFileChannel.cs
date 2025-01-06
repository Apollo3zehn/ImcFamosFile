namespace ImcFamosFile;

/// <summary>
/// Channels are used to assign names to components. A channel can be assigned to a group.
/// </summary>
public class FamosFileChannel : FamosFileBaseProperty
{
    #region Fields

    private int _groupIndex;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileChannel"/> class.
    /// </summary>
    /// <param name="name">The name of this channel.</param>
    public FamosFileChannel(string name)
    {
        Name = name;
    }

    internal FamosFileChannel(BinaryReader reader, int codePage) : base(reader, codePage)
    {
        DeserializeKey(expectedKeyVersion: 1, keySize =>
        {
            GroupIndex = DeserializeInt32();
            DeserializeInt32(); // reserved parameter
            BitIndex = DeserializeInt32();
            Name = DeserializeString();
            Comment = DeserializeString();
        });
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the index of the bit position. Only for digital data. Analog data = 0, LSB..MSB (digital data) = 1..16.
    /// </summary>
    public int BitIndex { get; set; }

    /// <summary>
    /// Gets or sets the name of this channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comment of this channel.
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    internal int GroupIndex
    {
        get { return _groupIndex; }
        set
        {
            if (value < 0)
                throw new FormatException($"Expected group index >= '0', got '{value}'.");

            _groupIndex = value;
        }
    }

    private protected override FamosFileKeyType KeyType => FamosFileKeyType.CN;

    #endregion

    #region Serialization

    internal override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        var data = new object[]
        {
            GroupIndex,
            "0", // reserved parameter
            BitIndex,
            Name.Length, Name,
            Comment.Length, Comment
        };

        SerializeKey(writer, 1, data);
    }

    #endregion
}
