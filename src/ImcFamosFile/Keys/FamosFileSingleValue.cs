using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    /// <summary>
    /// The base class for a named single value of certain type.
    /// </summary>
    public abstract class FamosFileSingleValue : FamosFileBaseProperty
    {
        #region Fields

        [HideFromApi]
        internal protected byte[] _rawData;

        private static readonly DateTime _referenceTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private int _groupIndex;

        #endregion

        #region Constructors

        [HideFromApi]
        internal protected FamosFileSingleValue(string name, byte[] rawBlock)
        {
            this.Name = name;

            _rawData = rawBlock;
        }

        [HideFromApi]
        internal protected unsafe FamosFileSingleValue(string name, byte* rawBlock, int size)
        {
            this.Name = name;

            _rawData = new byte[size];

            for (int i = 0; i < size; i++)
            {
                _rawData[i] = rawBlock[i];
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data type.
        /// </summary>
        public FamosFileDataType DataType { get; internal set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the raw data (bytes).
        /// </summary>
        public ReadOnlyCollection<byte> RawData => Array.AsReadOnly(_rawData);

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time when the value was created.
        /// </summary>
        public DateTime Time { get; set; }

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

        [HideFromApi]
        internal protected override FamosFileKeyType KeyType => FamosFileKeyType.CI;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.GroupIndex,
                (int)this.DataType,
                this.Name.Length, this.Name,
                _rawData,
                this.Unit.Length, this.Unit,
                this.Comment.Length, this.Comment,
                BitConverter.GetBytes((this.Time - _referenceTime).TotalSeconds)
            };

            this.SerializeKey(writer, 1, data);
            base.Serialize(writer);
        }

        #endregion

        internal class Deserializer : FamosFileBaseExtended
        {
            #region Constructors

            internal Deserializer(BinaryReader reader, int codePage) : base(reader, codePage)
            {
                //
            }

            #endregion

            #region Properties

            [HideFromApi]
            internal protected override FamosFileKeyType KeyType => throw new NotImplementedException();

            #endregion

            #region Methods

            public FamosFileSingleValue Deserialize()
            {
                FamosFileSingleValue? singleValue = null;

                this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                {
                    var groupIndex = this.DeserializeInt32();
                    var dataType = (FamosFileDataType)this.DeserializeInt32();
                    var name = this.DeserializeString();

                    var rawBlock = dataType switch
                    {
                        FamosFileDataType.UInt8 => this.Reader.ReadBytes(1),
                        FamosFileDataType.Int8 => this.Reader.ReadBytes(1),
                        FamosFileDataType.UInt16 => this.Reader.ReadBytes(2),
                        FamosFileDataType.Int16 => this.Reader.ReadBytes(2),
                        FamosFileDataType.UInt32 => this.Reader.ReadBytes(4),
                        FamosFileDataType.Int32 => this.Reader.ReadBytes(4),
                        FamosFileDataType.Float32 => this.Reader.ReadBytes(4),
                        FamosFileDataType.Float64 => this.Reader.ReadBytes(8),
                        FamosFileDataType.Digital16Bit => this.Reader.ReadBytes(2),
                        FamosFileDataType.UInt48 => this.Reader.ReadBytes(6),
                        _ => throw new FormatException("The data type is invalid.")
                    };

                    // read left over comma
                    this.Reader.ReadByte();

                    // create single value
                    singleValue = dataType switch
                    {
                        FamosFileDataType.UInt8 => new FamosFileSingleValue<Byte>(name, rawBlock),
                        FamosFileDataType.Int8 => new FamosFileSingleValue<SByte>(name, rawBlock),
                        FamosFileDataType.UInt16 => new FamosFileSingleValue<UInt16>(name, rawBlock),
                        FamosFileDataType.Int16 => new FamosFileSingleValue<Int16>(name, rawBlock),
                        FamosFileDataType.UInt32 => new FamosFileSingleValue<UInt32>(name, rawBlock),
                        FamosFileDataType.Int32 => new FamosFileSingleValue<Int32>(name, rawBlock),
                        FamosFileDataType.Float32 => new FamosFileSingleValue<Single>(name, rawBlock),
                        FamosFileDataType.Float64 => new FamosFileSingleValue<Double>(name, rawBlock),
                        FamosFileDataType.Digital16Bit => new FamosFileSingleValue<UInt16>(name, rawBlock),
                        FamosFileDataType.UInt48 => new FamosFileSingleValue<Int64>(name, new byte[] { 0, 0 }.Concat(rawBlock).ToArray()),
                        _ => throw new FormatException("The data type is invalid.")
                    };

                    singleValue.GroupIndex = groupIndex;
                    singleValue.DataType = dataType;
                    singleValue.Unit = this.DeserializeString();
                    singleValue.Comment = this.DeserializeString();
                    singleValue.Time = _referenceTime.AddSeconds(BitConverter.ToDouble(this.DeserializeKeyPart()));
                });

                if (singleValue is null)
                    throw new InvalidOperationException("An unexpected state has been reached.");

                return singleValue;
            }

            internal override void Serialize(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }

    /// <summary>
    /// A named single value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The data type parameter.</typeparam>
    public class FamosFileSingleValue<T> : FamosFileSingleValue where T : unmanaged
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileSingleValue{T}" class./>
        /// </summary>
        /// <param name="name">The name of the single value.</param>
        /// <param name="value">The value of the single value.</param>
        public unsafe FamosFileSingleValue(string name, T value) : base(name, (byte*)(&value), Marshal.SizeOf<T>())
        {
            this.DataType = value switch
            {
                SByte _ => FamosFileDataType.UInt8,
                Byte _ => FamosFileDataType.Int8,
                UInt16 _ => FamosFileDataType.UInt16,
                Int16 _ => FamosFileDataType.Int16,
                UInt32 _ => FamosFileDataType.UInt32,
                Int32 _ => FamosFileDataType.Int32,
                Single _ => FamosFileDataType.Float32,
                Double _ => FamosFileDataType.Float64,
                _ => throw new FormatException("The data type is invalid.")
            };
        }

        internal unsafe FamosFileSingleValue(string name, byte[] rawBlock) : base(name, rawBlock)
        {
            this.DataType = default(T) switch
            {
                SByte _ => FamosFileDataType.UInt8,
                Byte _ => FamosFileDataType.Int8,
                UInt16 _ => FamosFileDataType.UInt16,
                Int16 _ => FamosFileDataType.Int16,
                UInt32 _ => FamosFileDataType.UInt32,
                Int32 _ => FamosFileDataType.Int32,
                Single _ => FamosFileDataType.Float32,
                Double _ => FamosFileDataType.Float64,
                _ => throw new FormatException("The data type is invalid.")
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value of type <typeparamref name="T"/>.
        /// </summary>
        public T Value => MemoryMarshal.Cast<byte, T>(_rawData)[0];

        #endregion
    }
}
