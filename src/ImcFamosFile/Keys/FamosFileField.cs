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
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileField"/> class.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="components">A list of components belonging to the field.</param>
        public FamosFileField(FamosFileFieldType type, List<FamosFileComponent> components)
        {
            Type = type;
            Components.AddRange(components);
        }

        internal FamosFileField(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var componentCount = DeserializeInt32();
                var type = (FamosFileFieldType)DeserializeInt32();
                var dimension = DeserializeInt32();

                Type = type;

                if (dimension != Dimension)
                    throw new FormatException($"The data field dimension is invalid. Expected '{Dimension}', got '{dimension}'.");

                if (Type == FamosFileFieldType.MultipleYToSingleEquidistantTime &&
                    Dimension != 1)
                    throw new FormatException($"The field dimension is invalid. Expected '1', got '{Dimension}'.");

                if (Type > FamosFileFieldType.MultipleYToSingleEquidistantTime &&
                    Dimension != 2)
                    throw new FormatException($"The field dimension is invalid. Expected '2', got '{Dimension}'.");
            });

            while (true)
            {
                if (Reader.BaseStream.Position >= Reader.BaseStream.Length)
                    return;

                var nextKeyType = DeserializeKeyType();

                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    SkipKey();
                    continue;
                }

                // CD
                else if (nextKeyType == FamosFileKeyType.CD)
                    XAxisScaling = new FamosFileXAxisScaling(Reader, CodePage);

                // CZ
                else if (nextKeyType == FamosFileKeyType.CZ)
                    ZAxisScaling = new FamosFileZAxisScaling(Reader, CodePage);

                // NT
                else if (nextKeyType == FamosFileKeyType.NT)
                    TriggerTime = new FamosFileTriggerTime(Reader);

                // CC
                else if (nextKeyType == FamosFileKeyType.CC)
                {
                    var component = new FamosFileComponent.Deserializer(Reader, CodePage).Deserialize();
                    Components.Add(component);
                }

                // CV
                else if (nextKeyType == FamosFileKeyType.CV)
                    throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
                    //EventInfos.Add(new FamosFileEventInfo(Reader));

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                    throw new FormatException("Although the format specification allows '|Cb' keys at any level, this implementation supports this key only at component level. Please send a sample file to the project maintainer to overcome this limitation in future.");

                else
                {
                    // go back to start of key
                    Reader.BaseStream.Position -= 4;
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
        public int Dimension => Type == FamosFileFieldType.MultipleYToSingleEquidistantTime ? 1 : 2;

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

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CG;

        #endregion

        #region Methods

        /// <summary>
        /// Gets all channels that are part of the components belonging to this instance. 
        /// </summary>
        /// <returns>Returns a list of <see cref="FamosFileChannel"/>.</returns>
        public List<FamosFileChannel> GetChannels()
        {
            return Components.SelectMany(component => component.Channels).ToList();
        }

        /// <inheritdoc />
        public override void Validate()
        {
            // validate components
            foreach (var component in Components)
            {
                component.Validate();
            }

            // check that there are at least as many components as we have dimensions
            if (Components.Count < Dimension)
                throw new FormatException($"Expected number of data field components is >= '{Dimension}', got '{Components.Count}'.");

            // check that there is at least a single channel defined
            if (!Components.SelectMany(component => component.Channels).Any())
                throw new FormatException($"For a data field there must be at least one component with a minimum of one channel defined.");

            // check that ValidCR2 is 0 if there is only a single component
            var eventReference = Components.First().EventReference;

            if (Components.Count == 1 && eventReference != null)
            {
                if (eventReference.ValidCR2 != 0)
                    throw new FormatException($"For a data field with a single component, the ValidCR2 property of the component's event location info must be '0'.");
            }

            // check if event locations info's event info is part of this instance
            foreach (var current in Components.Select(component => component.EventReference))
            {
                if (current != null)
                {
                    if (!EventInfos.Contains(current.EventInfo))
                    {
                        throw new FormatException("The event location info' event info must be part of the data field's event info collection.");
                    };
                }
            }

            // check that the data type of time data is UInt32. (this causes no error in Famos)
            //if (Type == FamosFileFieldType.MultipleYToSingleMonotonousTime
            //&& !Components.Any(component => component.Type == FamosFileDataComponentType.Primary
            //                                  && component.PackInfo.DataType == FamosFileDataType.UInt32))
            //    throw new FormatException("A field with monotonous time axis must have a primary component with data type UInt32.");

            // check number of components
            var primaryCount = Components.Count(component => component.Type == FamosFileComponentType.Primary);
            var secondaryCount = Components.Count(component => component.Type == FamosFileComponentType.Secondary);

            switch (Type)
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
            if (Components.Count != Components.Distinct().Count())
                throw new FormatException("A component must be added only once.");

            /* not yet supported region */
            if (EventInfos.Any())
                throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
        }

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
            // prepare components
            foreach (var component in Components)
            {
                component.BeforeSerialize();
            }

            // update event info indices
            foreach (var eventInfo in EventInfos)
            {
                eventInfo.Index = EventInfos.IndexOf(eventInfo) + 1;
            }

            // combine properties of all components
            CombineComponentProperties();

            // update event info index of event location infos
            foreach (var eventReference in Components.Select(component => component.EventReference))
            {
                if (eventReference != null)
                {
                    var eventInfoIndex = EventInfos.IndexOf(eventReference.EventInfo) + 1;
                    eventReference.EventInfoIndex = eventInfoIndex;
                }
            }
        }

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                Components.Count,
                (int)Type,
                Dimension
            };

            SerializeKey(writer, 1, data);

            // CD
            XAxisScaling?.Serialize(writer);

            // CZ
            ZAxisScaling?.Serialize(writer);

            // NT
            TriggerTime?.Serialize(writer);

            // CC
            foreach (var component in Components)
            {
                component.Serialize(writer);
            }

            // CV
            foreach (var eventInfo in EventInfos)
            {
                eventInfo.Serialize(writer);
            }
        }

        internal override void AfterSerialize()
        {
            ExpandComponentProperties();
        }

        internal override void AfterDeserialize()
        {
            // check if event info indices are consistent
            base.CheckIndexConsistency("event info", EventInfos, current => current.Index);
            EventInfos = EventInfos.OrderBy(x => x.Index).ToList();

            foreach (var eventInfo in EventInfos)
            {
                eventInfo.AfterDeserialize();
            }

            // assign event info to event location info
            foreach (var eventReference in Components.Select(component => component.EventReference))
            {
                if (eventReference != null)
                    eventReference.EventInfo = EventInfos.First(eventInfo => eventInfo.Index == eventReference.EventInfoIndex);
            }

            // expand properties of all components
            ExpandComponentProperties();

            // prepare components
            foreach (var component in Components)
            {
                component.AfterDeserialize();
            }
        }

        private void CombineComponentProperties()
        {
            if (Components.Any())
            {
                var firstComponent = Components.First();

                TriggerTime ??= firstComponent.TriggerTime;

                XAxisScaling ??= firstComponent.XAxisScaling;

                ZAxisScaling ??= firstComponent.ZAxisScaling;
            }

            var currentTriggerTime = TriggerTime;
            var currentXAxisScaling = XAxisScaling;
            var currentZAxisScaling = ZAxisScaling;

            foreach (var component in Components)
            {
                // trigger time
                if (component.TriggerTime != null)
                {
                    if (currentTriggerTime != null && component.TriggerTime.Equals(currentTriggerTime))
                        component.TriggerTime = null;
                    else
                        currentTriggerTime = component.TriggerTime;
                }

                // x-axis scaling
                if (component.XAxisScaling != null)
                {
                    if (currentXAxisScaling != null && component.XAxisScaling.Equals(currentXAxisScaling))
                        component.XAxisScaling = null;
                    else
                        currentXAxisScaling = component.XAxisScaling;
                }

                // z-axis scaling
                if (component.ZAxisScaling != null)
                {
                    if (currentZAxisScaling != null && component.ZAxisScaling.Equals(currentZAxisScaling))
                        component.ZAxisScaling = null;
                    else
                        currentZAxisScaling = component.ZAxisScaling;
                }
            }
        }

        private void ExpandComponentProperties()
        {
            var currentTriggerTime = TriggerTime;
            var currentXAxisScaling = XAxisScaling;
            var currentZAxisScaling = ZAxisScaling;

            foreach (var component in Components)
            {
                // trigger time
                if (component.TriggerTime == null)
                    component.TriggerTime = currentTriggerTime?.Clone();
                else
                    currentTriggerTime = component.TriggerTime;

                // x-axis scaling
                if (component.XAxisScaling == null)
                    component.XAxisScaling = currentXAxisScaling?.Clone();
                else
                    currentXAxisScaling = component.XAxisScaling;

                // z-axis scaling
                if (component.ZAxisScaling == null)
                    component.ZAxisScaling = currentZAxisScaling?.Clone();
                else
                    currentZAxisScaling = component.ZAxisScaling;
            }
        }

        #endregion
    }
}
