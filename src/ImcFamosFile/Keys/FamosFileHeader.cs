using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    public class FamosFileHeader : FamosFileBaseExtended
    {
        #region Fields

        protected const int SUPPORTED_VERSION = 2;

        private int _processor;

        #endregion

        #region Constructors

        public FamosFileHeader()
        {
            this.Initialize();
        }

        protected FamosFileHeader(BinaryReader reader) : base(reader, 0)
        {
            this.Initialize();
        }

        #endregion

        #region Properties

        public int FormatVersion { get; } = 2;

        public int Processor
        {
            get { return _processor; }
            set
            {
                if (value != 1)
                    throw new FormatException($"Expected processor value '1', got '{value}'.");

                _processor = value;
            }
        }

        public FamosFileLanguageInfo? LanguageInfo { get; set; }
        public FamosFileDataOriginInfo? DataOriginInfo { get; set; }

        public List<FamosFileText> Texts { get; private set; } = new List<FamosFileText>();
        public List<FamosFileSingleValue> SingleValues { get; private set; } = new List<FamosFileSingleValue>();
        public List<FamosFileChannel> Channels { get; private set; } = new List<FamosFileChannel>();

        public List<FamosFileCustomKey> CustomKeys { get; private set; } = new List<FamosFileCustomKey>();
        public List<FamosFileGroup> Groups { get; protected set; } = new List<FamosFileGroup>();
        public List<FamosFileDataField> DataFields { get; private set; } = new List<FamosFileDataField>();
        public List<FamosFileRawData> RawData { get; protected set; } = new List<FamosFileRawData>();

        protected override FamosFileKeyType KeyType => FamosFileKeyType.CF;

        #endregion

        #region "Methods"

        internal override void Validate()
        {
            // validate data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.Validate();
            }

            // check if all texts are assigned to a single group
            var textsByGroup = this.GetItemsByGroups(group => group.Texts, () => this.Texts);

            if (textsByGroup.Count() != textsByGroup.Distinct().Count())
                throw new FormatException("A text must be assigned to a single group only.");

            // check if all single values are assigned to a single group
            var singleValuesByGroup = this.GetItemsByGroups(group => group.SingleValues, () => this.SingleValues);

            if (singleValuesByGroup.Count() != singleValuesByGroup.Distinct().Count())
                throw new FormatException("A single value must be assigned to a single group only.");

            // check if all channels are assigned to a single group
            var channelsByGroup = this.GetItemsByGroups(group => group.Channels, () => this.Channels);

            if (channelsByGroup.Count() != channelsByGroup.Distinct().Count())
                throw new FormatException("A channel must be assigned to a single group only.");

            foreach (var channel in this.GetItemsByComponents(component => component.Channels))
            {
                if (!channelsByGroup.Contains(channel))
                    throw new FormatException($"The channel named '{channel.Name}' must be assigned to a group.");
            }

            // check if all custom keys are unique
            var distinctCount = this.CustomKeys.Select(customKey => customKey.Key).Distinct().Count();

            if (this.CustomKeys.Count != distinctCount)
                throw new FormatException($"Custom keys must be globally unique.");

            // check if pack info's buffers are somewhere defined
            var bufferInfoBuffers = this.GetItemsByComponents(component => component.BufferInfo.Buffers);

            foreach (var buffer in this.GetItemsByComponents(component => component.PackInfo.Buffers))
            {
                if (!bufferInfoBuffers.Contains(buffer))
                    throw new FormatException("Every buffer associated to a pack info must be also assigned to the components buffer info property.");
            }

            // check if buffer's raw data is part of this instance
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                if (!this.RawData.Contains(buffer.RawData))
                {
                    throw new FormatException("The buffers' raw data must be part of the famos file's raw data collection.");
                };
            }
        }

        protected List<T> GetItemsByGroups<T>(Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>> getDefaultCollection)
        {
            return getDefaultCollection().Concat(this.Groups.SelectMany(group => getGroupCollection(group))).ToList();
        }

        protected List<T> GetItemsByComponents<T>(Func<FamosFileComponent, List<T>> getComponentCollection)
        {
            return this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => getComponentCollection(component))).ToList();
        }

        protected List<T> GetItemsByComponents<T>(Func<FamosFileComponent, T> getComponentValue)
        {
            return this.DataFields.SelectMany(dataField => dataField.Components.Select(component => getComponentValue(component))).ToList();
        }

        private void Initialize()
        {
            if (!BitConverter.IsLittleEndian)
                throw new NotSupportedException("Only little-endian systems are supported.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #endregion

        #region Serialization

        public void Save(string filePath, Action<BinaryWriter> writeData)
        {
            this.Save(filePath, FileMode.CreateNew, writeData);
        }

        public void Save(string filePath, FileMode fileMode, Action<BinaryWriter> writeData)
        {
            this.Save(File.Open(filePath, fileMode, FileAccess.Write), writeData);
        }

        public void Save(Stream stream, Action<BinaryWriter> writeData)
        {
            if (!stream.CanWrite || !stream.CanSeek)
                throw new InvalidOperationException("The stream must be writeable and seekable.");

            this.Validate();
            this.BeforeSerialize();

            var codePage = this.LanguageInfo is null ? 0 : this.LanguageInfo.CodePage;
            var encoding = Encoding.GetEncoding(codePage);

            using (var writer = new BinaryWriter(stream, encoding))
            {
                this.Serialize(writer);
                writeData.Invoke(writer);
            }
        }

        public void WriteSingle<T>(BinaryWriter writer, FamosFileComponent component, T[] data) where T : unmanaged
        {
            if (component.PackInfo.Buffers.First().RawData.CompressionType != FamosFileCompressionType.Uncompressed)
                throw new InvalidOperationException("This implementation does not support writing compressed data yet.");

#warning TODO: Implement WriteSingle().
        }

        internal override void BeforeSerialize()
        {
            // prepare data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.BeforeSerialize();
            }

            // update raw data indices
            foreach (var rawData in this.RawData)
            {
                rawData.Index = this.RawData.IndexOf(rawData) + 1;
            }

            // update group indices
            foreach (var group in this.Groups)
            {
                group.Index = this.Groups.IndexOf(group) + 1;
            }

            // update group index of texts
            foreach (var text in this.Texts)
            {
                text.GroupIndex = 0;
            }

            foreach (var group in this.Groups)
            {
                foreach (var text in group.Texts)
                {
                    var groupIndex = this.Groups.IndexOf(group) + 1;
                    text.GroupIndex = groupIndex;
                }
            }

            // update group index of single values
            foreach (var singleValue in this.SingleValues)
            {
                singleValue.GroupIndex = 0;
            }

            foreach (var group in this.Groups)
            {
                foreach (var singleValue in group.SingleValues)
                {
                    var groupIndex = this.Groups.IndexOf(group) + 1;
                    singleValue.GroupIndex = groupIndex;
                }
            }

            // update group index of channels
            foreach (var channel in this.Channels)
            {
                channel.GroupIndex = 0;
            }

            foreach (var group in this.Groups)
            {
                foreach (var channel in group.Channels)
                {
                    var groupIndex = this.Groups.IndexOf(group) + 1;
                    channel.GroupIndex = groupIndex;
                }
            }

            // update raw data index of buffers
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                var rawDataIndex = this.RawData.IndexOf(buffer.RawData) + 1;
                buffer.RawDataIndex = rawDataIndex;
            }

            // assign monotonous increasing buffer references to pack infos.
            var i = 1;

            foreach (var packInfo in this.GetItemsByComponents(component => component.PackInfo))
            {
                packInfo.BufferReference = i;
                i++;
            }
        }

        internal override void Serialize(BinaryWriter writer)
        {
            // CF
            var data = new object[]
            {
                this.Processor
            };

            this.SerializeKey(writer, SUPPORTED_VERSION, data, addLineBreak: false);

            // CK
            new FamosFileKeyGroup().Serialize(writer);

            // NO
            this.DataOriginInfo?.Serialize(writer);

            // NL
            this.LanguageInfo?.Serialize(writer);

            // NE - do nothing

            // Ca - do nothing

            // NU
            foreach (var customKey in this.CustomKeys)
            {
                customKey.Serialize(writer);
            }

            // CB
            foreach (var group in this.Groups)
            {
                group.Serialize(writer);
            }

            // CG
            foreach (var dataField in this.DataFields)
            {
                dataField.Serialize(writer);
            }

            // CT
            foreach (var text in this.Texts)
            {
                text.Serialize(writer);
            }

            foreach (var text in this.Groups.SelectMany(group => group.Texts))
            {
                text.Serialize(writer);
            }

            // CI
            foreach (var singleValue in this.SingleValues)
            {
                singleValue.Serialize(writer);
            }

            foreach (var singleValue in this.Groups.SelectMany(group => group.SingleValues))
            {
                singleValue.Serialize(writer);
            }

            // Nv - do nothing

            // CS
            foreach (var rawData in this.RawData)
            {
                rawData.Serialize(writer);
                writer.Flush();
            }

            // Close CK.
            writer.BaseStream.Seek(20, SeekOrigin.Begin);
            writer.Write('1');
        }

        #endregion
    }
}
