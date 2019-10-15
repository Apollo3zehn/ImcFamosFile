using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileDataField : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileDataField()
        {
            //
        }

        public FamosFileDataField(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            FamosFileXAxisScaling? currentXAxisScaling = null;
            FamosFileZAxisScaling? currentZAxisScaling = null;
            FamosFileTriggerTimeInfo? currentTriggerTimeInfo = null;

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var componentCount = this.DeserializeInt32();
                var type = (FamosFileDataFieldType)this.DeserializeInt32();
                var dimension = this.DeserializeInt32();

                this.Type = type;

                if (dimension != this.Dimension)
                    throw new FormatException($"The data field dimension is invalid. Expected '{this.Dimension}', got '{dimension}'.");

                if (this.Type == FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 1)
                    throw new FormatException($"The field dimension is invalid. Expected '1', got '{this.Dimension}'.");

                if (this.Type > FamosFileDataFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 2)
                    throw new FormatException($"The field dimension is invalid. Expected '2', got '{this.Dimension}'.");
            });

            while (true)
            {
                if (this.Reader.BaseStream.Position >= this.Reader.BaseStream.Length)
                    return;

                var nextKeyType = this.DeserializeKeyType();

                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                // CD
                else if (nextKeyType == FamosFileKeyType.CD)
                    currentXAxisScaling = new FamosFileXAxisScaling(this.Reader, this.CodePage);

                // CZ
                else if (nextKeyType == FamosFileKeyType.CZ)
                    currentZAxisScaling = new FamosFileZAxisScaling(this.Reader, this.CodePage);

                // NT
                else if (nextKeyType == FamosFileKeyType.NT)
                    currentTriggerTimeInfo = new FamosFileTriggerTimeInfo(this.Reader);

                // CC
                else if (nextKeyType == FamosFileKeyType.CC)
                {
                    var component = new FamosFileComponentDeserializer().Deserialize(this.Reader, this.CodePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo);

                    currentXAxisScaling = component.XAxisScaling;
                    currentZAxisScaling = component.ZAxisScaling;
                    currentTriggerTimeInfo = component.TriggerTimeInfo;

                    this.Components.Add(component);
                }

                // CV
                else if (nextKeyType == FamosFileKeyType.CV)
                    this.EventInfo = new FamosFileEventInfo(this.Reader);

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

        public FamosFileDataFieldType Type { get; set; } = FamosFileDataFieldType.MultipleYToSingleEquidistantTime;
        public int Dimension => this.Type == FamosFileDataFieldType.MultipleYToSingleEquidistantTime ? 1 : 2;
        public List<FamosFileComponent> Components { get; } = new List<FamosFileComponent>();
        public FamosFileEventInfo? EventInfo { get; private set; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CG;

        #endregion

        #region Methods

        internal override void Validate()
        {
            if (this.Components.Count < this.Dimension)
                throw new FormatException($"Expected number of data field components is >= '{this.Dimension}', got '{this.Components.Count}'.");

            // validate components
            foreach (var component in this.Components)
            {
                component.Validate();
            }
        }

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
            // prepare components
            foreach (var component in this.Components)
            {
                component.BeforeSerialize();
            }

            // prepare event info
            this.EventInfo?.BeforeSerialize();
        }

        internal override void Serialize(StreamWriter writer)
        {
            var data = new object[]
            {
                this.Components.Count,
                (int)this.Type,
                this.Dimension
            };

            this.SerializeKey(writer, 1, data);

            if (this.Components.Any())
            {
                var firstComponent = this.Components.First();

                // CD
                if (firstComponent.XAxisScaling != null)
                    firstComponent.XAxisScaling.Serialize(writer);

                // CZ
                if (firstComponent.ZAxisScaling != null)
                    firstComponent.ZAxisScaling.Serialize(writer);

                // NT
                if (firstComponent.TriggerTimeInfo != null)
                    firstComponent.TriggerTimeInfo.Serialize(writer);

                // CC
                foreach (var component in this.Components)
                {
                    component.Serialize(writer);
                }
            }

            // CV
            this.EventInfo?.Serialize(writer);
        }

        internal override void AfterDeserialize()
        {
            // prepare components
            foreach (var component in this.Components)
            {
                component.AfterDeserialize();
            }

            // prepare event info
            this.EventInfo?.AfterDeserialize();
        }

        #endregion
    }
}
