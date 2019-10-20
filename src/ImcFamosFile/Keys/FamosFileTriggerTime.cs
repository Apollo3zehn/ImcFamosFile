using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileTriggerTime : FamosFileBase
    {
        #region Constructors

        public FamosFileTriggerTime()
        {
            //
        }

        internal FamosFileTriggerTime(BinaryReader reader) : base(reader)
        {
            DateTime triggerTime = default;
            FamosFileTimeMode timeMode = FamosFileTimeMode.Unknown;

            var keyVersion = this.DeserializeInt32();

            this.DeserializeKey(keySize =>
            {
                // day
                var day = this.DeserializeInt32();

                if (!(1 <= day && day <= 31))
                    throw new FormatException($"Expected value for 'day' property: '1..31'. Got {day}.");

                // month
                var month = this.DeserializeInt32();

                if (!(1 <= month && month <= 12))
                    throw new FormatException($"Expected value for 'month' property: '1..12'. Got {month}.");

                // year
                var year = this.DeserializeInt32();

                if (year < 1980)
                    throw new FormatException($"Expected value for 'year' property: >= '1980'. Got {year}.");

                // hour
                var hour = this.DeserializeInt32();

                if (!(0 <= hour && hour <= 23))
                    throw new FormatException($"Expected value for 'hour' property: '0..23'. Got {hour}.");

                // minute
                var minute = this.DeserializeInt32();

                if (!(0 <= minute && minute <= 59))
                    throw new FormatException($"Expected value for 'minute' property: '0..59'. Got {minute}.");

                // second
                var second = this.DeserializeReal();

                if (!(0 <= second && second <= 60))
                    throw new FormatException($"Expected value for 'day' property: '0.0..60.0'. Got {second}.");

                // millisecond
                var millisecond = (int)((second - Math.Truncate(second)) * 1000);
                var intSecond = (int)Math.Truncate(second);

                // parse
                if (keyVersion == 1)
                {
                    triggerTime = new DateTime(year, month, day, hour, minute, intSecond, millisecond, DateTimeKind.Unspecified);
                }
                else if (keyVersion == 2)
                {
                    var timeZone = this.DeserializeInt32();

                    timeMode = (FamosFileTimeMode)this.DeserializeInt32();
                    triggerTime = new DateTimeOffset(year, month, day, hour, minute, intSecond, millisecond, TimeSpan.FromMinutes(timeZone)).UtcDateTime;
                }
                else
                {
                    throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
                }
            });

            this.DateTime = triggerTime;
            this.TimeMode = timeMode;
        }

        #endregion

        #region Properties

        public DateTime DateTime { get; set; }
        public FamosFileTimeMode TimeMode { get; set; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.NT;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.DateTime.Day,
                this.DateTime.Month,
                this.DateTime.Year,
                this.DateTime.Hour,
                this.DateTime.Minute,
                (decimal)this.DateTime.Second + this.DateTime.Millisecond / 1000,
                0, // since it is UTC+0 now
                0  // since it is UTC+0 now
            };

            this.SerializeKey(writer, 2, data);
        }

        #endregion
    }
}
