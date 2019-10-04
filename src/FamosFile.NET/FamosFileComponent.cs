using System;
using System.Collections.Generic;
using System.IO;

namespace FamosFile.NET
{
    public class FamosFileComponent : FamosFileBase
    {
        #region Constructors

        public FamosFileComponent(BinaryReader reader, int codePage) : base(reader)
        {
            this.CodePage = codePage;

            this.Buffers = new List<FamosFileBuffer>();
            this.ChannelInfos = new List<FamosFileChannelInfo>();
        }

        #endregion

        #region Properties

        public int ComponentIndex { get; private set; }
        public FamosFileAnalogDigital AnalogDigital { get; private set; }

        public DateTime TriggerTime { get; private set; }
        public FamosFileTimeMode TimeMode { get; private set; }

        public FamosFileXAxisScaling XAxisScaling { get; private set; }
        public FamosFileZAxisScaling ZAxisScaling { get; private set; }

        public FamosFilePackInfo PackInfo { get; private set; }
        public FamosFileBufferInfo BufferInfo { get; private set; }
        public FamosFileCalibrationInfo CalibrationInfo { get; private set; }
        public FamosFileDisplayInfo DisplayInfo { get; private set; }
        public FamosFileEventInfo EventInfo { get; private set; }

        public List<FamosFileBuffer> Buffers { get; private set; }
        public List<FamosFileChannelInfo> ChannelInfos { get; private set; }

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

        #region KeyParsing

        public FamosFileKeyType Parse(FamosFileXAxisScaling currentXAxisScaling, FamosFileZAxisScaling currentZAxisScaling, DateTime currentTriggerTime, FamosFileTimeMode currentTimeMode)
        {
            var nextKeyType = FamosFileKeyType.Unknown;

            this.XAxisScaling = currentXAxisScaling;
            this.ZAxisScaling = currentZAxisScaling;
            this.TriggerTime = currentTriggerTime;

            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                this.ComponentIndex = this.ParseInt32();
                this.AnalogDigital = (FamosFileAnalogDigital)this.ParseInt32();
            });

            while (true)
            {
                nextKeyType = this.ParseKeyType();

                // end of CC reached
                if (nextKeyType == FamosFileKeyType.CT ||
                    nextKeyType == FamosFileKeyType.CB ||
                    nextKeyType == FamosFileKeyType.CI ||
                    nextKeyType == FamosFileKeyType.CG)
                {
                    break;
                }

                else if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                else if (nextKeyType == FamosFileKeyType.CD)
                    this.XAxisScaling = base.ParseCD();

                else if (nextKeyType == FamosFileKeyType.CZ)
                    this.ZAxisScaling = base.ParseCZ();

                else if (nextKeyType == FamosFileKeyType.NT)
                    (this.TriggerTime, this.TimeMode) = base.ParseNT();

                else if (nextKeyType == FamosFileKeyType.CP)
                    this.ParseCP();

                else if (nextKeyType == FamosFileKeyType.Cb)
                    this.ParseCb();

                else if (nextKeyType == FamosFileKeyType.CR)
                    this.ParseCR();

                else if (nextKeyType == FamosFileKeyType.ND)
                    this.ParseND();

                else if (nextKeyType == FamosFileKeyType.Cv)
                    this.ParseCv();

                else if (nextKeyType == FamosFileKeyType.CN)
                    this.ParseCN();

                else
                    break;
            }

            return nextKeyType;
        }

        // Pack information for this component.
        private void ParseCP()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                this.PackInfo = new FamosFilePackInfo()
                {
                    BufferReference = this.ParseInt32(),
                    ValueSize = this.ParseInt32(),
                    DataType = (FamosFileDataType)this.ParseInt32(),
                    SignificantBits = this.ParseInt32(),
                    Offset = Math.Max(this.ParseInt32(), this.ParseInt32()),
                    GroupSize = this.ParseInt32(),
                    GapSize = this.ParseInt32(),
                };
            });
        }

        // Buffer description.
        private void ParseCb()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                var bufferCount = this.ParseInt32();
                var userInfoSize = this.ParseInt32();

                for (int i = 0; i < bufferCount; i++)
                {
                    this.Buffers.Add(new FamosFileBuffer()
                    {
                        Reference = this.ParseInt32(),
                        CsKeyReference = this.ParseInt32(),
                        CsKeyOffset = this.ParseInt32(),
                        Length = this.ParseInt32(),
                        Offset = this.ParseInt32(),
                        ConsumedBytes = this.ParseInt32(),
                        IsNewEvent = this.ParseInt32() == 1,
                        x0 = this.ParseInt32(),
                        TriggerAddTime = this.ParseInt32(),
                        UserInfo = this.ParseKeyPart()
                    });
                }
            });
        }

        // Calibration information.
        private void ParseCR()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                this.CalibrationInfo = new FamosFileCalibrationInfo()
                {
                    ApplyTransformation = this.ParseInt32() == 1,
                    Factor = this.ParseFloat64(),
                    Offset = this.ParseFloat64(),
                    IsCalibrated = this.ParseInt32() == 1,
                    Unit = this.ParseString()
                };
            });
        }

        // Display information.
        private void ParseND()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                this.DisplayInfo = new FamosFileDisplayInfo()
                {
                    R = this.ParseInt32(),
                    G = this.ParseInt32(),
                    B = this.ParseInt32(),
                    YMin = this.ParseFloat64(),
                    YMax = this.ParseFloat64()
                };
            });
        }

        // Event information.
        private void ParseCv()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                this.EventInfo = new FamosFileEventInfo()
                {
                    IndexEventListKey = this.ParseInt32(),
                    OffsetInEventList = this.ParseInt32(),
                    SubsequentEventCount = this.ParseInt32(),
                    EventDistance = this.ParseInt32(),
                    EventCount = this.ParseInt32(),
                    ValidNT = (FamosFileValidNTType)this.ParseInt32(),
                    ValidCD = (FamosFileValidCDType)this.ParseInt32(),
                    ValidCR1 = (FamosFileValidCR1Type)this.ParseInt32(),
                    ValidCR2 = (FamosFileValidCR2Type)this.ParseInt32(),
                };
            });
        }

        // Channel information.
        private void ParseCN()
        {
            this.ParseKey(expectedKeyVersion: 1, keySize =>
            {
                var groupIndex = this.ParseInt32();
                this.ParseInt32();
                var bitIndex = this.ParseInt32();
                var name = this.ParseString();
                var comment = this.ParseString();

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
