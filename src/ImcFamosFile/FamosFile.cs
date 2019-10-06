using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    public class FamosFile : FamosFileBase
    {
        #region Fields

        private const int SUPPORTED_VERSION = 2;

        #endregion

        #region Constructors

        public FamosFile()
        {
            //
        }

        public FamosFile(BinaryReader reader) : base(reader)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!this.Reader.BaseStream.CanSeek)
                throw new NotSupportedException("The underlying stream must be seekable.");

            this.DeserializeFile();
            this.Prepare();
            this.Validate();
        }

        #endregion

        #region Properties

        public int FormatVersion { get; } = 2;
        public int Processor { get; } = 1;
        public int Language { get; set; } = 0;

        public FamosFileDataOriginInfo? DataOriginInfo { get; set; }

        public List<FamosFileText> Texts { get; private set; } = new List<FamosFileText>();
        public List<FamosFileGroup> Groups { get; private set; } = new List<FamosFileGroup>();
        public List<FamosFileDataField> DataFields { get; private set; } = new List<FamosFileDataField>();
        public List<FamosFileEvent> Events { get; private set; } = new List<FamosFileEvent>();
        public List<FamosFileRawData> RawData { get; private set; } = new List<FamosFileRawData>();

        #endregion

        #region "Methods"

        public int GetCodePage()
        {
            return this.CodePage;
        }

        public void Save(string filePath, int codePage)
        {
            this.Validate();

#warning TODO: implement Save()
        }

        private FamosFileGroup GetOrCreateGroup(int id)
        {
            var group = this.Groups.FirstOrDefault(current => current.Index == id);

            if (group == null)
            {
                group = new FamosFileGroup(id);
                this.Groups.Add(group);
            }

            return group;
        }

        internal override void Prepare()
        {
            // associate channel infos to groups
            foreach (var channelInfo in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.ChannelInfos)))
            {
                var group = this.GetOrCreateGroup(channelInfo.GroupIndex);
                group.ChannelInfos.Add(channelInfo);
            }

            // associate raw data to buffers
            foreach (var buffer in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.Buffers)))
            {
                buffer.RawData = this.RawData.FirstOrDefault(rawData => rawData.Index == buffer.RawDataReference);
            }

            // sort groups
            this.Groups = this.Groups.OrderBy(x => x.Index).ToList();

            // sort raw data
            this.RawData = this.RawData.OrderBy(x => x.Index).ToList();

            // prepare data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.Prepare();
            }
        }

        internal override void Validate()
        {
            // check if group indices are consistent
            foreach (var group in this.Groups)
            {
                var expected = group.Index;
                var actual = this.Groups.IndexOf(group) + 1;

                if (expected != actual)
                    throw new FormatException($"The group indices are not consistent. Expected '{expected}', got '{actual}'.");
            }

            // check if raw data indices are consistent
            foreach (var rawData in this.RawData)
            {
                var expected = rawData.Index;
                var actual = this.RawData.IndexOf(rawData) + 1;

                if (expected != actual)
                    throw new FormatException($"The raw data indices are not consistent. Expected '{expected}', got '{actual}'.");
            }

            // check if buffer's raw data is part of this instance
            foreach (var buffer in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.Buffers)))
            {
#warning: Solve this.
                if (!this.RawData.Contains(buffer.RawData))
                {
                    throw new FormatException("The buffers' raw data must be part of the famos file's raw data collection.");
                };
            }

            // validate data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.Validate();
            }
        }

        #endregion

        #region KeyParsing

        // parse file
        private void DeserializeFile()
        {
            // CF
            this.DeserializeCF();

            // CK
            this.DeserializeCK();

            try
            {
                while (true)
                {
                    var nextKeyType = this.DeserializeKeyType();

                    // Unknown
                    if (nextKeyType == FamosFileKeyType.Unknown)
                    {
                        this.SkipKey();
                        continue;
                    }

                    // NO
                    else if (nextKeyType == FamosFileKeyType.NO)
                        this.DeserializeNO();

                    // NL 
                    else if (nextKeyType == FamosFileKeyType.NL)
                        this.DeserializeNL();

                    // CB
                    else if (nextKeyType == FamosFileKeyType.CB)
                        this.DeserializeCB();

                    // CT
                    else if (nextKeyType == FamosFileKeyType.CT)
                        this.DeserializeCT();

                    // CI
                    else if (nextKeyType == FamosFileKeyType.CI)
                        this.DeserializeCI();

                    // CV
                    else if (nextKeyType == FamosFileKeyType.CV)
                        this.DeserializeCV();

                    // CS 
                    else if (nextKeyType == FamosFileKeyType.CS)
                        this.DeserializeCS();

                    // CG
                    else if (nextKeyType == FamosFileKeyType.CG)
                    {
                        var group = new FamosFileDataField(this.Reader, this.CodePage);
                        this.DataFields.Add(group);
                    }

                    else
                        //throw new FormatException($"Unexpected key '{keyType}'.");
                        this.SkipKey();
                }
            }
            catch (EndOfStreamException)
            {
                //
            }
        }

        // Format version and processor type.
        private void DeserializeCF()
        {
            this.DeserializeKey(FamosFileKeyType.CF, expectedKeyVersion: SUPPORTED_VERSION, keySize =>
            {
                var processor = this.DeserializeInt32();

                if (processor != 1)
                    throw new FormatException($"Expected processor value '1', got '{processor}'.");
            });
        }

        // Starts a group of keys.
        private void DeserializeCK()
        {
            this.DeserializeKey(FamosFileKeyType.CK, expectedKeyVersion: 1, keySize =>
            {
                var unknown = this.DeserializeInt32();
                var keyGroupIsClosed = this.DeserializeInt32() == 1;

                if (!keyGroupIsClosed)
                    throw new FormatException($"The key group is not closed. This may be a hint to an interruption that occured while writing the file content to disk.");
            });
        }

        // Origin of data.
        private void DeserializeNO()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.DataOriginInfo = new FamosFileDataOriginInfo()
                {
                    DataOrigin = (FamosFileDataOrigin)this.DeserializeInt32(),
                    Name = this.DeserializeString(),
                    Comment = this.DeserializeString()
                };
            });
        }

        // Code page.
        private void DeserializeNL()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.CodePage = this.DeserializeInt32();
                this.Language = this.DeserializeHex();
            });
        }

        // Group definition.
        private void DeserializeCB()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var groupIndex = this.DeserializeInt32();
                var group = this.GetOrCreateGroup(groupIndex);

                group.Name = this.DeserializeString();
                group.Comment = this.DeserializeString();
            });
        }

        // Text definition.
        private void DeserializeCT()
        {
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    var groupIndex = this.DeserializeInt32();
                    var text = new FamosFileText()
                    {
                        Name = this.DeserializeString(),
                        Text = this.DeserializeString(),
                        Comment = this.DeserializeString()
                    };

                    if (groupIndex == 0)
                        this.Texts.Add(text);
                    else
                    {
                        var group = this.GetOrCreateGroup(groupIndex);
                        group.Texts.Add(text);
                    }
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    var groupIndex = this.DeserializeInt32();

                    var name = this.DeserializeString();
                    var texts = this.DeserializeStringArray();
                    var comment = this.DeserializeString();
                    var text = new FamosFileText(texts: texts)
                    {
                        Name = name,
                        Comment = comment
                    };

                    if (groupIndex == 0)
                        this.Texts.Add(text);
                    else
                    {
                        var group = this.GetOrCreateGroup(groupIndex);
                        group.Texts.Add(text);
                    }
                });
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }
        }

        // Single value.
        private void DeserializeCI()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var groupIndex = this.DeserializeInt32();
                var group = this.GetOrCreateGroup(groupIndex);

                var dataType = (FamosFileDataType)this.DeserializeInt32();
                var name = this.DeserializeString();

                var value = dataType switch
                {
                    FamosFileDataType.UInt8         => this.Reader.ReadByte(),
                    FamosFileDataType.Int8          => this.Reader.ReadSByte(),
                    FamosFileDataType.UInt16        => this.Reader.ReadUInt16(),
                    FamosFileDataType.Int16         => this.Reader.ReadInt16(),
                    FamosFileDataType.UInt32        => this.Reader.ReadUInt32(),
                    FamosFileDataType.Int32         => this.Reader.ReadInt32(),
                    FamosFileDataType.Float32       => this.Reader.ReadSingle(),
                    FamosFileDataType.Float64       => this.Reader.ReadDouble(),
                    FamosFileDataType.Digital16Bit  => this.Reader.ReadUInt16(),
                    FamosFileDataType.UInt48        => BitConverter.ToUInt64(this.Reader.ReadBytes(6)),
                    _                               => throw new FormatException("The data type of the single value is invalid.")
                };

                // read left over comma
                this.Reader.ReadByte();

                var unit = this.DeserializeString();
                var comment = this.DeserializeString();
                var time = BitConverter.ToDouble(this.DeserializeKeyPart());

                group.SingleValues.Add(new FamosFileSingleValue(value)
                {
                    DataType = dataType,
                    Name = name,
                    Unit = unit,
                    Comment = comment,
                    Time = time
                });
            });
        }

        // Event data.
        private void DeserializeCV()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                var eventCount = this.DeserializeInt32();

                for (int i = 0; i < eventCount; i++)
                {
                    var index = this.DeserializeInt32();
                    var offsetLo = this.Reader.ReadUInt32();
                    var lengthLo = this.Reader.ReadUInt32();
                    var time = this.Reader.ReadDouble();
                    var amplitudeOffset0 = this.Reader.ReadDouble();
                    var amplitudeOffset1 = this.Reader.ReadDouble();
                    var x0 = this.Reader.ReadDouble();
                    var amplificationFactor0 = this.Reader.ReadDouble();
                    var amplificationFactor1 = this.Reader.ReadDouble();
                    var dx = this.Reader.ReadDouble();
                    var offsetHi = this.Reader.ReadUInt32();
                    var lengthHi = this.Reader.ReadUInt32();

                    var offset = offsetLo + (offsetHi << 32);
                    var length = lengthLo + (lengthHi << 32);

                    this.Events.Add(new FamosFileEvent()
                    {
                        Index = index,
                        Offset = offset,
                        Length = length,
                        Time = time,
                        AmplitudeOffset0 = amplitudeOffset0,
                        AmplitudeOffset1 = amplitudeOffset1,
                        x0 = x0,
                        AmplificationFactor0 = amplificationFactor0,
                        AmplificationFactor1 = amplificationFactor1,
                        dx = dx
                    });
                }
            });
        }

        // Raw data.
        private void DeserializeCS()
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.RawData.Add(new FamosFileRawData()
                {
                    Index = this.DeserializeInt32(),
                    Length = keySize,
                    FileOffset = this.Reader.BaseStream.Position
                });

                this.Reader.BaseStream.Seek(keySize, SeekOrigin.Current);
            });
        }

        #endregion
    }
}
