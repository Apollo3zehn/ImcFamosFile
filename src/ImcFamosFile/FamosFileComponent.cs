using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFileComponent : FamosFileBase
    {
        #region Constructors

        public FamosFileComponent()
        {
            //
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

            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                // index
                var index = this.DeserializeInt32();

                if (index != 1 && index != 2)
                    throw new FormatException($"Expected index value '1' or '2', got {index}");

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
                    this.XAxisScaling = base.DeserializeCD();

                else if (nextKeyType == FamosFileKeyType.CZ)
                    this.ZAxisScaling = base.DeserializeCZ();

                else if (nextKeyType == FamosFileKeyType.NT)
                    this.TriggerTimeInfo = base.DeserializeNT();

                else if (nextKeyType == FamosFileKeyType.CP)
                    this.DeserializeCP();

                else if (nextKeyType == FamosFileKeyType.Cb)
                    this.DeserializeCb();

                else if (nextKeyType == FamosFileKeyType.CR)
                    this.DeserializeCR();

                else if (nextKeyType == FamosFileKeyType.ND)
                    this.DeserializeND();

                else if (nextKeyType == FamosFileKeyType.Cv)
                    this.DeserializeCv();

                else if (nextKeyType == FamosFileKeyType.CN)
                    this.DeserializeCN();

                else
                    // should never happen
                    throw new FormatException("An unexpected state has been reached.");
            }
        }

        #endregion

        #region Properties

        public int Index { get; private set; }
        public bool IsDigital { get; set; }

        public FamosFileXAxisScaling? XAxisScaling { get; set; }
        public FamosFileZAxisScaling? ZAxisScaling { get; set; }
        public FamosFileTriggerTimeInfo? TriggerTimeInfo { get; set; }

        public FamosFilePackInfo? PackInfo { get; set; }
        public FamosFileCalibrationInfo? CalibrationInfo { get; set; }
        public FamosFileDisplayInfo? DisplayInfo { get; set; }
        public FamosFileEventInfo? EventInfo { get; set; }

        public List<FamosFileBuffer> Buffers { get; private set; } = new List<FamosFileBuffer>();
        public List<FamosFileChannelInfo> ChannelInfos { get; private set; } = new List<FamosFileChannelInfo>();

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

        internal override void Prepare()
        {
            // sort buffers
            this.Buffers = this.Buffers.OrderBy(buffer => buffer.Reference).ToList();

            // associate buffers to pack info
            this.PackInfo?.Buffers.AddRange(this.Buffers.Where(buffer => buffer.Reference == this.PackInfo.BufferReference));
        }

        internal override void Validate()
        {
            // analog vs. digital
            if (!this.IsDigital && this.CalibrationInfo == null)
                throw new FormatException($"The analog component '{this.Name}' does not define calibration information.");

            if (this.IsDigital && this.CalibrationInfo != null)
                throw new FormatException($"The digital component '{this.Name}' defines analog calibration information.");

            // pack info
            if (this.PackInfo == null)
                throw new FormatException("The component's pack info must be provided.");

            if (!this.PackInfo.Buffers.Any())
                throw new FormatException("The pack info's buffers collection must container at least a single buffer instance.");

            foreach (var buffer in this.PackInfo.Buffers)
            {
                if (!this.Buffers.Contains(buffer))
                    throw new FormatException("The pack info's buffers must be part of the component's buffer collection.");
            }

            // buffers
            foreach (var buffer in this.Buffers)
            {
                if (buffer.RawData == null)
                    throw new FormatException("The buffer's raw data must be provided.");
            }
        }

        #endregion

        #region KeyParsing

        // Pack information for this component.
        private void DeserializeCP()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.PackInfo = new FamosFilePackInfo(this.DeserializeInt32())
                {
                    ValueSize = this.DeserializeInt32(),
                    DataType = (FamosFileDataType)this.DeserializeInt32(),
                    SignificantBits = this.DeserializeInt32(),
                    Offset = Math.Max(this.DeserializeInt32(), this.DeserializeInt32()),
                    GroupSize = this.DeserializeInt32(),
                    GapSize = this.DeserializeInt32(),
                };
            });
        }

        // Buffer description.
        private void DeserializeCb()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var bufferCount = this.DeserializeInt32();
                var userInfoSize = this.DeserializeInt32();

                for (int i = 0; i < bufferCount; i++)
                {
                    this.Buffers.Add(new FamosFileBuffer(this.DeserializeInt32(), this.DeserializeInt32())
                    {
                        RawDataOffset = this.DeserializeInt32(),
                        Length = this.DeserializeInt32(),
                        Offset = this.DeserializeInt32(),
                        ConsumedBytes = this.DeserializeInt32(),
                        IsNewEvent = this.DeserializeInt32() == 1,
                        x0 = this.DeserializeInt32(),
                        TriggerAddTime = this.DeserializeInt32(),
                        UserInfo = this.DeserializeKeyPart()
                    });
                }
            });
        }

        // Calibration information.
        private void DeserializeCR()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.CalibrationInfo = new FamosFileCalibrationInfo()
                {
                    ApplyTransformation = this.DeserializeInt32() == 1,
                    Factor = this.DeserializeFloat64(),
                    Offset = this.DeserializeFloat64(),
                    IsCalibrated = this.DeserializeInt32() == 1,
                    Unit = this.DeserializeString()
                };
            });
        }

        // Display information.
        private void DeserializeND()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.DisplayInfo = new FamosFileDisplayInfo()
                {
                    R = this.DeserializeInt32(),
                    G = this.DeserializeInt32(),
                    B = this.DeserializeInt32(),
                    YMin = this.DeserializeFloat64(),
                    YMax = this.DeserializeFloat64()
                };
            });
        }

        // Event information.
        private void DeserializeCv()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.EventInfo = new FamosFileEventInfo()
                {
                    IndexEventListKey = this.DeserializeInt32(),
                    OffsetInEventList = this.DeserializeInt32(),
                    SubsequentEventCount = this.DeserializeInt32(),
                    EventDistance = this.DeserializeInt32(),
                    EventCount = this.DeserializeInt32(),
                    ValidNT = (FamosFileValidNTType)this.DeserializeInt32(),
                    ValidCD = (FamosFileValidCDType)this.DeserializeInt32(),
                    ValidCR1 = (FamosFileValidCR1Type)this.DeserializeInt32(),
                    ValidCR2 = (FamosFileValidCR2Type)this.DeserializeInt32(),
                };
            });
        }

        // Channel information.
        private void DeserializeCN()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var groupIndex = this.DeserializeInt32();
                this.DeserializeInt32();
                var bitIndex = this.DeserializeInt32();
                var name = this.DeserializeString();
                var comment = this.DeserializeString();

                this.ChannelInfos.Add(new FamosFileChannelInfo()
                {
                    GroupIndex = groupIndex,
                    BitIndex = bitIndex,
                    Name = name,
                    Comment = comment
                });
            });
        }

        #endregion
    }
}
