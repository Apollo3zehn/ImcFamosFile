using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FamosFile.NET
{
    public class FamosFile : FamosFileBase
    {
#warning CodePage is redundant.

        #region Fields

        private const int SUPPORTED_VERSION = 2;

        #endregion

        #region Constructors

        public FamosFile(BinaryReader reader)
            : base(reader)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!this.Reader.BaseStream.CanSeek)
                throw new NotSupportedException("The underlying stream must be seekable.");

            this.Groups = new List<FamosFileGroup>();
            this.DataFields = new List<FamosFileDataField>();
            this.Events = new List<FamosFileEvent>();
            this.RawData = new List<FamosFileRawData>();

            try
            {
                this.DeserializeFile();
            }
            catch (EndOfStreamException)
            {
                //
            }
        }

        #endregion

        #region Properties

        public int FormatVersion { get; set; }
        public int Processor { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public int Language { get; set; }
        public FamosFileDataOrigin DataOrigin { get; set; }
        public List<FamosFileGroup> Groups { get; set; }
        public List<FamosFileDataField> DataFields { get; set; }
        public List<FamosFileEvent> Events { get; set; }
        public List<FamosFileRawData> RawData { get; set; }

        #endregion

        #region "Methods"

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

        #endregion

        #region KeyParsing

        // parse file
        private void DeserializeFile()
        {
            // CF
            this.DeserializeCF();

            // CK
            this.DeserializeCK();

            //
            this.DataOrigin = FamosFileDataOrigin.Unknown;
            this.Name = string.Empty;
            this.Comment = string.Empty;

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

        // Format version and processor type.
        private void DeserializeCF()
        {
            this.DeserializeKey(FamosFileKeyType.CF, expectedKeyVersion: SUPPORTED_VERSION, keySize =>
            {
                this.FormatVersion = 2;
                this.Processor = this.DeserializeInt32();
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
                this.DataOrigin = (FamosFileDataOrigin)this.DeserializeInt32();
                this.Name = this.DeserializeString();
                this.Comment = this.DeserializeString();
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
                    var group = this.GetOrCreateGroup(groupIndex);

                    group.Texts.Add(new FamosFileText()
                    {
                        Name = this.DeserializeString(),
                        Text = this.DeserializeString(),
                        Comment = this.DeserializeString()
                    });
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    var blockIndex = this.DeserializeInt32();
                    var group = this.GetOrCreateGroup(blockIndex);

                    var name = this.DeserializeString();
                    var texts = this.DeserializeStringArray();
                    var comment = this.DeserializeString();

                    group.Texts.Add(new FamosFileText(texts: texts)
                    {
                        Name = name,
                        Comment = comment
                    });
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
