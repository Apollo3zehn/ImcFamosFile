using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    /// <summary>
    /// A field is a collection of components, e.g. to represent X and Y data of a measurement.
    /// </summary>
    public class FamosFileField : FamosFileBaseExtended
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileField"/> class.
        /// </summary>
        public FamosFileField()
        {
            //
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileField"/> class.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        public FamosFileField(FamosFileFieldType type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileField"/> class.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="components">A list of components belonging to the field.</param>
        public FamosFileField(FamosFileFieldType type, List<FamosFileComponent> components)
        {
            this.Type = type;
            this.Components.AddRange(components);
        }

        internal FamosFileField(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var componentCount = this.DeserializeInt32();
                var type = (FamosFileFieldType)this.DeserializeInt32();
                var dimension = this.DeserializeInt32();

                this.Type = type;

                if (dimension != this.Dimension)
                    throw new FormatException($"The data field dimension is invalid. Expected '{this.Dimension}', got '{dimension}'.");

                if (this.Type == FamosFileFieldType.MultipleYToSingleEquidistantTime &&
                    this.Dimension != 1)
                    throw new FormatException($"The field dimension is invalid. Expected '1', got '{this.Dimension}'.");

                if (this.Type > FamosFileFieldType.MultipleYToSingleEquidistantTime &&
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
                    this.XAxisScaling = new FamosFileXAxisScaling(this.Reader, this.CodePage);

                // CZ
                else if (nextKeyType == FamosFileKeyType.CZ)
                    this.ZAxisScaling = new FamosFileZAxisScaling(this.Reader, this.CodePage);

                // NT
                else if (nextKeyType == FamosFileKeyType.NT)
                    this.TriggerTime = new FamosFileTriggerTime(this.Reader);

                // CC
                else if (nextKeyType == FamosFileKeyType.CC)
                {
                    var component = new FamosFileComponent.Deserializer(this.Reader, this.CodePage).Deserialize();
                    this.Components.Add(component);
                }

                // CV
                else if (nextKeyType == FamosFileKeyType.CV)
                    throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
                    //this.EventInfos.Add(new FamosFileEventInfo(this.Reader));

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

        /// <summary>
        /// Gets or sets the type of the field. Default is <see cref="FamosFileFieldType.MultipleYToSingleEquidistantTime"/>.
        /// </summary>
        public FamosFileFieldType Type { get; set; } = FamosFileFieldType.MultipleYToSingleEquidistantTime;

        /// <summary>
        /// Gets number of dimensions.
        /// </summary>
        public int Dimension => this.Type == FamosFileFieldType.MultipleYToSingleEquidistantTime ? 1 : 2;

        /// <summary>
        /// Gets a list of components.
        /// </summary>
        public List<FamosFileComponent> Components { get; } = new List<FamosFileComponent>();

        /// <summary>
        /// Gets a list of event infos.
        /// </summary>
        public List<FamosFileEventInfo> EventInfos { get; private set; } = new List<FamosFileEventInfo>();

        /// <summary>
        /// Gets or sets the x-axis scaling. If set, it will be applied to all components unless redefined by a component.
        /// </summary>
        public FamosFileXAxisScaling? XAxisScaling { get; set; }

        /// <summary>
        /// Gets or sets the z-axis scaling. If set, it will be applied to all components unless redefined by a component.
        /// </summary>
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }

        /// <summary>
        /// Gets or sets the trigger time. If set, it will be applied to all components unless redefined by a component.
        /// </summary>
        public FamosFileTriggerTime? TriggerTime { get; set; }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CG;

        #endregion

        #region Methods

        /// <summary>
        /// Gets all channels that are part of the components belonging to this instance. 
        /// </summary>
        /// <returns>Returns a list of <see cref="FamosFileChannel"/>.</returns>
        public List<FamosFileChannel> GetChannels()
        {
            return this.Components.SelectMany(component => component.Channels).ToList();
        }

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

            // check that the data type of time data is UInt32. (this causes no error in Famos)
            //if (this.Type == FamosFileFieldType.MultipleYToSingleMonotonousTime
            //&& !this.Components.Any(component => component.Type == FamosFileDataComponentType.Primary
            //                                  && component.PackInfo.DataType == FamosFileDataType.UInt32))
            //    throw new FormatException("A field with monotonous time axis must have a primary component with data type UInt32.");

            // check number of components
            var primaryCount = this.Components.Count(component => component.Type == FamosFileComponentType.Primary);
            var secondaryCount = this.Components.Count(component => component.Type == FamosFileComponentType.Secondary);

            switch (this.Type)
            {
                case FamosFileFieldType.MultipleYToSingleEquidistantTime:

                    if (secondaryCount > 0)
                        throw new FormatException($"A field of type '{nameof(FamosFileFieldType.MultipleYToSingleEquidistantTime)}', must have no components of type '{nameof(FamosFileComponentType.Secondary)}'.");

                    break;

                case FamosFileFieldType.MultipleYToSingleMonotonousTime:

                    if (primaryCount == 0)
                        throw new FormatException($"A field of type '{nameof(FamosFileFieldType.MultipleYToSingleMonotonousTime)}', must contain one or more '{nameof(FamosFileComponentType.Primary)}' components.");

                    if (secondaryCount != 1)
                        throw new FormatException($"A field of type '{nameof(FamosFileFieldType.MultipleYToSingleMonotonousTime)}', must contain a single '{nameof(FamosFileComponentType.Secondary)}' component (the time axis) and one or more '{nameof(FamosFileComponentType.Primary)}' components.");

                    break;

                case FamosFileFieldType.MultipleYToSingleXOrViceVersa:

                    if (!((primaryCount == 1 && secondaryCount >= 1) || (primaryCount >= 1 && secondaryCount == 1)))
                        throw new FormatException($"A field of type '{nameof(FamosFileFieldType.MultipleYToSingleXOrViceVersa)}', must contain a single '{nameof(FamosFileComponentType.Primary)}' component and one or more '{nameof(FamosFileComponentType.Secondary)}' components or vice versa.");

                    break;

                case FamosFileFieldType.ComplexRealImaginary:
                case FamosFileFieldType.ComplexMagnitudePhase:
                case FamosFileFieldType.ComplexMagnitudeDBPhase:

                    if (primaryCount != 1 || secondaryCount != 1)
                        throw new FormatException($"A complex field must contain a single '{nameof(FamosFileComponentType.Primary)}' component and a single '{nameof(FamosFileComponentType.Secondary)}' component.");

                    break;

                default:
                    break;
            }

            /* check unique region */
            if (this.Components.Count != this.Components.Distinct().Count())
                throw new FormatException("A component must be added only once.");

            /* not yet supported region */
            if (this.EventInfos.Any())
                throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
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

            // CD
            this.XAxisScaling?.Serialize(writer);

            // CZ
            this.ZAxisScaling?.Serialize(writer);

            // NT
            this.TriggerTime?.Serialize(writer);

            // CC
            foreach (var component in this.Components)
            {
                component.Serialize(writer);
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
