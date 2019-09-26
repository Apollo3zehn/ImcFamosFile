using System;
using System.IO;

namespace FamosFile.NET
{
    public class FamosFileComponent : FamosFileBase
    {
        public FamosFileComponent(BinaryReader reader, int codePage) : base(reader)
        {
            this.CodePage = codePage;
        }

        // Start of component.
        public FamosFileKeyType Parse(FamosFileXAxisScaling currentXAxisScaling, FamosFileZAxisScaling currentZAxisScaling, DateTime currentTriggerTime, FamosFileTimeMode currentTimeMode)
        {
            var nextKeyType = FamosFileKeyType.Unknown;

            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var componentIndex = this.ParseInteger();
                var analogDigital = (FamosFileAnalogDigital)this.ParseInteger();

                while (true)
                {
                    nextKeyType = this.ParseKeyType();

                    // end of CC reached
                    if (nextKeyType == FamosFileKeyType.CT ||
                        nextKeyType == FamosFileKeyType.CB ||
                        nextKeyType == FamosFileKeyType.CI ||
                        nextKeyType == FamosFileKeyType.CG)
                    {
                        break;
                    }

                    else if (nextKeyType == FamosFileKeyType.Unknown)
                    {
                        this.SkipKey();
                        continue;
                    }

                    else if (nextKeyType == FamosFileKeyType.CD)
                        this.XAxisScaling = base.ParseCD();

                    else if (nextKeyType == FamosFileKeyType.CZ)
                        this.ZAxisScaling = base.ParseCZ();

                    else if (nextKeyType == FamosFileKeyType.NT)
                        (this.TriggerTime, this.TimeMode) = base.ParseNT();

                    else if (nextKeyType == FamosFileKeyType.CP)
                        this.ParseCP();

                    else if (nextKeyType == FamosFileKeyType.Cb)
                        this.ParseCb();

                    else if (nextKeyType == FamosFileKeyType.CR)
                        this.ParseCR();

                    else if (nextKeyType == FamosFileKeyType.ND)
                        this.ParseND();

                    else if (nextKeyType == FamosFileKeyType.Cv)
                        this.ParseCv();

                    else if (nextKeyType == FamosFileKeyType.CN)
                        this.ParseCN();

                    else
                        break;
                }
            });
        }

        private FamosFileXAxisScaling XAxisScaling { get; set; }
        private FamosFileZAxisScaling ZAxisScaling { get; set; }
        private DateTime TriggerTime { get; set; }
        private FamosFileTimeMode TimeMode { get; set; }

    }
}
