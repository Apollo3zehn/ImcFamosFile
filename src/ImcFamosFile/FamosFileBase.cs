using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ImcFamosFile
{
    public abstract class FamosFileBase
    {
        #region Fields

        private readonly BinaryReader? _reader;

        #endregion

        #region Constructors

        public FamosFileBase(BinaryReader reader)
        {
            _reader = reader;
        }

        public FamosFileBase()
        {
            //
        }

        #endregion

        #region Properties

        private Regex MatchKey { get; } = new Regex("[|][a-zA-Z]{2},");

        protected BinaryReader Reader
        { 
            get
            {
                if (_reader is null)
                    throw new NullReferenceException($"{nameof(this.Reader)} is null.");

                return _reader;
            }
        }

        protected abstract FamosFileKeyType KeyType { get; }

        #endregion

        #region Methods

        internal virtual void Validate()
        {
            //
        }

        #endregion

        #region Serialization

        internal virtual void BeforeSerialize()
        {
            //
        }

        protected void SerializeKey(StreamWriter writer, FamosFileKeyType keyType, int keyVersion, string data, bool addLineBreak = true)
        {
            writer.Write($"|{keyType.ToString()},{keyVersion},{data.Length},");
            writer.Write(data);

            this.CloseKey(writer, addLineBreak);
        }

        protected void SerializeKey(StreamWriter writer, FamosFileKeyType keyType, int keyVersion, string dataPre, string dataPost, Action additionalWriteAction, bool addLineBreak = true)
        {
            writer.Write($"|{keyType.ToString()},{keyVersion},{dataPre.Length + dataPost.Length},");
            writer.Write(dataPre);
#warning TODO: this causes wrong length in key
            additionalWriteAction.Invoke();
            writer.Write(dataPost);

            this.CloseKey(writer, addLineBreak);
        }


        private void CloseKey(StreamWriter writer, bool addLineBreak)
        {
            writer.Write(';');

            if (addLineBreak)
                writer.Write($"\r\n");
        }

        internal abstract void Serialize(StreamWriter writer);

        #endregion

        #region Deserialization

        internal virtual void AfterDeserialize()
        {
            //
        }

        protected void SkipKey()
        {
            this.DeserializeInt32();
            this.DeserializeKey(deserializeKeyAction: null);
        }

        protected void DeserializeKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action<long> deserializeKeyAction)
        {
            var keyType = this.DeserializeKeyType();

            if (keyType != expectedKeyType)
                throw new FormatException($"Expected key '{expectedKeyType.ToString()}', got '{keyType.ToString()}'.");

            this.DeserializeKey(expectedKeyVersion, deserializeKeyAction);
        }

        protected void DeserializeKey(int expectedKeyVersion, Action<long> deserializeKeyAction)
        {
            // key version
            var keyVersion = this.DeserializeInt32();

            if (keyVersion != expectedKeyVersion)
                throw new FormatException($"Expected key version '{expectedKeyVersion}', got '{keyVersion}'.");

            this.DeserializeKey(deserializeKeyAction);
        }

        protected void DeserializeKey(Action<long>? deserializeKeyAction)
        {
            // key length
            var keyLength = this.DeserializeInt64();

            // data
            if (deserializeKeyAction is null)
                this.Reader.ReadBytes(unchecked((int)(keyLength + 1))); // should not fail as this is intended only for short keys
            else
                deserializeKeyAction?.Invoke(keyLength);

            // consume spaces
            this.ConsumeSpaces();
        }

        protected byte[] DeserializeKeyPart()
        {
            var bytes = new List<byte>();
            var counter = 0;

            while (counter < 32)
            {
                var current = this.Reader.ReadByte();

                if (current == ',' || current == ';')
                    break;

                bytes.Add(current);
                counter++;
            }

            if (counter >= 32)
                throw new FormatException("Value is too long or a comma is missing.");

            return bytes.ToArray();
        }

        protected int DeserializeHex()
        {
            var bytes = this.DeserializeKeyPart();
            return Convert.ToInt32(Encoding.ASCII.GetString(bytes), 16);
        }

        protected int DeserializeInt32()
        {
            var bytes = this.DeserializeKeyPart();
            return int.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected long DeserializeInt64()
        {
            var bytes = this.DeserializeKeyPart();
            return long.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected double DeserializeFloat64()
        {
            var bytes = this.DeserializeKeyPart();
            var numberStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

            return double.Parse(Encoding.ASCII.GetString(bytes), numberStyle, CultureInfo.InvariantCulture);
        }

        protected FamosFileKeyType DeserializeKeyType()
        {
            var keyName = Encoding.ASCII.GetString(this.Reader.ReadBytes(4));

            if (!this.MatchKey.IsMatch(keyName))
                throw new FormatException("The format of the current key is invalid.");

            if (Enum.TryParse<FamosFileKeyType>(keyName[1..3], out var result))
                return result;
            else
                return FamosFileKeyType.Unknown;
        }

        private void ConsumeSpaces()
        {
            if (this.Reader.BaseStream.Position >= this.Reader.BaseStream.Length)
                return;

            var data = this.Reader.ReadByte();

            while (string.IsNullOrWhiteSpace(Encoding.ASCII.GetString(new[] { data })))
            {
                if (this.Reader.BaseStream.Position < this.Reader.BaseStream.Length)
                    data = this.Reader.ReadByte();
                else
                    return;
            }

            this.Reader.BaseStream.Position -= 1;
        }

        #endregion
    }
}
