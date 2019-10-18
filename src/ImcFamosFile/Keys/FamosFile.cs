using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    public class FamosFile : FamosFileBaseExtended, IDisposable
    {
        #region Fields

        private const int SUPPORTED_VERSION = 2;

        private int _processor;

        private readonly List<FamosFileText> _texts = new List<FamosFileText>();
        private readonly List<FamosFileSingleValue> _singleValues = new List<FamosFileSingleValue>();

        #endregion

        #region Constructors

        public FamosFile()
        {
            this.Initialize();
        }

        private FamosFile(string filePath) : base(new BinaryReader(File.OpenRead(filePath)), 0)
        {
            this.Initialize();

            this.Deserialize();
            this.AfterDeserialize();
            this.Validate();
        }

        private FamosFile(Stream stream) : base(new BinaryReader(stream), 0)
        {
            this.Initialize();

            this.Deserialize();
            this.AfterDeserialize();
            this.Validate();
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
        public List<FamosFileGroup> Groups { get; private set; } = new List<FamosFileGroup>();
        public List<FamosFileDataField> DataFields { get; private set; } = new List<FamosFileDataField>();
        public List<FamosFileRawData> RawData { get; private set; } = new List<FamosFileRawData>();


        protected override FamosFileKeyType KeyType => FamosFileKeyType.CF;

        #endregion

        #region "Methods"

        public List<FamosFileChannelData> ReadAll()
        {
            return this.GetItemsByComponents(component => component.Channels).Select(channel => this.ReadSingleChannel(channel)).ToList();
        }

        public FamosFileChannelData ReadSingleChannel(FamosFileChannel channel)
        {
            if (this.Reader is null)
                throw new InvalidOperationException("Data can only be read with an actually opened file.");

            if (channel.BitIndex > 0)
                throw new InvalidOperationException("This implementation does not support reading boolean data yet. Please send a sample file to the package author to find a solution.");

            var dataField = this.DataFields.FirstOrDefault(dataField => dataField.Components.Any(current => current.Channels.Contains(channel)));

            if (dataField is null)
                throw new FormatException($"The provided channel is not part of any {nameof(FamosFileDataField)} instance.");

            var componentsData = new List<FamosFileComponentData>();

            foreach (var component in dataField.Components)
            {
                FamosFileComponentData componentData;

                var data = this.ReadComponentData(component);

                switch (component.PackInfo.DataType)
                {
                    case FamosFileDataType.UInt8:
                        componentData = new FamosFileComponentData<Byte>(component, data);
                        break;

                    case FamosFileDataType.Int8:
                        componentData = new FamosFileComponentData<SByte>(component, data);
                        break;

                    case FamosFileDataType.UInt16:
                        componentData = new FamosFileComponentData<UInt16>(component, data);
                        break;

                    case FamosFileDataType.Int16:
                        componentData = new FamosFileComponentData<Int16>(component, data);
                        break;

                    case FamosFileDataType.UInt32:
                        componentData = new FamosFileComponentData<UInt32>(component, data);
                        break;

                    case FamosFileDataType.Int32:
                        componentData = new FamosFileComponentData<Int32>(component, data);
                        break;

                    case FamosFileDataType.Float32:
                        componentData = new FamosFileComponentData<Single>(component, data);
                        break;

                    case FamosFileDataType.Float64:
                        componentData = new FamosFileComponentData<Double>(component, data);
                        break;

                    case FamosFileDataType.ImcDevicesTransitionalRecording:
                        throw new NotSupportedException($"Reading data of type '{FamosFileDataType.ImcDevicesTransitionalRecording}' is not supported.");

                    case FamosFileDataType.AsciiTimeStamp:
                        throw new NotSupportedException($"Reading data of type '{FamosFileDataType.AsciiTimeStamp}' is not supported.");

                    case FamosFileDataType.Digital16Bit:
                        componentData = new FamosFileComponentData<UInt16>(component, data);
                        break;


                    case FamosFileDataType.UInt48:
                        throw new NotSupportedException($"Reading data of type '{FamosFileDataType.UInt48}' is not supported.");

                    default:
                        throw new NotSupportedException($"The specified data type '{component.PackInfo.DataType}' is not supported.");
                }
                
                componentsData.Add(componentData);
            }

            return new FamosFileChannelData(dataField.Type, componentsData);
        }

        public void Dispose()
        {
            this.Reader.Dispose();
        }

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

        private void Initialize()
        {
            if (!BitConverter.IsLittleEndian)
                throw new NotSupportedException("Only little-endian systems are supported.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private byte[] ReadComponentData(FamosFileComponent component)
        {
            return this.ReadComponentData(component, 0, 0);
        }

        private byte[] ReadComponentData(FamosFileComponent component, int start, int length)
        {
            var packInfo = component.PackInfo;
            var buffer = packInfo.Buffers.First();

            if (buffer.IsRingBuffer)
                throw new InvalidOperationException("This implementation does not yet support reading ring buffers. Please send a sample file to the package author to find a solution.");

            if (packInfo.Mask != 0)
                throw new InvalidOperationException("This implementation does not yet support reading masked data. Please send a sample file to the package author to find a solution.");

            return this.InternalReadComponentData(packInfo, buffer, start, length);
        }

        private byte[] InternalReadComponentData(FamosFilePackInfo packInfo, FamosFileBuffer buffer, int start, int length)
        {
            var fileOffset = buffer.RawData.FileOffset + buffer.RawDataOffset + buffer.Offset + packInfo.Offset;

            // read all data at once
            if (packInfo.IsContiguous)
            {
                var actualLength = this.GetValueCount(packInfo, buffer, start, length, true);
                var valueOffset = start * packInfo.ValueSize;

                this.Reader.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);
                return this.Reader.ReadBytes(actualLength * packInfo.ValueSize);
            }

            // read grouped data
            else
            {
                if (packInfo.GroupSize > 1)
                    throw new InvalidOperationException("This implementation does not yet support a pack info group size > '1'. Please send a sample file to the package author to find a solution.");

                var actualBufferLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset;
                var actualLength = this.GetValueCount(packInfo, buffer, start, length, false);
                var data = new byte[actualLength * packInfo.ValueSize];
                var valueOffset = start * packInfo.ByteGroupSize;

                this.Reader.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);

                var bytePosition = 0;
                var valuePosition = 0;

                while (true)
                {
                    // read x subsequent values
                    for (int j = 0; j < packInfo.GroupSize; j++)
                    {
                        // read a single value
                        if (actualBufferLength - bytePosition >= packInfo.ValueSize)
                        {
                            for (int k = 0; k < packInfo.ValueSize; k++)
                            {
                                data[valuePosition] = this.Reader.ReadByte();
                                bytePosition += 1;
                            }

                            valuePosition += 1;
                        }
                    }

                    // skip x bytes
                    if (actualBufferLength - bytePosition >= packInfo.ByteGapSize)
                    {
                        this.Reader.BaseStream.Seek(packInfo.ByteGapSize, SeekOrigin.Current);
                        bytePosition += packInfo.ByteGapSize;
                    }
                    else
                    {
                        break;
                    }
                }

                return data;
            }
        }

        private long GetValueCount(FamosFilePackInfo packInfo, FamosFileBuffer buffer, int start, int length, bool isContiguous)
        {
            long maxLength;

            var actualBufferLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset;

            if (isContiguous)
            {
                maxLength = actualBufferLength / packInfo.ValueSize;
            }
            else
            {
                var rowSize = packInfo.ByteGroupSize + packInfo.ByteGapSize;
                maxLength = actualBufferLength / rowSize;

                if (actualBufferLength % rowSize >= packInfo.ValueSize)
                    maxLength += 1;
            }

            if (start + length > maxLength)
                throw new InvalidOperationException($"The specified '{nameof(start)}' and '{nameof(length)}' parameters lead to a dataset which is larger than the actual buffer.");

            return length > 0 ? length : maxLength;
        }

        #endregion

        #region Serialization

        public void Save(string filePath)
        {
            this.Save(filePath, FileMode.CreateNew);
        }

        public void Save(string filePath, FileMode fileMode)
        {
            this.Validate();
            this.BeforeSerialize();

            var codePage = this.LanguageInfo is null ? 0 : this.LanguageInfo.CodePage;
            var encoding = Encoding.GetEncoding(codePage);

            using (StreamWriter writer = new StreamWriter(File.Open(filePath, fileMode, FileAccess.Write), encoding))
            {
                this.Serialize(writer);
            }
        }

        private List<T> GetItemsByGroups<T>(Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>> getDefaultCollection)
        {
            return getDefaultCollection().Concat(this.Groups.SelectMany(group => getGroupCollection(group))).ToList();
        }

        private List<T> GetItemsByComponents<T>(Func<FamosFileComponent, List<T>> getComponentCollection)
        {
            return this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => getComponentCollection(component))).ToList();
        }

        private List<T> GetItemsByComponents<T>(Func<FamosFileComponent, T> getComponentValue)
        {
            return this.DataFields.SelectMany(dataField => dataField.Components.Select(component => getComponentValue(component))).ToList();
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

        internal override void Serialize(StreamWriter writer)
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

#warning TODO: Write CS key data.

            // Close CK.
            writer.BaseStream.Seek(20, SeekOrigin.Begin);
            writer.Write('1');
        }

        #endregion

        #region Deserialization

        public static FamosFile Open(string filePath)
        {
            return new FamosFile(filePath);
        }

        public static FamosFile Open(Stream stream)
        {
            return new FamosFile(stream);
        }

        private void Deserialize()
        {
            // CF
            var keyType = this.DeserializeKeyType();

            if (keyType != FamosFileKeyType.CF)
                throw new FormatException("The file is not a FAMOS file.");

            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                throw new FormatException($"Only files of format version '2' can be read.");
            }
            else if (keyVersion == SUPPORTED_VERSION)
            {
                this.DeserializeKey(keySize =>
                {
                    var processor = this.DeserializeInt32();
                    this.Processor = processor;
                });
            }
            else
            {
                throw new FormatException($"Expected key version '2', got '{keyVersion}'.");
            }

            // CK
            new FamosFileKeyGroup(this.Reader);

            // Now, all other keys.
            FamosFileBaseProperty? propertyInfoReceiver = null;

            while (true)
            {
                if (this.Reader.BaseStream.Position >= this.Reader.BaseStream.Length)
                    return;

                var nextKeyType = this.DeserializeKeyType();

                // Reset propertyInfoReceiver if next key type is not 'Nv'.
                if (nextKeyType != FamosFileKeyType.Nv)
                    propertyInfoReceiver = null;

                // Unknown
                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                // NO
                else if (nextKeyType == FamosFileKeyType.NO)
                    this.DataOriginInfo = new FamosFileDataOriginInfo(this.Reader, this.CodePage);

                // NL
                else if (nextKeyType == FamosFileKeyType.NL)
                {
                    this.LanguageInfo = new FamosFileLanguageInfo(this.Reader);
                    this.CodePage = this.LanguageInfo.CodePage;
                }

                // NE - only imc internal
                else if (nextKeyType == FamosFileKeyType.NE)
                    this.SkipKey();

                // Ca
                else if (nextKeyType == FamosFileKeyType.Ca)
                    throw new FormatException("The functionality of the 'Ca'-key is not supported. Please submit a sample .dat or .raw file to the package author to find a solution.");

                // NU
                else if (nextKeyType == FamosFileKeyType.NU)
                    this.CustomKeys.Add(new FamosFileCustomKey(this.Reader, this.CodePage));

                // CB
                else if (nextKeyType == FamosFileKeyType.CB)
                {
                    this.Groups.Add(new FamosFileGroup(this.Reader, this.CodePage));
                    propertyInfoReceiver = this.Groups.Last();
                }

                // CG
                else if (nextKeyType == FamosFileKeyType.CG)
                    this.DataFields.Add(new FamosFileDataField(this.Reader, this.CodePage));

                // CT
                else if (nextKeyType == FamosFileKeyType.CT)
                {
                    _texts.Add(new FamosFileText(this.Reader, this.CodePage));
                    propertyInfoReceiver = _texts.Last();
                }

                // CI
                else if (nextKeyType == FamosFileKeyType.CI)
                {
                    _singleValues.Add(new FamosFileSingleValue.Deserializer(this.Reader, this.CodePage).Deserialize());
                    propertyInfoReceiver = _singleValues.Last();
                }

                // CS 
                else if (nextKeyType == FamosFileKeyType.CS)
                    this.RawData.Add(new FamosFileRawData(this.Reader));

                // Nv - only for data manager
                else if (nextKeyType == FamosFileKeyType.Nv)
                    this.SkipKey();

                // Np
                else if (nextKeyType == FamosFileKeyType.Np)
                {
                    var propertyInfo = new FamosFilePropertyInfo(this.Reader, this.CodePage);

                    if (propertyInfoReceiver != null)
                        propertyInfoReceiver.PropertyInfo = propertyInfo;
                    else
                        throw new FormatException("Found property key in an unexpected location.");

                    propertyInfoReceiver = null;
                }

                // Cb
                else if (nextKeyType == FamosFileKeyType.Cb)
                    throw new FormatException("Although the format specification allows '|Cb' keys at any level, this implementation supports this key only at component level. Please send a sample file to the project maintainer to overcome this limitation in future.");

                else
                    //throw new FormatException($"Unexpected key '{keyType}'.");
                    this.SkipKey();
            }
        }

        private void AssignToGroup<T>(int groupIndex, T value, Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>> getDefaultCollection)
        {
            if (groupIndex == 0)
            {
                getDefaultCollection.Invoke().Add(value);
            }
            else
            {
                var group = this.Groups.FirstOrDefault(group => group.Index == groupIndex);

                if (group != null)
                    getGroupCollection.Invoke(group).Add(value);
                else
                    throw new FormatException("The referenced group does not exist.");
            }
        }

        internal override void AfterDeserialize()
        {
            // prepare data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.AfterDeserialize();
            }

            // check if group indices are consistent
            base.CheckIndexConsistency("group", this.Groups, current => current.Index);
            this.Groups = this.Groups.OrderBy(x => x.Index).ToList();

            // check if raw data indices are consistent
            base.CheckIndexConsistency("raw data", this.RawData, current => current.Index);
            this.RawData = this.RawData.OrderBy(x => x.Index).ToList();

            // assign text to group
            foreach (var text in _texts)
            {
                this.AssignToGroup(text.GroupIndex, text, group => group.Texts, () => this.Texts);
            }

            // assign single value to group
            foreach (var singleValue in _singleValues)
            {
                this.AssignToGroup(singleValue.GroupIndex, singleValue, group => group.SingleValues, () => this.SingleValues);
            }

            // assign channel info to group
            foreach (var channel in this.GetItemsByComponents(component => component.Channels))
            {
                this.AssignToGroup(channel.GroupIndex, channel, group => group.Channels, () => this.Channels);
            }

            // assign raw data to buffer
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                buffer.RawData = this.RawData.First(rawData => rawData.Index == buffer.RawDataIndex);
            }
        }

        #endregion
    }
}
