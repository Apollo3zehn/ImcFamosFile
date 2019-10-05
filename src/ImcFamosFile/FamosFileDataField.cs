using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileDataField : FamosFileBase
    {
        #region Constructors

        public FamosFileDataField()
        {
            this.Initialize();
        }

        public FamosFileDataField(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.Initialize();

            var nextKeyType = FamosFileKeyType.Unknown;

            FamosFileXAxisScaling currentXAxisScaling = null;
            FamosFileZAxisScaling currentZAxisScaling = null;
            FamosFileTriggerTimeInfo currentTriggerTimeInfo = null;

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var componentCount = this.DeserializeInt32();

                this.Type = (FamosFileDataFieldType)this.DeserializeInt32();
                this.Dimension = this.DeserializeInt32();

                if (this.Type == FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 1)
                    throw new FormatException($"The field dimension is invalid. Expected '1', got '{this.Dimension}'.");

                if (this.Type > FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 2)
                    throw new FormatException($"The field dimension is invalid. Expected '2', got '{this.Dimension}'.");
            });

            while (true)
            {
                nextKeyType = this.DeserializeKeyType();

                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                else if (nextKeyType == FamosFileKeyType.CD)
                    currentXAxisScaling = base.DeserializeCD();

                else if (nextKeyType == FamosFileKeyType.CZ)
                    currentZAxisScaling = base.DeserializeCZ();

                else if (nextKeyType == FamosFileKeyType.NT)
                    currentTriggerTimeInfo = base.DeserializeNT();

                else if (nextKeyType == FamosFileKeyType.CC)
                {
                    var component = new FamosFileComponent(this.Reader, this.CodePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo);
                    this.Components.Add(component);
                }

                else
                {
                    // go back to start of key
                    this.Reader.BaseStream.Position -= 4;
                    break;
                }
            }
        }

        #endregion

        #region Properties

        public FamosFileDataFieldType Type { get; set; }
        public int Dimension { get; set; }
        public List<FamosFileComponent> Components { get; set; }

        #endregion

        #region Methods

        public void Initialize()
        {
            this.Type = FamosFileDataFieldType.MultipleYToSingleEquidistantTime;
            this.Dimension = 1;
            this.Components = new List<FamosFileComponent>();
        }

        #endregion
    }
}
