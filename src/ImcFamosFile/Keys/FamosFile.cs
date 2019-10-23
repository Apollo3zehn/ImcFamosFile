using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    public class FamosFile : FamosFileHeader, IDisposable
    {
        #region Fields

        private readonly List<FamosFileText> _texts = new List<FamosFileText>();
        private readonly List<FamosFileSingleValue> _singleValues = new List<FamosFileSingleValue>();

        #endregion

        #region Constructors

        private FamosFile(string filePath) : base(new BinaryReader(File.OpenRead(filePath)))
        {
            this.Deserialize();
            this.AfterDeserialize();
            this.Validate();
        }

        private FamosFile(Stream stream) : base(new BinaryReader(stream))
        {
            this.Deserialize();
            this.AfterDeserialize();
            this.Validate();
        }

        #endregion

        #region "Methods"

        public void Dispose()
        {
            this.Reader.Dispose();
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

        public FamosFileChannelData ReadSingle(FamosFileChannel channel)
        {
            return this.ReadSingle(channel, 0, 0);
        }

        public FamosFileChannelData ReadSingle(FamosFileChannel channel, int start, int length)
        {
            FamosFileField? foundField = null;
            FamosFileComponent? foundComponent = null;

            foreach (var field in this.Fields)
            {
                foreach (var component in field.Components)
                {
                    if (component.Channels.Contains(channel))
                    {
                        foundField = field;
                        foundComponent = component;
                        break;
                    }
                }

                if (foundComponent != null)
                    break;
            }

            if (foundField is null || foundComponent is null)
                throw new FormatException($"The provided channel is not part of any {nameof(FamosFileField)} instance.");

            List<FamosFileComponent> GetAlternatingComponents()
            {
                FamosFileComponentType filter;

                if (foundComponent.Type == FamosFileComponentType.Primary)
                    filter = FamosFileComponentType.Secondary;
                else if (foundComponent.Type == FamosFileComponentType.Secondary)
                    filter = FamosFileComponentType.Primary;
                else
                    throw new InvalidOperationException($"The component type '{foundComponent.Type}' is unknown.");

                return new List<FamosFileComponent> { foundComponent, foundField.Components.First(component => component.Type == filter) };
            }

            var components = foundField.Type switch
            {
                FamosFileFieldType.MultipleYToSingleEquidistantTime => new List<FamosFileComponent>() { foundComponent },
                FamosFileFieldType.MultipleYToSingleMonotonousTime  => GetAlternatingComponents().OrderBy(component => component.Type).ToList(),
                FamosFileFieldType.MultipleYToSingleXOrViceVersa    => GetAlternatingComponents().OrderBy(component => component.Type).ToList(),
                _                                                   => foundField.Components.OrderBy(component => component.Type).ToList()
            };

            var componentsData = new List<FamosFileComponentData>();

            foreach (var component in components)
            {
                T? FindFirst<T>(FamosFileField field, FamosFileComponent component, T? defaultValue, Func<FamosFileComponent, T?> getPropertyValue) where T : class
                {
                    var selfValue = getPropertyValue(component);
                    var selfOrParentValue = selfValue ?? defaultValue;

                    if (selfOrParentValue != null)
                        return selfOrParentValue;
                    else
                    {
                        var index = field.Components.IndexOf(component);
                        var siblingValue = field.Components.Take(index).FirstOrDefault(component => getPropertyValue(component) != null);

                        return siblingValue != null ? getPropertyValue(siblingValue) : null;
                    }
                }

                // find shared instances
                var xAxisScaling = FindFirst(foundField, component, foundField.XAxisScaling, component => component.XAxisScaling);
                var zAxisScaling = FindFirst(foundField, component, foundField.ZAxisScaling, component => component.ZAxisScaling);
                var triggerTime = FindFirst(foundField, component, foundField.TriggerTime, component => component.TriggerTime);

                //
                FamosFileComponentData componentData;

                var data = this.ReadComponentData(component, start, length);

                componentData = component.PackInfo.DataType switch
                {
                    FamosFileDataType.UInt8             => new FamosFileComponentData<Byte>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int8              => new FamosFileComponentData<SByte>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt16            => new FamosFileComponentData<UInt16>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int16             => new FamosFileComponentData<Int16>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt32            => new FamosFileComponentData<UInt32>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int32             => new FamosFileComponentData<Int32>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Float32           => new FamosFileComponentData<Single>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Float64           => new FamosFileComponentData<Double>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.ImcDevicesTransitionalRecording => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.ImcDevicesTransitionalRecording}' is not supported."),
                    FamosFileDataType.AsciiTimeStamp    => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.AsciiTimeStamp}' is not supported."),
                    FamosFileDataType.Digital16Bit      => new FamosFileComponentData<UInt16>(component, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt48            => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.UInt48}' is not supported."),
                                                        _ => throw new NotSupportedException($"The specified data type '{component.PackInfo.DataType}' is not supported.")
                };

                componentsData.Add(componentData);
            }

            return new FamosFileChannelData(foundComponent.Name, foundField.Type, componentsData);
        }

        public List<FamosFileChannelData> ReadGroup(List<FamosFileChannel> channels)
        {
            return channels.Select(channel => this.ReadSingle(channel)).ToList();
        }

        public List<FamosFileChannelData> ReadAll()
        {
            return this.GetItemsByComponents(component => component.Channels).Select(channel => this.ReadSingle(channel)).ToList();
        }

        private byte[] ReadComponentData(FamosFileComponent component, int start, int length)
        {
            var packInfo = component.PackInfo;
            var buffer = packInfo.Buffers.First();
            var fileOffset = buffer.RawData.FileReadOffset + buffer.RawDataOffset + buffer.Offset + packInfo.Offset;

            // read all data at once
            if (packInfo.IsContiguous)
            {
                var actualLength = component.GetSize(start, length);
                var valueOffset = start * packInfo.ValueSize;

                this.Reader.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);
                return this.Reader.ReadBytes(actualLength * packInfo.ValueSize);
            }

            // read grouped data
            else
            {
                var valueLength = component.GetSize(start, length);
                var valueOffset = start * packInfo.ByteGroupSize;

                var byteLength = valueLength * packInfo.ValueSize;
                var data = new byte[byteLength];

                this.Reader.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);

                var bytePosition = 0;
                var valuePosition = 0;

                while (true)
                {
                    // read x subsequent values
                    for (int j = 0; j < packInfo.GroupSize; j++)
                    {
                        // read a single value
                        if (valueLength - valuePosition >= 1)
                        {
                            for (int k = 0; k < packInfo.ValueSize; k++)
                            {
                                var position = valuePosition * packInfo.ValueSize + k;
                                data[position] = this.Reader.ReadByte();
                            }

                            bytePosition += packInfo.ValueSize;
                            valuePosition += 1;
                        }
                    }

                    // skip x bytes
                    if (byteLength - bytePosition >= packInfo.ByteGapSize)
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

                // Reset propertyInfoReceiver if next key type is not 'Np'.
                if (nextKeyType != FamosFileKeyType.Np)
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
                    this.Fields.Add(new FamosFileField(this.Reader, this.CodePage));

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
            foreach (var field in this.Fields)
            {
                field.AfterDeserialize();
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
