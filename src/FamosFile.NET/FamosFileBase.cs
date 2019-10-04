using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FamosFile.NET
{
    public class FamosFileBase
    {
        #region Fields

        private Regex _matchKey;

        #endregion

        #region Constructors

        public FamosFileBase(BinaryReader reader)
        {
            this.Reader = reader;

            _matchKey = new Regex("[|][a-zA-Z]{2},");
        }

        #endregion

        #region Properties

        protected BinaryReader Reader { get; private set; }

        protected int CodePage { get; set; }

        #endregion

        #region Methods

        protected void SkipKey()
        {
            this.ParseInt32();
            this.ParseKey(null);
        }

        protected void ParseKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action<long> parseKeyDataAction)
        {
            // key type
            var keyType = this.ParseKeyType();

            if (keyType != expectedKeyType)
                throw new FormatException($"Expected key '{expectedKeyType.ToString()}', got '{keyType.ToString()}'.");

            this.ParseKey(expectedKeyVersion, parseKeyDataAction);
        }

        protected void ParseKey(int expectedKeyVersion, Action<long> parseKeyDataAction)
        {
            // key version
            var keyVersion = this.ParseInt32();

            if (keyVersion != expectedKeyVersion)
                throw new FormatException($"Expected key version '{expectedKeyVersion}', got '{keyVersion}'.");

            this.ParseKey(parseKeyDataAction);
        }

        protected void ParseKey(Action<long> parseKeyDataAction)
        {
            // key length
            var keyLength = this.ParseInt64();

            // data
            if (parseKeyDataAction == null)
                this.Reader.ReadBytes(unchecked((int)(keyLength + 1))); // should not fail as this is intended only for short keys
            else
                parseKeyDataAction?.Invoke(keyLength);

            // consume spaces
            this.ConsumeSpaces();
        }

        protected byte[] ParseKeyPart()
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

        protected int ParseHex()
        {
            var bytes = this.ParseKeyPart();
            return Convert.ToInt32(Encoding.ASCII.GetString(bytes), 16);
        }

        protected int ParseInt32()
        {
            var bytes = this.ParseKeyPart();
            return int.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected long ParseInt64()
        {
            var bytes = this.ParseKeyPart();
            return long.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected double ParseFloat64()
        {
            var bytes = this.ParseKeyPart();
            return double.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected FamosFileKeyType ParseKeyType()
        {
            var keyName = Encoding.ASCII.GetString(this.Reader.ReadBytes(4));

            if (!_matchKey.IsMatch(keyName))
                throw new FormatException("The format of the current key is invalid.");

            if (Enum.TryParse<FamosFileKeyType>(keyName[1..3], out var result))
                return result;
            else
                return FamosFileKeyType.Unknown;
        }

        protected string ParseString()
        {
            var length = this.ParseInt32();
            var value = Encoding.GetEncoding(this.CodePage).GetString(this.Reader.ReadBytes(length));

            this.Reader.ReadByte();

            return value;
        }

        protected List<string> ParseStringArray()
        {
            var elementCount = this.ParseInt32();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => this.ParseString()).ToList();
        }

        private void ConsumeSpaces()
        {
            var data = this.Reader.ReadByte();

            while (string.IsNullOrWhiteSpace(Encoding.ASCII.GetString(new[] { data })))
            {
                data = this.Reader.ReadByte();
            }

            this.Reader.BaseStream.Position -= 1;
        }

        #endregion

        #region Keys

        // Component scaling (x-axis scaling for single component, parameter for 2-component components.
        protected FamosFileXAxisScaling ParseCD()
        {
            FamosFileXAxisScaling axisScaling = null;

            var keyVersion = this.ParseInt32();

            if (keyVersion == 1)
            {
                this.ParseKey(keySize =>
                {
                    var dx = this.ParseFloat64();
                    var isCalibrated = this.ParseInt32() == 1;
                    var unit = this.ParseString();

                    this.ParseInt32();
                    this.ParseInt32();
                    this.ParseInt32();

                    axisScaling = new FamosFileXAxisScaling(dx, isCalibrated, unit);
                });
            }
            else if (keyVersion == 2)
            {
                this.ParseKey(keySize =>
                {
                    var dx = this.ParseInt32();
                    var isCalibrated = this.ParseInt32() == 1;
                    var unit = this.ParseString();

                    // some data is not defined in imc document.
                    this.ParseKeyPart();
                    this.ParseKeyPart();
                    this.ParseKeyPart();

                    var x0 = this.ParseInt32();
                    var PretriggerUsage = (FamosFilePretriggerUsage)this.ParseInt32();

                    axisScaling = new FamosFileXAxisScaling(dx, isCalibrated, unit, x0, PretriggerUsage);
                });
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }

            return axisScaling;
        }

        // Z-axis scaling.
        protected FamosFileZAxisScaling ParseCZ()
        {
            FamosFileZAxisScaling axisScaling = null;

            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                var dz = this.ParseFloat64();
                var isDzCalibrated = this.ParseInt32() == 1;

                var z0 = this.ParseFloat64();
                var isZ0Calibrated = this.ParseInt32() == 1;

                var unit = this.ParseString();
                var segmentSize = this.ParseInt32();

                axisScaling = new FamosFileZAxisScaling(dz, isDzCalibrated, z0, isZ0Calibrated, unit, segmentSize);
            });

            return axisScaling;
        }

        // Trigger time
        protected (DateTime TriggerTime, FamosFileTimeMode TimeMode) ParseNT()
        {
            DateTime triggerTime = default;
            FamosFileTimeMode timeMode = FamosFileTimeMode.Unknown;

            var keyVersion = this.ParseInt32();

            this.ParseKey(keySize =>
            {
                var day = this.ParseInt32();
                var month = this.ParseInt32();
                var year = this.ParseInt32();
                var hour = this.ParseInt32();
                var minute = this.ParseInt32();
                var second = this.ParseInt32();

                if (keyVersion == 1)
                {
                    triggerTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                }
                else if (keyVersion == 2)
                {
                    var timeZone = this.ParseInt32();

                    timeMode = (FamosFileTimeMode)this.ParseInt32();
                    triggerTime = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.FromMinutes(timeZone)).UtcDateTime;
                }
                else
                {
                    throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
                }
            });

            return (triggerTime, timeMode);
        }

        #endregion
    }
}
