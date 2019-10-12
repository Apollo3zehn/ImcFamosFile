using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileSingleValue : FamosFileBaseExtended
    {
        #region Fields

        private DateTime _referenceTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private int _groupIndex;

        #endregion

        #region Constructors

        public FamosFileSingleValue(byte[] value)
        {
            this.Value = value;
        }

        internal FamosFileSingleValue(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.Value = new byte[0];

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.GroupIndex = this.DeserializeInt32();
                this.DataType = (FamosFileDataType)this.DeserializeInt32();
                this.Name = this.DeserializeString();

                this.Value = this.DataType switch
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

                this.Unit = this.DeserializeString();
                this.Comment = this.DeserializeString();
                this.Time = _referenceTime.AddSeconds(BitConverter.ToDouble(this.DeserializeKeyPart()));
            });
        }

        #endregion

        #region Properties

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

        public FamosFileDataType DataType { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime Time { get; set; }

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
                this.Value,
                this.Unit.Length, this.Unit,
                this.Comment.Length, this.Comment,
                (this.Time - _referenceTime).TotalSeconds
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
