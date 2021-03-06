﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            this.Type = componentType;
            this.BufferInfo = new FamosFileBufferInfo(new List<FamosFileBuffer>() { new FamosFileBuffer() });
            this.PackInfo = new FamosFilePackInfo(dataType, this.BufferInfo.Buffers.ToList());

            var buffer = this.BufferInfo.Buffers.First();
            buffer.Length = length * this.PackInfo.ValueSize;
            buffer.ConsumedBytes = buffer.Length; // This could theoretically be set to actual value during 'famosFile.Save(...);' but how handle interlaced data?
        }

        private protected FamosFileComponent(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            FamosFilePackInfo? packInfo = null;
            FamosFileBufferInfo? bufferInfo = null;
            FamosFilePropertyInfo? propertyInfo = null;

            while (true)
            {
                var nextKeyType = this.DeserializeKeyType();

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
                    this.Reader.BaseStream.Position -= 4;
                    break;
                }

                else if (nextKeyType == FamosFileKeyType.Unknown)
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

                // CP
                else if (nextKeyType == FamosFileKeyType.CP)
                    packInfo = new FamosFilePackInfo(this.Reader);

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                {
                    if (bufferInfo == null)
                        bufferInfo = new FamosFileBufferInfo(this.Reader);
                    else
                        throw new FormatException("Although the format specification allows multiple '|Cb' keys, this implementation supports only a single definition per component. Please send a sample file to the project maintainer to overcome this limitation in future.");
                }

                // CR
                else if (nextKeyType == FamosFileKeyType.CR)
                    this.DeserializeCR();

                // ND
                else if (nextKeyType == FamosFileKeyType.ND)
                    this.DisplayInfo = new FamosFileDisplayInfo(this.Reader);

                // Cv
                else if (nextKeyType == FamosFileKeyType.Cv)
                    throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");
                    //this.EventReference = new FamosFileEventReference(this.Reader);

                // CN
                else if (nextKeyType == FamosFileKeyType.CN)
                {
                    this.Channels.Add(new FamosFileChannel(this.Reader, this.CodePage));
                    this.Channels.Last().PropertyInfo = propertyInfo;

                    propertyInfo = null;
                }

                // Np
                else if (nextKeyType == FamosFileKeyType.Np)
                    propertyInfo = new FamosFilePropertyInfo(this.Reader, this.CodePage);

                else
                    // should never happen
                    throw new FormatException("An unexpected state has been reached.");
            }

            if (packInfo is null)
                throw new FormatException("No pack information was found in the component.");
            else
                this.PackInfo = packInfo;

            if (bufferInfo is null)
                throw new FormatException("No buffer information was found in the component.");
            else
                this.BufferInfo = bufferInfo;
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

                foreach (var channel in this.Channels)
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
            return this.GetSize(0, 0);
        }

        /// <summary>
        /// Calulates the size of this component depending on the values of <see cref="BufferInfo"/> and <see cref="PackInfo"/>.
        /// </summary>
        /// <param name="start">The number of values to skip.</param>
        /// <returns>Returns the size of the component.</returns>
        public int GetSize(int start)
        {
            return this.GetSize(start, 0);
        }

        /// <summary>
        /// Calulates the size of this component depending on the values of <see cref="BufferInfo"/> and <see cref="PackInfo"/>.
        /// </summary>
        /// <param name="start">The number of values to skip.</param>
        /// <param name="length">The number of values to take.</param>
        /// <returns>Returns the size of the component.</returns>
        public int GetSize(int start, int length)
        {
            var packInfo = this.PackInfo;
            var buffer = this.PackInfo.Buffers.First();

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
            if (!this.PackInfo.Buffers.Any())
                throw new FormatException("The pack info's buffers collection must container at least a single buffer instance.");

            foreach (var buffer in this.PackInfo.Buffers)
            {
                if (!this.BufferInfo.Buffers.Contains(buffer))
                    throw new FormatException("The pack info's buffers must be part of the component's buffer collection.");
            }

            // validate pack info
            this.PackInfo.Validate();

            // validate buffer info
            this.BufferInfo.Validate();

            /* check unique region */
            if (this.Channels.Count != this.Channels.Distinct().Count())
                throw new FormatException("A channel must be added only once.");

            /* not yet supported region */
            foreach (var channel in this.Channels)
            {
                if (channel.BitIndex > 0)
                    throw new InvalidOperationException("This implementation does not support processing boolean data yet. Please send a sample file to the package author to find a solution.");
            }

            if (this.EventReference != null)
                throw new NotSupportedException("Events are not supported yet. Please send a sample file to the package author to find a solution.");

            if (this.PackInfo.GroupSize > 1)
                throw new InvalidOperationException("This implementation does not yet support processing data with a pack info group size > '1'. Please send a sample file to the package author to find a solution.");

            if (this.PackInfo.Mask != 0)
                throw new InvalidOperationException("This implementation does not yet support processing masked data. Please send a sample file to the package author to find a solution.");

            if (this.PackInfo.Buffers.First().IsRingBuffer)
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
                    (int)this.Type,
                    this.GetType() == typeof(FamosFileAnalogComponent) ? 1 : 2
            };

            this.SerializeKey(writer, 1, data);

            // CD
            this.XAxisScaling?.Serialize(writer);

            // CZ
            this.ZAxisScaling?.Serialize(writer);

            // NT
            this.TriggerTime?.Serialize(writer);

            // CP
            this.PackInfo.Serialize(writer);

            // Cb
            this.BufferInfo?.Serialize(writer);

            // CR
            this.SerializeCR(writer);

            // ND
            this.DisplayInfo?.Serialize(writer);

            // Cv
            this.EventReference?.Serialize(writer);

            // CN
            foreach (var channel in this.Channels)
            {
                channel.Serialize(writer);
            }
        }

        internal override void AfterDeserialize()
        {
            // prepare buffer info
            this.BufferInfo.AfterDeserialize();

            // assign buffers to pack info
            this.PackInfo.Buffers.AddRange(this.BufferInfo.Buffers.Where(buffer => buffer.Reference == this.PackInfo.BufferReference));
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

                this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                {
                    // type
                    type = this.DeserializeInt32();

                    if (type != 1 && type != 2)
                        throw new FormatException($"Expected index value of '1' or '2', got {type}");

                    // analog / digital
                    var analogDigital = this.DeserializeInt32();

                    if (analogDigital != 1 && analogDigital != 2)
                        throw new FormatException($"Expected analog / digital value of '1' or '2', got {analogDigital}");

                    isDigital = analogDigital == 2;
                });

                FamosFileComponent component;

                if (isDigital)
                    component = new FamosFileDigitalComponent(this.Reader, this.CodePage);
                else
                    component = new FamosFileAnalogComponent(this.Reader, this.CodePage);

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

            if (!this.Channels.Any())
                throw new FormatException("At least a single channel must be defined.");

            if (this.Channels.Count() > 16)
                throw new FormatException("A maximum number of 16 channels can be defined for digital components.");

            if (this.PackInfo.Mask != 0)
                throw new FormatException($"For digital components the mask must be set to '0'.");

            foreach (var channel in this.Channels)
            {
                if (!(1 <= channel.BitIndex && channel.BitIndex <= 16))
                    throw new FormatException("For digital components the channel bit indices must be within the range '1..16'.");
            }

            if (this.PackInfo.DataType != FamosFileDataType.Digital16Bit)
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
            throw new FormatException($"The digital component '{this.Name}' defines analog calibration information.");
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
            this.CalibrationInfo = calibrationInfo;
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
            this.Channels.Add(new FamosFileChannel(name));
            this.CalibrationInfo = calibrationInfo;
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

            foreach (var channel in this.Channels)
            {
                if (channel.BitIndex != 0)
                    throw new FormatException("For analog components the channel bit indices must be set to '0'.");
            }

            if (this.PackInfo.DataType == FamosFileDataType.Digital16Bit)
                throw new FormatException($"For analog components the data type must be not '{nameof(FamosFileDataType.Digital16Bit)}'.");
        }

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
            base.BeforeSerialize();

            // remove all channel property infos, except of the first to keep Famos happy.
            foreach (var channel in this.Channels.Skip(1))
            {
                channel.PropertyInfo = null;
            }
        }

        private protected override void SerializeCR(BinaryWriter writer)
        {
            this.CalibrationInfo?.Serialize(writer);
        }

        private protected override void DeserializeCR()
        {
            this.CalibrationInfo = new FamosFileCalibration(this.Reader, this.CodePage);
        }

        #endregion
    }
}
