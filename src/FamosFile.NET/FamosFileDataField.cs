using System;
using System.Collections.Generic;
using System.IO;

namespace FamosFile.NET
{
    public class FamosFileDataField : FamosFileBase
    {
        #region Constructors

        public FamosFileDataField(BinaryReader reader, int codePage) : base(reader)
        {
            this.Components = new List<FamosFileComponent>();
            this.CodePage = codePage;
        }

        #endregion

        #region Properties

        public FamosFileDataFieldType Type { get; set; }
        public int Dimension { get; set; }
        public List<FamosFileComponent> Components { get; set; }

        #endregion

        #region KeyParsing

        public FamosFileKeyType Parse()
        {
            var nextKeyType = FamosFileKeyType.Unknown;

            FamosFileXAxisScaling currentXAxisScaling = null;
            FamosFileZAxisScaling currentZAxisScaling = null;
            DateTime currentTriggerTime = default;
            FamosFileTimeMode currentTimeMode = default;

            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                var componentCount = this.ParseInt32();

                this.Type = (FamosFileDataFieldType)this.ParseInt32();
                this.Dimension = this.ParseInt32();

                if (this.Type == FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 1)
                    throw new FormatException($"The field dimension is invalid. Expected '1', got '{this.Dimension}'.");

                if (this.Type > FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 2)
                    throw new FormatException($"The field dimension is invalid. Expected '2', got '{this.Dimension}'.");
            });

            var parseKey = true;

            while (true)
            {
                // if key has not been parsed yet
                if (parseKey)
                    nextKeyType = this.ParseKeyType();
                else
                    parseKey = true;

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

            return nextKeyType;
        }

        #endregion
    }
}
