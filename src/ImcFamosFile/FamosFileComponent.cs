using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileComponent : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileComponent(FamosFilePackInfo packInfo)
        {
            this.PackInfo = packInfo;
        }

        public FamosFileComponent(BinaryReader reader,
                                  int codePage,
                                  FamosFileXAxisScaling? currentXAxisScaling,
                                  FamosFileZAxisScaling? currentZAxisScaling,
                                  FamosFileTriggerTimeInfo? currentTriggerTimeInfo) : base(reader, codePage)
        {
            this.XAxisScaling = currentXAxisScaling;
            this.ZAxisScaling = currentZAxisScaling;
            this.TriggerTimeInfo = currentTriggerTimeInfo;

            FamosFilePackInfo? packInfo = null;

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                // index
                var index = this.DeserializeInt32();

                if (index != 1 && index != 2)
                    throw new FormatException($"Expected index value '1' or '2', got {index}");

                this.Index = index;

                // analog / digital
                var analogDigital = this.DeserializeInt32();

                if (analogDigital != 1 && analogDigital != 2)
                    throw new FormatException($"Expected analog / digital value '1' or '2', got {analogDigital}");

                this.IsDigital = analogDigital == 2;
            });

            while (true)
            {
                var nextKeyType = this.DeserializeKeyType();

                // end of CC reached
                if (nextKeyType == FamosFileKeyType.CT ||
                    nextKeyType == FamosFileKeyType.CB ||
                    nextKeyType == FamosFileKeyType.CI ||
                    nextKeyType == FamosFileKeyType.CG)
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

                else if (nextKeyType == FamosFileKeyType.CD)
                    this.XAxisScaling = new FamosFileXAxisScaling(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.CZ)
                    this.ZAxisScaling = new FamosFileZAxisScaling(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.NT)
                    this.TriggerTimeInfo = new FamosFileTriggerTimeInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.CP)
                    packInfo = new FamosFilePackInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.Cb)
                {
                    this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                    {
                        var bufferCount = this.DeserializeInt32();
                        var userInfoSize = this.DeserializeInt32();

                        for (int i = 0; i < bufferCount; i++)
                        {
                            var buffer = new FamosFileBuffer(this.Reader);
                            this.Buffers.Add(buffer);
                        }

                        this.UserInfo = this.DeserializeKeyPart();

                        if (this.UserInfo.Length != userInfoSize)
                            throw new FormatException("The given size of the user info does not match the actual size.");
                    });
                }

                else if (nextKeyType == FamosFileKeyType.CR)
                    this.CalibrationInfo = new FamosFileCalibrationInfo(this.Reader, this.CodePage);

                else if (nextKeyType == FamosFileKeyType.ND)
                    this.DisplayInfo = new FamosFileDisplayInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.Cv)
                    this.EventInfo = new FamosFileEventInfo(this.Reader);

                else if (nextKeyType == FamosFileKeyType.CN)
                    this.ChannelInfos.Add(new FamosFileChannelInfo(this.Reader, this.CodePage));

                else
                    // should never happen
                    throw new FormatException("An unexpected state has been reached.");
            }

            if (packInfo is null)
                throw new FormatException("No pack information was found in the component.");
            else
                this.PackInfo = packInfo;
        }

        #endregion

        #region Properties

        public int Index { get; set; }
        public bool IsDigital { get; set; }

        public FamosFileXAxisScaling? XAxisScaling { get; set; }
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }
        public FamosFileTriggerTimeInfo? TriggerTimeInfo { get; set; }

        public FamosFilePackInfo PackInfo { get; set; }
        public FamosFileCalibrationInfo? CalibrationInfo { get; set; }
        public FamosFileDisplayInfo? DisplayInfo { get; set; }
        public FamosFileEventInfo? EventInfo { get; set; }

        public byte[]? UserInfo { get; set; }
        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();
        public List<FamosFileChannelInfo> ChannelInfos { get; } = new List<FamosFileChannelInfo>();

        #endregion

        #region Relay Properties

        public string Name
        {
            get
            {
                var name = string.Empty;

                foreach (var channelInfo in this.ChannelInfos)
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
            // analog vs. digital
            if (!this.IsDigital && this.CalibrationInfo is null)
                throw new FormatException($"The analog component '{this.Name}' does not define calibration information.");

            if (this.IsDigital && this.CalibrationInfo != null)
                throw new FormatException($"The digital component '{this.Name}' defines analog calibration information.");

            // pack info
            if (this.PackInfo is null)
                throw new FormatException("The component's pack info must be provided.");

            if (!this.PackInfo.Buffers.Any())
                throw new FormatException("The pack info's buffers collection must container at least a single buffer instance.");

            foreach (var buffer in this.PackInfo.Buffers)
            {
                if (!this.Buffers.Contains(buffer))
                    throw new FormatException("The pack info's buffers must be part of the component's buffer collection.");
            }

            // validate buffers
            foreach (var buffer in this.Buffers)
            {
                buffer.Validate();
            }

            // validate display info
            this.DisplayInfo?.Validate();

            // validate pack info
            this.PackInfo?.Validate();
        }

        #endregion

        #region Serialization

        internal override void BeforeSerialize()
        {
#warning TODO: It is unclear if there may be buffers defined which do NOT contain data of this component. Are these dangling buffers?

            // reset all buffer references to a value != 1
            foreach (var buffer in this.Buffers)
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
            var data = string.Join(',', new object[]
            {
                this.Index,
                this.IsDigital ? 2 : 1
            });

            this.SerializeKey(writer, FamosFileKeyType.CC, 1, data);

#warning TODO: do not write these always
            this.XAxisScaling?.Serialize(writer);
            this.ZAxisScaling?.Serialize(writer);
            this.TriggerTimeInfo?.Serialize(writer);

            this.PackInfo.Serialize(writer);
            this.CalibrationInfo?.Serialize(writer);
            this.DisplayInfo?.Serialize(writer);
            this.EventInfo?.Serialize(writer);

            if (this.UserInfo != null)
            {
                foreach (var buffer in this.Buffers)
                {
                    var dataPre = string.Join(',', new object[]
                    {
                        this.Buffers.Count,
                        this.UserInfo.Length,
                    });

                    var dataPost = string.Join(',', new object[]
                    {
                        this.UserInfo
#warning TODO: byte[] is not written correctly
                    });

                    this.SerializeKey(writer, FamosFileKeyType.Cb, 1, dataPre, dataPost, () => buffer.Serialize(writer));
                }
            }

            foreach (var channelInfo in this.ChannelInfos)
            {
                channelInfo.Serialize(writer);
            }
        }

        #endregion

        #region Deserialization

        internal override void AfterDeserialize()
        {
            // sort buffers
            this.Buffers = this.Buffers.OrderBy(buffer => buffer.Reference).ToList();

            // associate buffers to pack info
            this.PackInfo?.Buffers.AddRange(this.Buffers.Where(buffer => buffer.Reference == this.PackInfo.BufferReference));
        }

        #endregion
    }
}
