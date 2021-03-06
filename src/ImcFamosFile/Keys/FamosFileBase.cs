﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImcFamosFile
{
    /// <summary>
    /// Base type to represent all imc FAMOS file keys.
    /// </summary>
    public abstract class FamosFileBase
    {
        #region Fields

        private readonly BinaryReader? _reader;

        #endregion

        #region Constructors

        private protected FamosFileBase(BinaryReader reader)
        {
            _reader = reader;
        }

        private protected FamosFileBase()
        {
            //
        }

        #endregion

        #region Properties

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private protected BinaryReader Reader
        { 
            get
            {
                if (_reader is null)
                    throw new NullReferenceException($"{nameof(this.Reader)} is null.");

                return _reader;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private protected abstract FamosFileKeyType KeyType { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Regex MatchKey { get; } = new Regex("[|][a-zA-Z]{2},");

        #endregion

        #region Methods

        /// <summary>
        /// Validates the key status.
        /// </summary>
        public virtual void Validate()
        {
            //
        }

        internal void CheckIndexConsistency<T>(string name, List<T> collection, Func<T, int> getIndex)
        {
            foreach (var item in collection)
            {
                var expected = collection.IndexOf(item) + 1;
                var actual = getIndex(item);

                if (expected != actual)
                    throw new FormatException($"The {name} indices are not consistent. Expected '{expected}', got '{actual}'.");
            }
        }

        #endregion

        #region Serialization

        internal virtual void BeforeSerialize()
        {
            //
        }

        internal virtual void AfterSerialize()
        {
            //
        }

        internal abstract void Serialize(BinaryWriter writer);

        private protected void SerializeKey(BinaryWriter writer, int keyVersion, object[] data, bool addLineBreak = true)
        {
            // convert data
            long length = 0;

            var combinedData = data.Select<object, object>(current =>
            {
                switch (current)
                {
                    case FamosFilePlaceHolder x:
                        length += x.Length;
                        return x;

                    case byte[] x:
                        length += x.LongLength;
                        return x;

                    case decimal x:
                        var charArray1 = x.ToString("0.######################", CultureInfo.InvariantCulture).ToCharArray();
                        length += charArray1.LongLength;
                        return charArray1;

                    case int _:
                    case long _:
                    case string _:
                        var charArray2 = $"{current}".ToCharArray();
                        length += charArray2.LongLength;
                        return charArray2;

                    default:
                        throw new InvalidOperationException($"The data type {current.GetType()} is not supported.");
                }
            }).ToList();

            length += Math.Max(0, combinedData.Count() - 1);

            // write key preamble
            writer.Write($"|{this.KeyType.ToString()},{keyVersion},{length},".ToCharArray());

            // write key content
            for (int i = 0; i < combinedData.Count; i++)
            {
                switch (combinedData[i])
                {
                    case char[] x:
                        writer.Write(x); break;

                    case byte[] x:
                        writer.Write(x); break;

                    case FamosFilePlaceHolder x:
                        writer.BaseStream.Seek(x.Length, SeekOrigin.Current); break;
                }

                if (i < combinedData.Count - 1)
                    writer.Write(',');
            }

            this.CloseKey(writer, addLineBreak);
        }

        private void CloseKey(BinaryWriter writer, bool addLineBreak)
        {
            writer.Write(';');

            if (addLineBreak)
            {
                writer.Write((byte)0x0D);
                writer.Write((byte)0x0A);
            }
        }

        #endregion

        #region Deserialization

        internal virtual void AfterDeserialize()
        {
            //
        }

        private protected void SkipKey()
        {
            this.DeserializeInt32();
            this.DeserializeKey(deserializeKeyAction: null);
        }

        private protected void DeserializeKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action<long> deserializeKeyAction)
        {
            var keyType = this.DeserializeKeyType();

            if (keyType != expectedKeyType)
                throw new FormatException($"Expected key '{expectedKeyType.ToString()}', got '{keyType.ToString()}'.");

            this.DeserializeKey(expectedKeyVersion, deserializeKeyAction);
        }

        private protected void DeserializeKey(int expectedKeyVersion, Action<long> deserializeKeyAction)
        {
            // key version
            var keyVersion = this.DeserializeInt32();

            if (keyVersion != expectedKeyVersion)
                throw new FormatException($"Expected key version '{expectedKeyVersion}', got '{keyVersion}'.");

            this.DeserializeKey(deserializeKeyAction);
        }

        private protected void DeserializeKey(Action<long>? deserializeKeyAction)
        {
            // key length
            var keyLength = this.DeserializeInt64();

            // data
            if (deserializeKeyAction is null)
                this.DeserializeFixedLength(unchecked((int)keyLength));// should not fail as this is intended only for short keys
            else
                deserializeKeyAction?.Invoke(keyLength);

            // consume spaces
            this.ConsumeSpaces();
        }

        private protected byte[] DeserializeKeyPart()
        {
            var bytes = new List<byte>();

            while (true)
            {
                var current = this.Reader.ReadByte();

                if (current == ',' || current == ';')
                    break;

                bytes.Add(current);
            }

            return bytes.ToArray();
        }

        private protected int DeserializeHex()
        {
            var bytes = this.DeserializeKeyPart();
            return Convert.ToInt32(Encoding.ASCII.GetString(bytes), 16);
        }

        private protected int DeserializeInt32()
        {
            var bytes = this.DeserializeKeyPart();
            return int.Parse(Encoding.ASCII.GetString(bytes));
        }

        private protected long DeserializeInt64()
        {
            var bytes = this.DeserializeKeyPart();
            return long.Parse(Encoding.ASCII.GetString(bytes));
        }

        private protected decimal DeserializeReal()
        {
            var bytes = this.DeserializeKeyPart();
            var numberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

            return decimal.Parse(Encoding.ASCII.GetString(bytes), numberStyle, CultureInfo.InvariantCulture);
        }

        private protected FamosFileKeyType DeserializeKeyType()
        {
            var keyName = Encoding.ASCII.GetString(this.Reader.ReadBytes(4));

            if (!this.MatchKey.IsMatch(keyName))
                throw new FormatException("The format of the current key is invalid.");

            if (Enum.TryParse<FamosFileKeyType>(keyName[1..3], out var result))
                return result;
            else
                return FamosFileKeyType.Unknown;
        }

        private protected byte[] DeserializeFixedLength(int length)
        {
            var data = this.Reader.ReadBytes(length);

            // read comma or semicolon
            this.Reader.ReadByte();

            return data;
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
