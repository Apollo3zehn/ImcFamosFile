namespace ImcFamosFile;

/// <summary>
/// Contains the actual data when serialized to the file.
/// </summary>
public class FamosFileRawBlock : FamosFileBase
{
    #region Fields

    private int _index;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FamosFileRawBlock"/> instance.
    /// </summary>
    public FamosFileRawBlock()
    {
        //
    }

    internal FamosFileRawBlock(BinaryReader reader) : base(reader)
    {
        var keyVersion = DeserializeInt32();

        if (keyVersion == 1)
        {
            DeserializeKey((Action<long>)(keySize =>
            {
                var startPosition = Reader.BaseStream.Position;
                Index = DeserializeInt32();
                var endPosition = Reader.BaseStream.Position;

                CompressionType = FamosFileCompressionType.Uncompressed;
                Length = keySize - (endPosition - startPosition);
                FileOffset = endPosition;

                Reader.BaseStream.TrySeek(Length + 1, SeekOrigin.Current);
            }));
        }
        else if (keyVersion == 2)
        {
            DeserializeKey((Action<long>)(keySize =>
            {
                Index = DeserializeInt32();
                CompressionType = (FamosFileCompressionType)DeserializeInt32();
                Length = DeserializeInt64();
                FileOffset = Reader.BaseStream.Position;

                Reader.BaseStream.TrySeek(Length + 1, SeekOrigin.Current);
            }));
        }
        else
        {
            throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the length of the raw data block in bytes.
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Gets or sets the compression type of the data.
    /// </summary>
    public FamosFileCompressionType CompressionType { get; set; }

    internal int Index
    {
        get { return _index; }
        set
        {
            if (value <= 0)
                throw new FormatException($"Expected index > '0', got '{value}'.");

            _index = value;
        }
    }

    internal long FileOffset { get; private set; }

    private protected override FamosFileKeyType KeyType => FamosFileKeyType.CS;

    #endregion

    #region Serialization

    internal override void Serialize(BinaryWriter writer)
    {
        var data = new object[]
        {
            Index,
            (int)CompressionType,
            Length,
            new FamosFilePlaceHolder() { Length = Length }
        };

        SerializeKey(writer, 2, data, addLineBreak: true); // --> -2 characters
        FileOffset = writer.BaseStream.Position - Length - 1 - 2;
    }

    #endregion
}
