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
            this.ParseInteger();
            this.ParseKey(null);
        }

        protected void ParseKey(FamosFileKeyType expectedKeyType, int expectedKeyVersion, Action parseKeyDataAction)
        {
            // key type
            var keyType = this.ParseKeyType();

            if (keyType != expectedKeyType)
                throw new FormatException($"Expected key '{expectedKeyType.ToString()}', got '{keyType.ToString()}'.");

            this.ParseKey(expectedKeyVersion, parseKeyDataAction);
        }

        protected void ParseKey(int expectedKeyVersion, Action parseKeyDataAction)
        {
            // key version
            var keyVersion = this.ParseInteger();

            if (keyVersion != expectedKeyVersion)
                throw new FormatException($"Expected key version '{expectedKeyVersion}', got '{keyVersion}'.");

            this.ParseKey(parseKeyDataAction);
        }

        protected void ParseKey(Action parseKeyDataAction)
        {
            // key length
            var keyLength = this.ParseInteger();

            // data
            if (parseKeyDataAction == null)
                this.Reader.ReadBytes(keyLength + 1);
            else
                parseKeyDataAction?.Invoke();

            // consume spaces
            this.ConsumeSpaces();
        }

        protected byte[] ParseNumber()
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
                throw new FormatException("Number is too long or a comma is missing.");

            return bytes.ToArray();
        }

        protected int ParseInteger()
        {
            var bytes = this.ParseNumber();
            return int.Parse(Encoding.ASCII.GetString(bytes));
        }

        protected double ParseDouble()
        {
            var bytes = this.ParseNumber();
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
            var length = this.ParseInteger();
            var value = Encoding.GetEncoding(this.CodePage).GetString(this.Reader.ReadBytes(length));

            this.Reader.ReadByte();

            return value;
        }

        protected List<string> ParseStringArray()
        {
            var elementCount = this.ParseInteger();

            if (elementCount < 0 || elementCount > int.MaxValue)
                throw new FormatException("The number of texts is out of range.");

            return Enumerable.Range(0, elementCount).Select(current => this.ParseString()).ToList();
        }

        private void ConsumeSpaces()
        {
            var data = this.Reader.ReadByte();

            while (string.IsNullOrWhiteSpace(Encoding.ASCII.GetString(new[] { data })))
                data = this.Reader.ReadByte();

            this.Reader.BaseStream.Position -= 1;
        }

        #endregion

        #region Keys

        // Component scaling (x-axis scaling for single component, parameter for 2-component components.
        protected FamosFileXAxisScaling ParseCD()
        {
            FamosFileXAxisScaling axisScaling = null;

            var keyVersion = this.ParseInteger();

            if (keyVersion == 1)
            {
                this.ParseKey(() =>
                {
                    var dz = this.ParseInteger();
                    var isCalibrated = this.ParseInteger() == 1;
                    var unit = this.ParseString();

                    this.ParseInteger();
                    this.ParseInteger();
                    this.ParseInteger();

                    axisScaling = new FamosFileXAxisScaling(dz, isCalibrated, unit);
                });
            }
            else if (keyVersion == 2)
            {
#warning TODO: Key type 'CD' version '2'.
                throw new FormatException("Unable to parse key type 'CD' version '2' due to lack of imc documentation.");

                //this.ParseKey(() =>
                //{
                //    var dz = this.ParseInteger();
                //    var isCalibrated = this.ParseInteger() == 1;
                //    var unit = this.ParseString();

                //    // some data is not defined in imc document.

                //    var x0 = this.ParseInteger();
                //    var PretriggerUsage = (FamosFilePretriggerUsage)this.ParseInteger();

                //    axisScaling = new FamosFileXAxisScaling(dz, isCalibrated, unit);
                //});
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

            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var dz = this.ParseInteger();
                var isDzCalibrated = this.ParseInteger() == 1;

                var z0 = this.ParseInteger();
                var isZ0Calibrated = this.ParseInteger() == 1;

                var unit = this.ParseString();
                var segmentSize = this.ParseInteger();

                axisScaling = new FamosFileZAxisScaling(dz, isDzCalibrated, z0, isZ0Calibrated, unit, segmentSize);
            });

            return axisScaling;
        }

        // Trigger time
        protected (DateTime TriggerTime, FamosFileTimeMode TimeMode) ParseNT()
        {
            DateTime triggerTime = default;
            FamosFileTimeMode timeMode = FamosFileTimeMode.Unknown;

            var keyVersion = this.ParseInteger();

            this.ParseKey(() =>
            {
                var day = this.ParseInteger();
                var month = this.ParseInteger();
                var year = this.ParseInteger();
                var hour = this.ParseInteger();
                var minute = this.ParseInteger();
                var second = this.ParseInteger();

                if (keyVersion == 1)
                {
                    triggerTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                }
                else if (keyVersion == 2)
                {
                    var timeZone = this.ParseInteger();

                    timeMode = (FamosFileTimeMode)this.ParseInteger();
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
