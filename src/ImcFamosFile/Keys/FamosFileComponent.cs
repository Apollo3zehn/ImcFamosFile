using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public abstract class FamosFileComponent : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileComponent(FamosFilePackInfo packInfo, FamosFileBufferInfo bufferInfo)
        {
            this.PackInfo = packInfo;
            this.BufferInfo = bufferInfo;
        }

        internal FamosFileComponent(BinaryReader reader,
                                    int codePage,
                                    FamosFileXAxisScaling? currentXAxisScaling,
                                    FamosFileZAxisScaling? currentZAxisScaling,
                                    FamosFileTriggerTimeInfo? currentTriggerTimeInfo) : base(reader, codePage)
        {
            FamosFilePackInfo? packInfo = null;
            FamosFileBufferInfo? bufferInfo = null;

            this.XAxisScaling = currentXAxisScaling;
            this.ZAxisScaling = currentZAxisScaling;
            this.TriggerTimeInfo = currentTriggerTimeInfo;

            FamosFilePropertyInfo? propertyInfo = null;

            while (true)
            {
                var nextKeyType = this.DeserializeKeyType();

                if (propertyInfo != null && nextKeyType != FamosFileKeyType.CN)
                    throw new FormatException("A channel info of type '|CN' was expected because a property info of type '|Np' has been defined previously.");

                // end of CC reached
                if (nextKeyType == FamosFileKeyType.CT ||
                    nextKeyType == FamosFileKeyType.CB ||
                    nextKeyType == FamosFileKeyType.CI ||
                    nextKeyType == FamosFileKeyType.CG ||
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
                    this.TriggerTimeInfo = new FamosFileTriggerTimeInfo(this.Reader);

                // CP
                else if (nextKeyType == FamosFileKeyType.CP)
                    packInfo = new FamosFilePackInfo(this.Reader);

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                    bufferInfo = new FamosFileBufferInfo(this.Reader);

                // CR
                else if (nextKeyType == FamosFileKeyType.CR)
                    this.DeserializeCR();

                // ND
                else if (nextKeyType == FamosFileKeyType.ND)
                    this.DisplayInfo = new FamosFileDisplayInfo(this.Reader);

                // Cv
                else if (nextKeyType == FamosFileKeyType.Cv)
                    this.EventLocationInfo = new FamosFileEventLocationInfo(this.Reader);

                // CN
                else if (nextKeyType == FamosFileKeyType.CN)
                {
                    this.Channels.Add(new FamosFileChannelInfo(this.Reader, this.CodePage));
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

        public int Index { get; set; }

        public FamosFileXAxisScaling? XAxisScaling { get; set; }
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }
        public FamosFileTriggerTimeInfo? TriggerTimeInfo { get; set; }

        public FamosFilePackInfo PackInfo { get; set; }
        public FamosFileBufferInfo BufferInfo { get; set; }

        public FamosFileDisplayInfo? DisplayInfo { get; set; }
        public FamosFileEventLocationInfo? EventLocationInfo { get; set; }

        public List<FamosFileChannelInfo> Channels { get; } = new List<FamosFileChannelInfo>();

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CC;

        #endregion

        #region Relay Properties

        public string Name
        {
            get
            {
                var name = string.Empty;

                foreach (var channelInfo in this.Channels)
                {
                    if (!string.IsNullOrWhiteSpace(channelInfo.Name))
                    {
                        name = channelInfo.Name;
                        break;
                    }
                }

                return name;
            }
        }

        #endregion

        #region Methods

        internal override void Validate()
        {
            // pack info's buffers
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
        }

        protected abstract void SerializeCR(StreamWriter writer);
        protected abstract void DeserializeCR();

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
#warning TODO: It is unclear if there may be buffers defined which do NOT contain data of this component. Are these dangling buffers?

            // reset all buffer references to a value != 1
            foreach (var buffer in this.BufferInfo.Buffers)
            {
                buffer.Reference = 2;
            }

            // update buffer reference of pack info and its corresponding buffers
            this.PackInfo.BufferReference = 1;

            foreach (var buffer in this.PackInfo.Buffers)
            {
                buffer.Reference = this.PackInfo.BufferReference;
            }
        }

        internal override void Serialize(StreamWriter writer)
        {
            // CC
            var data = new object[]
            {
                    this.Index,
                    this.GetType() == typeof(FamosFileAnalogComponent) ? 1 : 2
            };

            this.SerializeKey(writer, 1, data);

            // CD
            this.XAxisScaling?.Serialize(writer);

            // CZ
            this.ZAxisScaling?.Serialize(writer);

            // NT
            this.TriggerTimeInfo?.Serialize(writer);

            // CP
            this.PackInfo.Serialize(writer);

            // Cb
            this.BufferInfo?.Serialize(writer);

            // CR
            this.SerializeCR(writer);

            // ND
            this.DisplayInfo?.Serialize(writer);

            // Cv
            this.EventLocationInfo?.Serialize(writer);

            // CN
            foreach (var channelInfo in this.Channels)
            {
                channelInfo.Serialize(writer);
            }
        }

        internal override void AfterDeserialize()
        {
            // prepare buffer info
            this.BufferInfo.AfterDeserialize();
        }

        #endregion

        internal class Deserializer : FamosFileBaseExtended
        {
            #region Constructors

            public Deserializer(BinaryReader reader, int codePage) : base(reader, codePage)
            {
                //
            }

            #endregion

            #region Properties

            protected override FamosFileKeyType KeyType => throw new NotImplementedException();

            #endregion

            #region Methods

            internal FamosFileComponent Deserialize(FamosFileXAxisScaling? currentXAxisScaling,
                                                    FamosFileZAxisScaling? currentZAxisScaling,
                                                    FamosFileTriggerTimeInfo? currentTriggerTimeInfo)
            {
                int index = 0;
                bool isDigital = false;

                this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                {
                    // index
                    index = this.DeserializeInt32();

                    if (index != 1 && index != 2)
                        throw new FormatException($"Expected index value of '1' or '2', got {index}");

                    // analog / digital
                    var analogDigital = this.DeserializeInt32();

                    if (analogDigital != 1 && analogDigital != 2)
                        throw new FormatException($"Expected analog / digital value of '1' or '2', got {analogDigital}");

                    isDigital = analogDigital == 2;
                });

                FamosFileComponent component;

                if (isDigital)
                    component = new FamosFileDigitalComponent(this.Reader, this.CodePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo);
                else
                    component = new FamosFileAnalogComponent(this.Reader, this.CodePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo);

                component.Index = index;

                return component;
            }

            internal override void Serialize(StreamWriter writer)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }

    public class FamosFileDigitalComponent : FamosFileComponent
    {
        #region Constructors

        public FamosFileDigitalComponent(FamosFilePackInfo packInfo, FamosFileBufferInfo bufferInfo) : base(packInfo, bufferInfo)
        {
            //
        }

        public FamosFileDigitalComponent(BinaryReader reader,
                                         int codePage,
                                         FamosFileXAxisScaling? currentXAxisScaling,
                                         FamosFileZAxisScaling? currentZAxisScaling,
                                         FamosFileTriggerTimeInfo? currentTriggerTimeInfo)
            : base(reader, codePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo)
        {
            //
        }

        #endregion

        #region Methods

        internal override void Validate()
        {
            base.Validate();

            if (!this.Channels.Any())
                throw new FormatException("At least a single channel must be defined.");

            if (this.Channels.Count() > 16)
                throw new FormatException("A maximum number of 16 channels can be defined for digital components.");

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

        protected override void SerializeCR(StreamWriter writer)
        {
            //
        }

        protected override void DeserializeCR()
        {
            throw new FormatException($"The digital component '{this.Name}' defines analog calibration information.");
        }

        #endregion
    }

    public class FamosFileAnalogComponent : FamosFileComponent
    {
        #region Constructors

        public FamosFileAnalogComponent(FamosFileCalibrationInfo calibrationInfo, FamosFilePackInfo packInfo, FamosFileBufferInfo bufferInfo) : base(packInfo, bufferInfo)
        {
            this.CalibrationInfo = calibrationInfo;
        }

#pragma warning disable CS8618
        public FamosFileAnalogComponent(BinaryReader reader,
                                        int codePage,
                                        FamosFileXAxisScaling? currentXAxisScaling,
                                        FamosFileZAxisScaling? currentZAxisScaling,
                                        FamosFileTriggerTimeInfo? currentTriggerTimeInfo)
            : base(reader, codePage, currentXAxisScaling, currentZAxisScaling, currentTriggerTimeInfo)
        {
            if (this.CalibrationInfo is null)
                throw new FormatException($"The analog component '{this.Name}' does not define calibration information.");
        }

        #endregion

        #region Properties

        public FamosFileCalibrationInfo CalibrationInfo { get; set; }

        #endregion

        #region Methods

        internal override void Validate()
        {
            base.Validate();

            foreach (var channel in this.Channels)
            {
                if (channel.BitIndex != 0)
                    throw new FormatException("For analog components the channel bit indices must be set to '0'.");
            }

            if (this.PackInfo.DataType == FamosFileDataType.Digital16Bit)
                throw new FormatException($"For analog components the data type must be not '{nameof(FamosFileDataType.Digital16Bit)}'.");

            if ((this.PackInfo.DataType == FamosFileDataType.Float32 
              || this.PackInfo.DataType == FamosFileDataType.Float64) 
              && this.CalibrationInfo.ApplyTransformation)
                throw new FormatException($"Components with raw data of type '{nameof(FamosFileDataType.Float32)}' or '{nameof(FamosFileDataType.Float64)}' are not allowed to set the calibration info's property '{nameof(this.CalibrationInfo.ApplyTransformation)}' to 'true'.");
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

        protected override void SerializeCR(StreamWriter writer)
        {
            this.CalibrationInfo?.Serialize(writer);
        }

        protected override void DeserializeCR()
        {
            this.CalibrationInfo = new FamosFileCalibrationInfo(this.Reader, this.CodePage);
        }

        #endregion
    }
}
