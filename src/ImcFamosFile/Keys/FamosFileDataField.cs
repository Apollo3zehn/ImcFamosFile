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
            FamosFileTriggerTime? currentTriggerTime = null;

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
                    currentTriggerTime = new FamosFileTriggerTime(this.Reader);

                // CC
                else if (nextKeyType == FamosFileKeyType.CC)
                {
                    var component = new FamosFileComponent.Deserializer(this.Reader, this.CodePage).Deserialize(currentXAxisScaling, currentZAxisScaling, currentTriggerTime);

                    currentXAxisScaling = component.XAxisScaling;
                    currentZAxisScaling = component.ZAxisScaling;
                    currentTriggerTime = component.TriggerTime;

                    this.Components.Add(component);
                }

                // CV
                else if (nextKeyType == FamosFileKeyType.CV)
                    this.EventInfos.Add(new FamosFileEventInfo(this.Reader));

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                    throw new FormatException("Although the format specification allows '|Cb' keys at any level, this implementation supports this key only at component level. Please send a sample file to the project maintainer to overcome this limitation in future.");

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
        public List<FamosFileEventInfo> EventInfos { get; private set; } = new List<FamosFileEventInfo>();

        public string Name
        {
            get
            {
                var name = string.Empty;

                foreach (var component in this.Components)
                {
                    if (!string.IsNullOrWhiteSpace(component.Name))
                    {
                        name = component.Name;
                        break;
                    }
                }

                return name;
            }
        }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CG;

        #endregion

        #region Methods

        public override void Validate()
        {
            // validate components
            foreach (var component in this.Components)
            {
                component.Validate();
            }

            // check that there are at least as many components as we have dimensions
            if (this.Components.Count < this.Dimension)
                throw new FormatException($"Expected number of data field components is >= '{this.Dimension}', got '{this.Components.Count}'.");

            // check that there is at least a single channel defined
            if (!this.Components.SelectMany(component => component.Channels).Any())
                throw new FormatException($"For a data field there must be at least one component with a minimum of one channel defined.");

            // check that ValidCR2 is 0 if there is only a single component
            var eventReference = this.Components.First().EventReference;

            if (this.Components.Count == 1 && eventReference != null)
            {
                if (eventReference.ValidCR2 != 0)
                    throw new FormatException($"For a data field with a single component, the ValidCR2 property of the component's event location info must be '0'.");
            }

            // check if event locations info's event info is part of this instance
            foreach (var current in this.Components.Select(component => component.EventReference))
            {
                if (current != null)
                {
                    if (!this.EventInfos.Contains(current.EventInfo))
                    {
                        throw new FormatException("The event location info' event info must be part of the data field's event info collection.");
                    };
                }
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

            // update event info indices
            foreach (var eventInfo in this.EventInfos)
            {
                eventInfo.Index = this.EventInfos.IndexOf(eventInfo) + 1;
            }

            // update event info index of event location infos
            foreach (var eventReference in this.Components.Select(component => component.EventReference))
            {
                if (eventReference != null)
                {
                    var eventInfoIndex = this.EventInfos.IndexOf(eventReference.EventInfo) + 1;
                    eventReference.EventInfoIndex = eventInfoIndex;
                }
            }
        }

        internal override void Serialize(BinaryWriter writer)
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
                if (firstComponent.TriggerTime != null)
                    firstComponent.TriggerTime.Serialize(writer);

                // CC
                foreach (var component in this.Components)
                {
                    component.Serialize(writer);
                }
            }

            // CV
            foreach (var eventInfo in this.EventInfos)
            {
                eventInfo.Serialize(writer);
            }
        }

        internal override void AfterDeserialize()
        {
            // check if event info indices are consistent
            base.CheckIndexConsistency("event info", this.EventInfos, current => current.Index);
            this.EventInfos = this.EventInfos.OrderBy(x => x.Index).ToList();

            foreach (var eventInfo in this.EventInfos)
            {
                eventInfo.AfterDeserialize();
            }

            // assign event info to event location info
            foreach (var eventReference in this.Components.Select(component => component.EventReference))
            {
                if (eventReference != null)
                    eventReference.EventInfo = this.EventInfos.First(eventInfo => eventInfo.Index == eventReference.EventInfoIndex);
            }

            // prepare components
            foreach (var component in this.Components)
            {
                component.AfterDeserialize();
            }
        }

        #endregion
    }
}
