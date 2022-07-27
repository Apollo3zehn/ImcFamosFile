namespace ImcFamosFile
{
    /// <summary>
    /// A base class for a component, which is a full description of a single dataset.
    /// </summary>
    public abstract class FamosFileComponent : FamosFileBaseExtended
    {
        #region Fields

        private FamosFileComponentType _type;

        #endregion

        #region Constructors

        private protected FamosFileComponent(FamosFileDataType dataType, int length, FamosFileComponentType componentType)
        {
            Type = componentType;
            BufferInfo = new FamosFileBufferInfo(new List<FamosFileBuffer>() { new FamosFileBuffer() });
            PackInfo = new FamosFilePackInfo(dataType, BufferInfo.Buffers.ToList());

            var buffer = BufferInfo.Buffers.First();
            buffer.Length = length * PackInfo.ValueSize;
            buffer.ConsumedBytes = buffer.Length; // This could theoretically be set to actual value during 'famosFile.Save(...);' but how handle interlaced data?
        }

        private protected FamosFileComponent(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            FamosFilePackInfo? packInfo = null;
            FamosFileBufferInfo? bufferInfo = null;
            FamosFilePropertyInfo? propertyInfo = null;

            while (true)
            {
                var nextKeyType = DeserializeKeyType();

                if (propertyInfo != null && nextKeyType != FamosFileKeyType.CN)
                    throw new FormatException("A channel info of type '|CN' was expected because a property info of type '|Np' has been defined previously.");

                // end of CC reached
                if (nextKeyType == FamosFileKeyType.CT ||
                    nextKeyType == FamosFileKeyType.CI ||
                    nextKeyType == FamosFileKeyType.CB ||
                    nextKeyType == FamosFileKeyType.CG ||
                    nextKeyType == FamosFileKeyType.CC ||
                    nextKeyType == FamosFileKeyType.CV ||
                    nextKeyType == FamosFileKeyType.CS)
                {
                    // go back to start of key
                    Reader.BaseStream.Position -= 4;
                    break;
                }

                else if (nextKeyType == FamosFileKeyType.Unknown)
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

                // CP
                else if (nextKeyType == FamosFileKeyType.CP)
                    packInfo = new FamosFilePackInfo(Reader);

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                {
                    if (bufferInfo == null)
                        bufferInfo = new FamosFileBufferInfo(Reader);
                    else
                        throw new FormatException("Although the format specification allows multiple '|Cb' keys, this implementation supports only a single definition per component. Please send a sample file to the project maintainer to overcome this limitation in future.");
                }

                // CR
                else if (nextKeyType == FamosFileKeyType.CR)
                    DeserializeCR();

                // ND
                else if (nextKeyType == FamosFileKeyType.ND)
                    DisplayInfo = new FamosFileDisplayInfo(Reader);

                // Cv
                else if (nextKeyType == FamosFileKeyType.Cv)
                    throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
                    //EventReference = new FamosFileEventReference(Reader);

                // CN
                else if (nextKeyType == FamosFileKeyType.CN)
                {
                    Channels.Add(new FamosFileChannel(Reader, CodePage));
                    Channels.Last().PropertyInfo = propertyInfo;

                    propertyInfo = null;
                }

                // Np
                else if (nextKeyType == FamosFileKeyType.Np)
                    propertyInfo = new FamosFilePropertyInfo(Reader, CodePage);

                else
                    // should never happen
                    throw new FormatException("An unexpected state has been reached.");
            }

            if (packInfo is null)
                throw new FormatException("No pack information was found in the component.");
            else
                PackInfo = packInfo;

            if (bufferInfo is null)
                throw new FormatException("No buffer information was found in the component.");
            else
                BufferInfo = bufferInfo;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.
        /// </summary>
        public FamosFileComponentType Type
        {
            get { return _type; }
            private set
            {
                if (value != FamosFileComponentType.Primary && value != FamosFileComponentType.Secondary)
                    throw new FormatException($"The component type enum value is invalid.");

                _type = value;
            }
        }

        /// <summary>
        /// Gets or sets the x-axis scaling of this component. If set, it will be applied to all subsequent components in the higher-level <see cref="FamosFileField"/>.
        /// </summary>
        public FamosFileXAxisScaling? XAxisScaling { get; set; }

        /// <summary>
        /// Gets or sets the z-axis scaling of this component. If set, it will be applied to all subsequent components in the higher-level <see cref="FamosFileField"/>.
        /// </summary>
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }

        /// <summary>
        /// Gets or sets the trigger time of this component. If set, it will be applied to all subsequent components in the higher-level <see cref="FamosFileField"/>.
        /// </summary>
        public FamosFileTriggerTime? TriggerTime { get; set; }

        /// <summary>
        /// Gets or sets the pack info containing a description of this components data layout.
        /// </summary>
        public FamosFilePackInfo PackInfo { get; set; }

        /// <summary>
        /// Gets or sets the buffer info containing a list of <see cref="FamosFileBuffer"/>.
        /// </summary>
        public FamosFileBufferInfo BufferInfo { get; set; }

        /// <summary>
        /// Gets or sets the display info to describe how to display the data.
        /// </summary>
        public FamosFileDisplayInfo? DisplayInfo { get; set; }

        /// <summary>
        /// Gets or sets the event reference containing a description of related events.
        /// </summary>
        public FamosFileEventReference? EventReference { get; set; }

        /// <summary>
        /// Gets a list of <see cref="FamosFileChannel"/>. In FAMOS, each channel is displayed individually and can be assigned to a group. For digital components, there should be one channel per bit.
        /// </summary>
        public List<FamosFileChannel> Channels { get; } = new List<FamosFileChannel>();

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CC;

        #endregion

        #region Relay Properties

        /// <summary>
        /// Gets the name of the first channel found in the channel list.
        /// </summary>
        public string Name
        {
            get
            {
                var name = string.Empty;

                foreach (var channel in Channels)
                {
                    if (!string.IsNullOrWhiteSpace(channel.Name))
                    {
                        name = channel.Name;
                        break;
                    }
                }

                return name;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calulates the size of this component depending on the values of <see cref="BufferInfo"/> and <see cref="PackInfo"/>.
        /// </summary>
        /// <returns>Returns the size of the component.</returns>
        public int GetSize()
        {
            return GetSize(0, 0);
        }

        /// <summary>
        /// Calulates the size of this component depending on the values of <see cref="BufferInfo"/> and <see cref="PackInfo"/>.
        /// </summary>
        /// <param name="start">The number of values to skip.</param>
        /// <returns>Returns the size of the component.</returns>
        public int GetSize(int start)
        {
            return GetSize(start, 0);
        }

        /// <summary>
        /// Calulates the size of this component depending on the values of <see cref="BufferInfo"/> and <see cref="PackInfo"/>.
        /// </summary>
        /// <param name="start">The number of values to skip.</param>
        /// <param name="length">The number of values to take.</param>
        /// <returns>Returns the size of the component.</returns>
        public int GetSize(int start, int length)
        {
            var packInfo = PackInfo;
            var buffer = PackInfo.Buffers.First();

            int maxLength;

            var actualBufferLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset;

            if (packInfo.IsContiguous)
            {
                maxLength = actualBufferLength / packInfo.ValueSize;
            }
            else
            {
                var rowSize = packInfo.ByteRowSize;
                maxLength = actualBufferLength / rowSize;

                if (actualBufferLength % rowSize >= packInfo.ValueSize)
                    maxLength += 1;
            }

            if (start + length > maxLength)
                throw new InvalidOperationException($"The specified '{nameof(start)}' and '{nameof(length)}' parameters lead to a dataset which is larger than the actual buffer.");

            return length > 0 ? length : maxLength;
        }

        /// <inheritdoc />
        public override void Validate()
        {
            // validate pack info's buffers
            if (!PackInfo.Buffers.Any())
                throw new FormatException("The pack info's buffers collection must container at least a single buffer instance.");

            foreach (var buffer in PackInfo.Buffers)
            {
                if (!BufferInfo.Buffers.Contains(buffer))
                    throw new FormatException("The pack info's buffers must be part of the component's buffer collection.");
            }

            // validate pack info
            PackInfo.Validate();

            // validate buffer info
            BufferInfo.Validate();

            /* check unique region */
            if (Channels.Count != Channels.Distinct().Count())
                throw new FormatException("A channel must be added only once.");

            /* not yet supported region */
            foreach (var channel in Channels)
            {
                if (channel.BitIndex > 0)
                    throw new InvalidOperationException("This implementation does not support processing boolean data yet. Please send a sample file to the package author to find a solution.");
            }

            if (EventReference != null)
                throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");

            if (PackInfo.GroupSize > 1)
                throw new InvalidOperationException("This implementation does not yet support processing data with a pack info group size > '1'. Please send a sample file to the package author to find a solution.");

            if (PackInfo.Mask != 0)
                throw new InvalidOperationException("This implementation does not yet support processing masked data. Please send a sample file to the package author to find a solution.");

            if (PackInfo.Buffers.First().IsRingBuffer)
                throw new InvalidOperationException("This implementation does not yet support processing ring buffers. Please send a sample file to the package author to find a solution.");
        }

        private protected abstract void SerializeCR(BinaryWriter writer);

        private protected abstract void DeserializeCR();

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
            //
        }

        internal override void Serialize(BinaryWriter writer)
        {
            // CC
            var data = new object[]
            {
                    (int)Type,
                    GetType() == typeof(FamosFileAnalogComponent) ? 1 : 2
            };

            SerializeKey(writer, 1, data);

            // CD
            XAxisScaling?.Serialize(writer);

            // CZ
            ZAxisScaling?.Serialize(writer);

            // NT
            TriggerTime?.Serialize(writer);

            // CP
            PackInfo.Serialize(writer);

            // Cb
            BufferInfo?.Serialize(writer);

            // CR
            SerializeCR(writer);

            // ND
            DisplayInfo?.Serialize(writer);

            // Cv
            EventReference?.Serialize(writer);

            // CN
            foreach (var channel in Channels)
            {
                channel.Serialize(writer);
            }
        }

        internal override void AfterDeserialize()
        {
            // prepare buffer info
            BufferInfo.AfterDeserialize();

            // assign buffers to pack info
            PackInfo.Buffers.AddRange(BufferInfo.Buffers.Where(buffer => buffer.Reference == PackInfo.BufferReference));
        }

        #endregion

        internal class Deserializer : FamosFileBaseExtended
        {
            #region Constructors

            internal Deserializer(BinaryReader reader, int codePage) : base(reader, codePage)
            {
                //
            }

            #endregion

            #region Properties

                private protected override FamosFileKeyType KeyType => throw new NotImplementedException();

            #endregion

            #region Methods

            internal FamosFileComponent Deserialize()
            {
                int type = 0;
                bool isDigital = false;

                DeserializeKey(expectedKeyVersion: 1, keySize =>
                {
                    // type
                    type = DeserializeInt32();

                    if (type != 1 && type != 2)
                        throw new FormatException($"Expected index value of '1' or '2', got {type}");

                    // analog / digital
                    var analogDigital = DeserializeInt32();

                    if (analogDigital != 1 && analogDigital != 2)
                        throw new FormatException($"Expected analog / digital value of '1' or '2', got {analogDigital}");

                    isDigital = analogDigital == 2;
                });

                FamosFileComponent component;

                if (isDigital)
                    component = new FamosFileDigitalComponent(Reader, CodePage);
                else
                    component = new FamosFileAnalogComponent(Reader, CodePage);

                component.Type = (FamosFileComponentType)type;

                return component;
            }

            internal override void Serialize(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }

    /// <summary>
    /// A digital component, which is a full description of a single dataset with bit-oriented data.
    /// </summary>
    public class FamosFileDigitalComponent : FamosFileComponent
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileDigitalComponent"/> class.
        /// </summary>
        /// <param name="length">The length of this component.</param>
        public FamosFileDigitalComponent(int length) : base(FamosFileDataType.Digital16Bit, length, FamosFileComponentType.Primary)
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileDigitalComponent"/> class.
        /// </summary>
        /// <param name="length">The length of this component.</param>
        /// <param name="componentType">The type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.</param>
        public FamosFileDigitalComponent(int length,
                                         FamosFileComponentType componentType) : base(FamosFileDataType.Digital16Bit, length, componentType)
        {
            //
        }

        internal FamosFileDigitalComponent(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            //
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (!Channels.Any())
                throw new FormatException("At least a single channel must be defined.");

            if (Channels.Count() > 16)
                throw new FormatException("A maximum number of 16 channels can be defined for digital components.");

            if (PackInfo.Mask != 0)
                throw new FormatException($"For digital components the mask must be set to '0'.");

            foreach (var channel in Channels)
            {
                if (!(1 <= channel.BitIndex && channel.BitIndex <= 16))
                    throw new FormatException("For digital components the channel bit indices must be within the range '1..16'.");
            }

            if (PackInfo.DataType != FamosFileDataType.Digital16Bit)
                throw new FormatException("For digital components the data type must be 'Digital16Bit'.");
        }

        #endregion

        #region Serialization

        private protected override void SerializeCR(BinaryWriter writer)
        {
            //
        }

        private protected override void DeserializeCR()
        {
            throw new FormatException($"The digital component '{Name}' defines analog calibration information.");
        }

        #endregion
    }

    /// <summary>
    /// An analog component, which is a full description of a single dataset with byte-oriented data.
    /// </summary>
    public class FamosFileAnalogComponent : FamosFileComponent
    {
        #region Unnamed Constructors

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        public FamosFileAnalogComponent(FamosFileDataType dataType,
                                        int length) : this(dataType, length, FamosFileComponentType.Primary, new FamosFileCalibration())
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="calibrationInfo">The calibration info of this component.</param>
        public FamosFileAnalogComponent(FamosFileDataType dataType,
                                        int length,
                                        FamosFileCalibration calibrationInfo) : this(dataType, length, FamosFileComponentType.Primary, calibrationInfo)
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="componentType">The type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.</param>
        public FamosFileAnalogComponent(FamosFileDataType dataType,
                                        int length,
                                        FamosFileComponentType componentType) : this(dataType, length, componentType, new FamosFileCalibration())
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="componentType">The type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.</param>
        /// <param name="calibrationInfo">The calibration info of this component.</param>
        public FamosFileAnalogComponent(FamosFileDataType dataType,
                                        int length,
                                        FamosFileComponentType componentType,
                                        FamosFileCalibration calibrationInfo) : base(dataType, length, componentType)
        {
            CalibrationInfo = calibrationInfo;
        }

        #endregion

        #region Named Constructors

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="name">The name of this component. This automatically adds a <see cref="FamosFileChannel"/> instance to the component.</param>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        public FamosFileAnalogComponent(string name,
                                        FamosFileDataType dataType,
                                        int length) : this(name, dataType, length, FamosFileComponentType.Primary, new FamosFileCalibration())
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="name">The name of this component. This automatically adds a <see cref="FamosFileChannel"/> instance to the component.</param>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="calibrationInfo">The calibration info of this component.</param>
        public FamosFileAnalogComponent(string name,
                                        FamosFileDataType dataType,
                                        int length,
                                        FamosFileCalibration calibrationInfo) : this(name, dataType, length, FamosFileComponentType.Primary, calibrationInfo)
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="name">The name of this component. This automatically adds a <see cref="FamosFileChannel"/> instance to the component.</param>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="componentType">The type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.</param>
        public FamosFileAnalogComponent(string name,
                                        FamosFileDataType dataType,
                                        int length,
                                        FamosFileComponentType componentType) : this(name, dataType, length, componentType, new FamosFileCalibration())
        {
            //
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="FamosFileAnalogComponent"/> class.
        /// </summary>
        /// <param name="name">The name of this component. This automatically adds a <see cref="FamosFileChannel"/> instance to the component.</param>
        /// <param name="dataType">The data type of this component.</param>
        /// <param name="length">The length of this component.</param>
        /// <param name="componentType">The type of this component. Depending on the higher-level field type, the meaning varies between 'Y', 'Y of XY', 'real part', 'magnitude', 'magnitude in dB' and 'timestamp ASCII' for <see cref="FamosFileComponentType.Primary"/> and between 'X of XY', 'imaginary part' and 'phase' for <see cref="FamosFileComponentType.Secondary"/>.</param>
        /// <param name="calibrationInfo">The calibration info of this component.</param>
        public FamosFileAnalogComponent(string name,
                                        FamosFileDataType dataType,
                                        int length,
                                        FamosFileComponentType componentType,
                                        FamosFileCalibration calibrationInfo) : base(dataType, length, componentType)
        {
            Channels.Add(new FamosFileChannel(name));
            CalibrationInfo = calibrationInfo;
        }

        #endregion

        #region Constructors

        internal FamosFileAnalogComponent(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            //
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the calibration info of this component.
        /// </summary>
        public FamosFileCalibration? CalibrationInfo { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            foreach (var channel in Channels)
            {
                if (channel.BitIndex != 0)
                    throw new FormatException("For analog components the channel bit indices must be set to '0'.");
            }

            if (PackInfo.DataType == FamosFileDataType.Digital16Bit)
                throw new FormatException($"For analog components the data type must be not '{nameof(FamosFileDataType.Digital16Bit)}'.");
        }

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
            base.BeforeSerialize();

            // remove all channel property infos, except of the first to keep Famos happy.
            foreach (var channel in Channels.Skip(1))
            {
                channel.PropertyInfo = null;
            }
        }

        private protected override void SerializeCR(BinaryWriter writer)
        {
            CalibrationInfo?.Serialize(writer);
        }

        private protected override void DeserializeCR()
        {
            CalibrationInfo = new FamosFileCalibration(Reader, CodePage);
        }

        #endregion
    }
}
