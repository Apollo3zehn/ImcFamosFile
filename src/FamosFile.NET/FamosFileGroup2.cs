using System;
using System.Collections.Generic;
using System.IO;

namespace FamosFile.NET
{
    public class FamosFileGroup2 : FamosFileBase
    {
        public FamosFileGroup2(BinaryReader reader, int codePage) : base(reader)
        {
            this.Components = new List<FamosFileComponent>();
            this.CodePage = codePage;
        }

        // Group of components.
        public FamosFileKeyType Parse()
        {
            var nextKeyType = FamosFileKeyType.Unknown;

            FamosFileXAxisScaling currentXAxisScaling = null;
            FamosFileZAxisScaling currentZAxisScaling = null;
            DateTime currentTriggerTime = default;
            FamosFileTimeMode currentTimeMode = default;

            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var componentCount = this.ParseInteger();

                this.FieldType = (FamosFileFieldType)this.ParseInteger();
                this.Dimension = this.ParseInteger();

                var parseKey = true;

                while (true)
                {
                    // if key has not been parsed yet
                    if (parseKey)
                    {
                        nextKeyType = this.ParseKeyType();
                        parseKey = true;
                    }

                    if (nextKeyType == FamosFileKeyType.Unknown)
                    {
                        this.SkipKey();
                        continue;
                    }

                    else if (nextKeyType == FamosFileKeyType.CD)
                        currentXAxisScaling = base.ParseCD();

                    else if (nextKeyType == FamosFileKeyType.CZ)
                        currentZAxisScaling = base.ParseCZ();

                    else if (nextKeyType == FamosFileKeyType.NT)
                        (currentTriggerTime, currentTimeMode) = base.ParseNT();

                    else if (nextKeyType == FamosFileKeyType.CC)
                    {
                        var component = new FamosFileComponent(this.Reader, this.CodePage);

                        nextKeyType = component.Parse(currentXAxisScaling, currentZAxisScaling, currentTriggerTime, currentTimeMode);
                        parseKey = false;

                        this.Components.Add(component);
                    }

                    else
                        break;
                }
            });

            return nextKeyType;
        }

        public FamosFileFieldType FieldType { get; set; }
        public int Dimension { get; set; }
        public List<FamosFileComponent> Components { get; set; }
    }
}
