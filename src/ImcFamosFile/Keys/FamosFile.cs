using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImcFamosFile
{
    /// <summary>
    /// Represents an imc FAMOS file in a hierachical structure containing texts, single values, channels and more. It is capable of reading file data.
    /// </summary>
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
            stream.Seek(0, SeekOrigin.Begin);

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

        /// <summary>
        /// Opens the file at the location specified by the parameter <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to be opened.</param>
        /// <returns>Returns a new <see cref="FamosFile"/> instance.</returns>
        public static FamosFile Open(string filePath)
        {
            return new FamosFile(filePath);
        }

        /// <summary>
        /// Opens and reads the provided <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream containing the serialized FAMOS data.</param>
        /// <returns>Returns a new <see cref="FamosFile"/> instance.</returns>
        public static FamosFile Open(Stream stream)
        {
            return new FamosFile(stream);
        }

        /// <summary>
        /// Reads the full dataset associated to the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel that describes the data to read.</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> instance, which may consists of more than one dataset.</returns>
        public FamosFileChannelData ReadSingle(FamosFileChannel channel)
        {
            return this.ReadSingle(channel, 0, 0);
        }

        /// <summary>
        /// Reads a partial dataset associated to the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel that describes the data to read.</param>
        /// <param name="start">The reading start offset.</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> instance, which may consists of more than one dataset.</returns>
        public FamosFileChannelData ReadSingle(FamosFileChannel channel, int start)
        {
            return this.ReadSingle(channel, start, 0);
        }

        /// <summary>
        /// Reads a partial dataset associated to the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel that describes the data to read.</param>
        /// <param name="start">The reading start offset.</param>
        /// <param name="length">The number of the values to read.</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> instance, which may consists of more than one dataset.</returns>
        public FamosFileChannelData ReadSingle(FamosFileChannel channel, int start, int length)
        {
            var component = channel.Component;
            var foundField = this.GetField(component);

            List<FamosFileComponent> GetAlternatingComponents()
            {
                FamosFileComponentType filter;

                if (component.Type == FamosFileComponentType.Primary)
                    filter = FamosFileComponentType.Secondary;
                else if (component.Type == FamosFileComponentType.Secondary)
                    filter = FamosFileComponentType.Primary;
                else
                    throw new InvalidOperationException($"The component type '{component.Type}' is unknown.");

                return new List<FamosFileComponent> { component, foundField.Components.First(component => component.Type == filter) };
            }

            var components = foundField.Type switch
            {
                FamosFileFieldType.MultipleYToSingleEquidistantTime => new List<FamosFileComponent>() { component },
                FamosFileFieldType.MultipleYToSingleMonotonousTime  => GetAlternatingComponents().OrderBy(current => current.Type).ToList(),
                FamosFileFieldType.MultipleYToSingleXOrViceVersa    => GetAlternatingComponents().OrderBy(current => current.Type).ToList(),
                _                                                   => foundField.Components.OrderBy(current => current.Type).ToList()
            };

            var componentsData = new List<FamosFileComponentData>();
            var cache = new Dictionary<FamosFileComponent, FamosFileComponentData>();

            foreach (var currentComponent in components)
            {
                if (cache.ContainsKey(currentComponent))
                {
                    componentsData.Add(cache[currentComponent]);
                    continue;
                }

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
                var xAxisScaling = FindFirst(foundField, currentComponent, foundField.XAxisScaling, current => current.XAxisScaling);
                var zAxisScaling = FindFirst(foundField, currentComponent, foundField.ZAxisScaling, current => current.ZAxisScaling);
                var triggerTime = FindFirst(foundField, currentComponent, foundField.TriggerTime, current => current.TriggerTime);

                //
                FamosFileComponentData componentData;

                var data = this.ReadComponentData(currentComponent, start, length);

                componentData = currentComponent.PackInfo.DataType switch
                {
                    FamosFileDataType.UInt8             => new FamosFileComponentData<Byte>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int8              => new FamosFileComponentData<SByte>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt16            => new FamosFileComponentData<UInt16>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int16             => new FamosFileComponentData<Int16>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt32            => new FamosFileComponentData<UInt32>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Int32             => new FamosFileComponentData<Int32>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Float32           => new FamosFileComponentData<Single>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.Float64           => new FamosFileComponentData<Double>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.ImcDevicesTransitionalRecording => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.ImcDevicesTransitionalRecording}' is not supported."),
                    FamosFileDataType.AsciiTimeStamp    => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.AsciiTimeStamp}' is not supported."),
                    FamosFileDataType.Digital16Bit      => new FamosFileComponentData<UInt16>(currentComponent, xAxisScaling, zAxisScaling, triggerTime, data),
                    FamosFileDataType.UInt48            => throw new NotSupportedException($"Reading data of type '{FamosFileDataType.UInt48}' is not supported."),
                                                        _ => throw new NotSupportedException($"The specified data type '{currentComponent.PackInfo.DataType}' is not supported.")
                };

                componentsData.Add(componentData);
                cache[currentComponent] = componentData;
            }

            return new FamosFileChannelData(component.Name, foundField.Type, componentsData);
        }

        /// <summary>
        /// Reads the full datasets of all provided channels.
        /// </summary>
        /// <param name="channels">The list of channels that the descibe the data to read</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> for each channel.</returns>
        public List<FamosFileChannelData> ReadGroup(List<FamosFileChannel> channels)
        {
            return channels.Select(channel => this.ReadSingle(channel)).ToList();
        }

        /// <summary>
        /// Reads the full datasets of all channels in the file.
        /// </summary>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> for each channel.</returns>
        public List<FamosFileChannelData> ReadAll()
        {
            return this.GetItemsByComponents(component => component.Channels).Select(channel => this.ReadSingle(channel)).ToList();
        }

        private byte[] ReadComponentData(FamosFileComponent component, int start, int length)
        {
            var packInfo = component.PackInfo;
            var buffer = packInfo.Buffers.First();
            var fileOffset = buffer.RawBlock.FileReadOffset + buffer.RawBlockOffset + buffer.Offset + packInfo.Offset;

            var valueLength = component.GetSize(start, length);
            var dataByteLength = valueLength * packInfo.ValueSize;

            // read all data at once
            if (packInfo.IsContiguous)
            {
                var valueOffset = start * packInfo.ValueSize;

                this.Reader.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);
                return this.Reader.ReadBytes(dataByteLength);
            }

            // read grouped data
            else
            {
                var bufferByteLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset;
                var valueOffset = start * packInfo.ByteRowSize;

                var data = new byte[dataByteLength];

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
                    if (bufferByteLength - bytePosition >= packInfo.ByteGapSize)
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
                    this.OriginInfo = new FamosFileOriginInfo(this.Reader, this.CodePage);

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
                    this.RawBlocks.Add(new FamosFileRawBlock(this.Reader));

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

            // check if raw block indices are consistent
            base.CheckIndexConsistency("raw block", this.RawBlocks, current => current.Index);
            this.RawBlocks = this.RawBlocks.OrderBy(x => x.Index).ToList();

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

            // assign raw block to buffer
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                buffer.RawBlock = this.RawBlocks.First(rawBlock => rawBlock.Index == buffer.RawBlockIndex);
            }
        }

        #endregion
    }
}
