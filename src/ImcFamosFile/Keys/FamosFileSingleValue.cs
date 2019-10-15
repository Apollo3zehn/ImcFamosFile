using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    public abstract class FamosFileSingleValue : FamosFileBaseExtended
    {
        #region Fields

        protected byte[] _rawData;
        private static DateTime _referenceTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private int _groupIndex;

        #endregion

        #region Constructors

        internal FamosFileSingleValue(byte[] rawData)
        {
            _rawData = rawData;
        }

        internal unsafe FamosFileSingleValue(byte* rawData, int size)
        {
            _rawData = new byte[size];

            for (int i = 0; i < size; i++)
            {
                _rawData[i] = rawData[i];
            }
        }

        #endregion

        #region Properties

        public FamosFileDataType DataType { get; protected set; }
        public string Name { get; set; } = string.Empty;
        public ReadOnlyCollection<byte> RawData => Array.AsReadOnly(_rawData);
        public string Unit { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
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

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CI;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var data = new object[]
            {
                this.GroupIndex,
                (int)this.DataType,
                this.Name.Length, this.Name,
                this.RawData,
                this.Unit.Length, this.Unit,
                this.Comment.Length, this.Comment,
                (this.Time - _referenceTime).TotalSeconds
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion

        internal class Deserializer : FamosFileBaseExtended
        {
            #region Constructors

            public Deserializer(BinaryReader reader, int codePage) : base(reader, codePage)
            {
                //
            }

            #endregion

            #region Properties

            protected override FamosFileKeyType KeyType => throw new NotImplementedException();

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

                    var rawData = dataType switch
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
                        FamosFileDataType.UInt8 => new FamosFileSingleValue<Byte>(rawData),
                        FamosFileDataType.Int8 => new FamosFileSingleValue<SByte>(rawData),
                        FamosFileDataType.UInt16 => new FamosFileSingleValue<UInt16>(rawData),
                        FamosFileDataType.Int16 => new FamosFileSingleValue<Int16>(rawData),
                        FamosFileDataType.UInt32 => new FamosFileSingleValue<UInt32>(rawData),
                        FamosFileDataType.Int32 => new FamosFileSingleValue<Int32>(rawData),
                        FamosFileDataType.Float32 => new FamosFileSingleValue<Single>(rawData),
                        FamosFileDataType.Float64 => new FamosFileSingleValue<Double>(rawData),
                        FamosFileDataType.Digital16Bit => new FamosFileSingleValue<UInt16>(rawData),
                        FamosFileDataType.UInt48 => new FamosFileSingleValue<Int64>(new byte[] { 0, 0 }.Concat(rawData).ToArray()),
                        _ => throw new FormatException("The data type is invalid.")
                    };

                    singleValue.Unit = this.DeserializeString();
                    singleValue.Comment = this.DeserializeString();
                    singleValue.Time = _referenceTime.AddSeconds(BitConverter.ToDouble(this.DeserializeKeyPart()));
                });

                if (singleValue is null)
                    throw new InvalidOperationException("An unexpected state has been reached.");

                return singleValue;
            }

            internal override void Serialize(StreamWriter writer)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }

    public class FamosFileSingleValue<T> : FamosFileSingleValue where T : unmanaged
    {
        internal unsafe FamosFileSingleValue(byte[] rawData) : base(rawData)
        {
            this.DataType = default(T) switch
            {
                SByte  _ => FamosFileDataType.UInt8,
                Byte   _ => FamosFileDataType.Int8,
                UInt16 _ => FamosFileDataType.UInt16,
                Int16  _ => FamosFileDataType.Int16,
                UInt32 _ => FamosFileDataType.UInt32,
                Int32  _ => FamosFileDataType.Int32,
                Single _ => FamosFileDataType.Float32,
                Double _ => FamosFileDataType.Float64,
                _ => throw new FormatException("The data type is invalid.")
            };
        }

        public unsafe FamosFileSingleValue(T value) : base((byte*)(&value), Marshal.SizeOf<T>())
        {
            this.DataType = value switch
            {
                SByte  _ => FamosFileDataType.UInt8,
                Byte   _ => FamosFileDataType.Int8,
                UInt16 _ => FamosFileDataType.UInt16,
                Int16  _ => FamosFileDataType.Int16,
                UInt32 _ => FamosFileDataType.UInt32,
                Int32  _ => FamosFileDataType.Int32,
                Single _ => FamosFileDataType.Float32,
                Double _ => FamosFileDataType.Float64,
                _ => throw new FormatException("The data type is invalid.")
            };
        }

        public T Value => MemoryMarshal.Cast<byte, T>(_rawData)[0];
    }
}
