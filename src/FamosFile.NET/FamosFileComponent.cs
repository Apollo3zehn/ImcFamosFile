using System;
using System.IO;

namespace FamosFile.NET
{
    public class FamosFileComponent : FamosFileBase
    {
        #region Constructors

        public FamosFileComponent(BinaryReader reader, int codePage) : base(reader)
        {
            this.CodePage = codePage;
        }

        #endregion

        #region Properties

        public int GroupIndex { get; private set; }
        public int BitIndex { get; private set; }
        public string Name { get; private set; }
        public string Comment { get; private set; }

        public DateTime TriggerTime { get; private set; }
        public FamosFileTimeMode TimeMode { get; private set; }

        public FamosFileXAxisScaling XAxisScaling { get; private set; }
        public FamosFileZAxisScaling ZAxisScaling { get; private set; }
        public FamosFilePackInformation PackInformation { get; private set; }
        public FamosFileBufferInformation BufferInformation { get; private set; }
        public FamosFileCalibrationInformation CalibrationInformation { get; private set; }
        public FamosFileDisplayInformation DisplayInformation { get; private set; }
        public FamosFileEventInformation EventInformation { get; private set; }

        #endregion

        #region KeyParsing

        public FamosFileKeyType Parse(FamosFileXAxisScaling currentXAxisScaling, FamosFileZAxisScaling currentZAxisScaling, DateTime currentTriggerTime, FamosFileTimeMode currentTimeMode)
        {
            var nextKeyType = FamosFileKeyType.Unknown;

            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var componentIndex = this.ParseInteger();
                var analogDigital = (FamosFileAnalogDigital)this.ParseInteger();

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
            });

            return nextKeyType;
        }

        // Pack information for this component.
        private void ParseCP()
        {
            this.ParseKey(FamosFileKeyType.CP, expectedKeyVersion: 1, () =>
            {
                this.PackInformation = new FamosFilePackInformation()
                {
                    BufferReference = this.ParseInteger(),
                    ByteCountPerValue = this.ParseInteger(),
                    DataType = (FamosFileDataType)this.ParseInteger(),
                    SignificantBitsCount = this.ParseInteger(),
                    FirstValueOffset = this.ParseInteger(),
                    SubsequentValueCount = this.ParseInteger(),
                    ValueDistanceByteCount = this.ParseInteger(),
                };
            });
        }

        // Buffer description.
        private void ParseCb()
        {
            this.ParseKey(FamosFileKeyType.Cb, expectedKeyVersion: 1, () =>
            {
                this.BufferInformation = new FamosFileBufferInformation();

                var bufferCount = this.ParseInteger();

                for (int i = 0; i < bufferCount; i++)
                {
                    this.BufferInformation.Buffers.Add(new FamosFileBuffer()
                    {
                        BufferReference = this.ParseInteger(),
                        IndexSampleKey = this.ParseInteger(),
                        OffsetBufferInSamplesKey = this.ParseInteger(),
                        BufferSize = this.ParseInteger(),
                        OffsetFirstSampleInBuffer = this.ParseInteger(),
                        BufferFilledBytes = this.ParseInteger(),
                        x0 = this.ParseInteger(),
                        AddTime = this.ParseInteger(),
                        UserInformation = this.ParseInteger(),
                        NewEvent = this.ParseInteger() == 1
                    });
                }
            });
        }

        // Calibration information.
        private void ParseCR()
        {
            this.ParseKey(FamosFileKeyType.CR, expectedKeyVersion: 1, () =>
            {
                this.CalibrationInformation = new FamosFileCalibrationInformation()
                {
                    ApplyTransformation = this.ParseInteger() == 1,
                    Factor = this.ParseDouble(),
                    Offset = this.ParseDouble(),
                    IsCalibrated = this.ParseInteger() == 1,
                    Unit = this.ParseString()
                };
            });
        }

        // Display information.
        private void ParseND()
        {
            this.ParseKey(FamosFileKeyType.ND, expectedKeyVersion: 1, () =>
            {
                this.DisplayInformation = new FamosFileDisplayInformation()
                {
                    R = this.ParseInteger(),
                    G = this.ParseInteger(),
                    B = this.ParseInteger(),
                    YMin = this.ParseDouble(),
                    YMax = this.ParseDouble()
                };
            });
        }

        // Event information.
        private void ParseCv()
        {
            this.ParseKey(FamosFileKeyType.Cv, expectedKeyVersion: 1, () =>
            {
                this.EventInformation = new FamosFileEventInformation()
                {
                    IndexEventListKey = this.ParseInteger(),
                    OffsetInEventList = this.ParseInteger(),
                    SubsequentEventCount = this.ParseInteger(),
                    EventDistance = this.ParseInteger(),
                    EventCount = this.ParseInteger(),
                    ValidNT = (FamosFileValidNTType)this.ParseInteger(),
                    ValidCD = (FamosFileValidCDType)this.ParseInteger(),
                    ValidCR1 = (FamosFileValidCR1Type)this.ParseInteger(),
                    ValidCR2 = (FamosFileValidCR2Type)this.ParseInteger(),
                };
            });
        }

        // Channel information.
        private void ParseCN()
        {
            this.ParseKey(FamosFileKeyType.Cv, expectedKeyVersion: 1, () =>
            {
                this.GroupIndex = this.ParseInteger();

                this.ParseInteger();

                this.BitIndex = this.ParseInteger();
                this.Name = this.ParseString();
                this.Comment = this.ParseString();
            });
        }

        #endregion

#warning TODO: implement ParseCV
    }
}
