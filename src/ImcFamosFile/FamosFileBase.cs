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

        private Regex _matchKey;

        #endregion

        #region Constructors

        public FamosFileBase(BinaryReader reader, int codePage) : this(reader)
        {
            this.CodePage = codePage;
        }

        public FamosFileBase(BinaryReader reader)
        {
            this.Reader = reader;

            _matchKey = new Regex("[|][a-zA-Z]{2},");
        }

        public FamosFileBase()
        {
            //
        }

        #endregion

        #region Properties

        protected BinaryReader Reader { get; private set; }

        protected int CodePage { get; set; }

        #endregion

        #region Methods

        protected void SkipKey()
        {
            this.DeserializeInt32();
            this.DeserializeKey(null);
        }

        protected void DeserializeKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action<long> parseKeyDataAction)
        {
            // key type
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

        protected void DeserializeKey(Action<long> parseKeyDataAction)
        {
            // key length
            var keyLength = this.DeserializeInt64();

            // data
            if (parseKeyDataAction == null)
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

            if (!_matchKey.IsMatch(keyName))
                throw new FormatException("The format of the current key is invalid.");

            if (Enum.TryParse<FamosFileKeyType>(keyName[1..3], out var result))
                return result;
            else
                return FamosFileKeyType.Unknown;
        }

        protected string DeserializeString()
        {
            var length = this.DeserializeInt32();
            var value = Encoding.GetEncoding(this.CodePage).GetString(this.Reader.ReadBytes(length));

            this.Reader.ReadByte();

            return value;
        }

        protected List<string> DeserializeStringArray()
        {
            var elementCount = this.DeserializeInt32();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => this.DeserializeString()).ToList();
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
        protected FamosFileXAxisScaling DeserializeCD()
        {
            FamosFileXAxisScaling axisScaling = null;

            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    var dx = this.DeserializeFloat64();
                    var isCalibrated = this.DeserializeInt32() == 1;
                    var unit = this.DeserializeString();

                    this.DeserializeInt32();
                    this.DeserializeInt32();
                    this.DeserializeInt32();

                    axisScaling = new FamosFileXAxisScaling(dx, isCalibrated, unit);
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    var dx = this.DeserializeInt32();
                    var isCalibrated = this.DeserializeInt32() == 1;
                    var unit = this.DeserializeString();

                    // some data is not defined in imc document.
                    this.DeserializeKeyPart();
                    this.DeserializeKeyPart();
                    this.DeserializeKeyPart();

                    var x0 = this.DeserializeInt32();
                    var PretriggerUsage = (FamosFilePretriggerUsage)this.DeserializeInt32();

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
        protected FamosFileZAxisScaling DeserializeCZ()
        {
            FamosFileZAxisScaling axisScaling = null;

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var dz = this.DeserializeFloat64();
                var isDzCalibrated = this.DeserializeInt32() == 1;

                var z0 = this.DeserializeFloat64();
                var isZ0Calibrated = this.DeserializeInt32() == 1;

                var unit = this.DeserializeString();
                var segmentSize = this.DeserializeInt32();

                axisScaling = new FamosFileZAxisScaling(dz, isDzCalibrated, z0, isZ0Calibrated, unit, segmentSize);
            });

            return axisScaling;
        }

        // Trigger time
        protected FamosFileTriggerTimeInfo DeserializeNT()
        {
            DateTime triggerTime = default;
            FamosFileTimeMode timeMode = FamosFileTimeMode.Unknown;

            var keyVersion = this.DeserializeInt32();

            this.DeserializeKey(keySize =>
            {
                var day = this.DeserializeInt32();
                var month = this.DeserializeInt32();
                var year = this.DeserializeInt32();
                var hour = this.DeserializeInt32();
                var minute = this.DeserializeInt32();
                var second = this.DeserializeInt32();

                if (keyVersion == 1)
                {
                    triggerTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                }
                else if (keyVersion == 2)
                {
                    var timeZone = this.DeserializeInt32();

                    timeMode = (FamosFileTimeMode)this.DeserializeInt32();
                    triggerTime = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.FromMinutes(timeZone)).UtcDateTime;
                }
                else
                {
                    throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
                }
            });

            return new FamosFileTriggerTimeInfo()
            {
                DateTime = triggerTime,
                TimeMode = timeMode
            };
        }

        #endregion
    }
}
