using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileSingleValue : FamosFileBaseExtended
    {
        #region Fields

        private int _groupIndex;

        #endregion

        #region Constructors

        public FamosFileSingleValue()
        {
            //
        }

        internal FamosFileSingleValue(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.GroupIndex = this.DeserializeInt32();
                this.DataType = (FamosFileDataType)this.DeserializeInt32();
                this.Name = this.DeserializeString();

                this.Value = this.DataType switch
                {
                    FamosFileDataType.UInt8 => this.Reader.ReadByte(),
                    FamosFileDataType.Int8 => this.Reader.ReadSByte(),
                    FamosFileDataType.UInt16 => this.Reader.ReadUInt16(),
                    FamosFileDataType.Int16 => this.Reader.ReadInt16(),
                    FamosFileDataType.UInt32 => this.Reader.ReadUInt32(),
                    FamosFileDataType.Int32 => this.Reader.ReadInt32(),
                    FamosFileDataType.Float32 => this.Reader.ReadSingle(),
                    FamosFileDataType.Float64 => this.Reader.ReadDouble(),
                    FamosFileDataType.Digital16Bit => this.Reader.ReadUInt16(),
                    FamosFileDataType.UInt48 => BitConverter.ToUInt64(this.Reader.ReadBytes(6)),
                    _ => throw new FormatException("The data type of the single value is invalid.")
                };

                // read left over comma
                this.Reader.ReadByte();

                this.Unit = this.DeserializeString();
                this.Comment = this.DeserializeString();
                this.Time = BitConverter.ToDouble(this.DeserializeKeyPart());
            });
        }

        #endregion

        #region Properties

        internal int GroupIndex
        {
            get { return _groupIndex; }
            private set
            {
                if (value < 0)
                    throw new FormatException($"Expected group index >= '0', got '{value}'.");

                _groupIndex = value;
            }
        }

        public FamosFileDataType DataType { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public double Time { get; set; }

        #endregion
    }
}
