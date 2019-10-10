using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImcFamosFile
{
    public abstract class FamosFileBase
    {
        #region Fields

        BinaryReader? _reader;

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

        #endregion

        #region Methods

        internal virtual void Prepare()
        {
            //
        }

        internal virtual void Validate()
        {
            //
        }

        protected void SkipKey()
        {
            this.DeserializeInt32();
            this.DeserializeKey(parseKeyDataAction: null);
        }

        protected void DeserializeKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action<long> parseKeyDataAction)
        {
            var keyType = this.DeserializeKeyType();

            if (keyType != expectedKeyType)
                throw new FormatException($"Expected key '{expectedKeyType.ToString()}', got '{keyType.ToString()}'.");

            this.DeserializeKey(expectedKeyVersion, parseKeyDataAction);
        }

        protected void DeserializeKey(int expectedKeyVersion, Action<long> parseKeyDataAction)
        {
            // key version
            var keyVersion = this.DeserializeInt32();

            if (keyVersion != expectedKeyVersion)
                throw new FormatException($"Expected key version '{expectedKeyVersion}', got '{keyVersion}'.");

            this.DeserializeKey(parseKeyDataAction);
        }

        protected void DeserializeKey(Action<long>? parseKeyDataAction)
        {
            // key length
            var keyLength = this.DeserializeInt64();

            // data
            if (parseKeyDataAction is null)
                this.Reader.ReadBytes(unchecked((int)(keyLength + 1))); // should not fail as this is intended only for short keys
            else
                parseKeyDataAction?.Invoke(keyLength);

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
            return double.Parse(Encoding.ASCII.GetString(bytes));
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
