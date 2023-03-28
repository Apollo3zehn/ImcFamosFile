using System.Text;

namespace ImcFamosFile
{
    /// <summary>
    /// Represents an imc FAMOS file in a hierachical structure containing texts, single values, channels and more. It is capable of reading file data.
    /// </summary>
    public class FamosFile : FamosFileHeader, IDisposable
    {
        #region Fields

        private readonly List<FamosFileText> _texts = new();
        private readonly List<FamosFileSingleValue> _singleValues = new();

        #endregion

        #region Constructors

        private FamosFile(string filePath) 
            : this(filePath, FileMode.Open, FileAccess.Read)
        {
            //
        }

        private FamosFile(string filePath, FileMode fileMode, FileAccess fileAccess) 
            : base(new BinaryReader(File.Open(filePath, fileMode, fileAccess)))
        {
            try
            {
                Deserialize();
                AfterDeserialize();
                Validate();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private FamosFile(Stream stream) 
            : base(new BinaryReader(stream))
        {
            stream.Seek(0, SeekOrigin.Begin);

            Deserialize();
            AfterDeserialize();
            Validate();
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Closes the file stream. Has the same effect as <see cref="FamosFile.Dispose"/>.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Releases the resources used by <see cref="FamosFile"/>. Has the same effect as <see cref="FamosFile.Close"/>.
        /// </summary>
        public void Dispose()
        {
            Reader?.Dispose();
        }

        #endregion

        #region Deserialization

        /// <summary>
        /// Opens the file at the location specified by the parameter <paramref name="filePath"/> in read-only mode.
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
        /// Opens the file at the location specified by the parameter <paramref name="filePath"/>. Use this method in conjunction with <see cref="FamosFile.Edit(Action{BinaryWriter})"/> to edit the file's raw data without touching the header itself.
        /// </summary>
        /// <param name="filePath">The path to the file to be opened.</param>
        /// <returns>Returns a new <see cref="FamosFile"/> instance.</returns>
        public static FamosFile OpenEditable(string filePath)
        {
            return new FamosFile(filePath, FileMode.Open, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Writes the provided data to the currently opened file. Use this only in conjunction with <see cref="FamosFile.OpenEditable(string)"/> and without modification of the returned <see cref="FamosFile"/> instance to ensure proper buffer alignment.
        /// </summary>
        /// <param name="writeData"></param>
        public void Edit(Action<BinaryWriter> writeData)
        {
            var stream = Reader.BaseStream;

            if (!stream.CanWrite || !stream.CanSeek)
                throw new InvalidOperationException("The stream must be writeable and seekable.");

            var codePage = LanguageInfo is null ? 0 : LanguageInfo.CodePage;
            var encoding = Encoding.GetEncoding(codePage);

            using var writer = new BinaryWriter(stream, encoding, leaveOpen: true);
            writeData.Invoke(writer);
        }

        /// <summary>
        /// Reads the full length dataset associated to the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel that describes the data to read.</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> instance, which may consists of more than one dataset.</returns>
        public FamosFileChannelData ReadSingle(FamosFileChannel channel)
        {
            return ReadSingle(channel, 0, 0);
        }

        /// <summary>
        /// Reads a partial dataset associated to the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel that describes the data to read.</param>
        /// <param name="start">The reading start offset.</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> instance, which may consists of more than one dataset.</returns>
        public FamosFileChannelData ReadSingle(FamosFileChannel channel, int start)
        {
            return ReadSingle(channel, start, 0);
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
            (var foundField, var foundComponent) = FindFieldAndComponent(channel);

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

                var data = ReadComponentData(currentComponent, start, length);

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

            return new FamosFileChannelData(foundComponent.Name, foundField.Type, componentsData);
        }

        /// <summary>
        /// Reads the full length datasets of all provided channels.
        /// </summary>
        /// <param name="channels">The list of channels that the descibe the data to read</param>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> for each channel.</returns>
        public List<FamosFileChannelData> ReadGroup(List<FamosFileChannel> channels)
        {
            return channels.Select(channel => ReadSingle(channel)).ToList();
        }

        /// <summary>
        /// Reads the full length datasets of all channels in the file.
        /// </summary>
        /// <returns>Returns a <see cref="FamosFileChannelData"/> for each channel.</returns>
        public List<FamosFileChannelData> ReadAll()
        {
            return GetItemsByComponents(component => component.Channels).Select(channel => ReadSingle(channel)).ToList();
        }

        private byte[] ReadComponentData(FamosFileComponent component, int start, int length)
        {
            var packInfo = component.PackInfo;
            var buffer = packInfo.Buffers.First();
            var fileOffset = buffer.RawBlock.FileOffset + buffer.RawBlockOffset + buffer.Offset + packInfo.Offset;

            var valueLength = component.GetSize(start, length);
            var dataByteLength = valueLength * packInfo.ValueSize;

            // read all data at once
            if (packInfo.IsContiguous)
            {
                var valueOffset = start * packInfo.ValueSize;

                Reader.BaseStream.TrySeek(fileOffset + valueOffset, SeekOrigin.Begin);
                return Reader.ReadBytes(dataByteLength);
            }

            // read grouped data
            else
            {
                var valueOffset = start * packInfo.ByteRowSize;
                var bufferByteLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset - valueOffset;

                var data = new byte[dataByteLength];

                Reader.BaseStream.TrySeek(fileOffset + valueOffset, SeekOrigin.Begin);

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
                                data[position] = Reader.ReadByte();
                            }

                            bytePosition += packInfo.ValueSize;
                            valuePosition += 1;
                        }
                    }

                    // skip x bytes
                    if (bufferByteLength - bytePosition >= packInfo.ByteGapSize)
                    {
                        Reader.BaseStream.TrySeek(packInfo.ByteGapSize, SeekOrigin.Current);
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
            var keyType = DeserializeKeyType();

            if (keyType != FamosFileKeyType.CF)
                throw new FormatException("The file is not a FAMOS file.");

            var keyVersion = DeserializeInt32();

            if (keyVersion == 1)
            {
                throw new FormatException($"Only files of format version '2' can be read.");
            }
            else if (keyVersion == SUPPORTED_VERSION)
            {
                DeserializeKey(keySize =>
                {
                    var processor = DeserializeInt32();
                });
            }
            else
            {
                throw new FormatException($"Expected key version '2', got '{keyVersion}'.");
            }

            // CK
            new FamosFileKeyGroup(Reader);

            // Now, all other keys.
            FamosFileBaseProperty? propertyInfoReceiver = null;

            while (true)
            {
                if (Reader.BaseStream.Position >= Reader.BaseStream.Length)
                    return;

                var nextKeyType = DeserializeKeyType();

                // Reset propertyInfoReceiver if next key type is not 'Np'.
                if (nextKeyType != FamosFileKeyType.Np)
                    propertyInfoReceiver = null;

                // Unknown
                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    SkipKey();
                    continue;
                }

                // NO
                else if (nextKeyType == FamosFileKeyType.NO)
                    OriginInfo = new FamosFileOriginInfo(Reader, CodePage);

                // NL
                else if (nextKeyType == FamosFileKeyType.NL)
                {
                    LanguageInfo = new FamosFileLanguageInfo(Reader);
                    CodePage = LanguageInfo.CodePage;
                }

                // NE - only imc internal
                else if (nextKeyType == FamosFileKeyType.NE)
                    SkipKey();

                // Ca
                else if (nextKeyType == FamosFileKeyType.Ca)
                    throw new FormatException("The functionality of the 'Ca'-key is not supported. Please submit a sample .dat or .raw file to the package author to find a solution.");

                // NU
                else if (nextKeyType == FamosFileKeyType.NU)
                    CustomKeys.Add(new FamosFileCustomKey(Reader, CodePage));

                // CB
                else if (nextKeyType == FamosFileKeyType.CB)
                {
                    Groups.Add(new FamosFileGroup(Reader, CodePage));
                    propertyInfoReceiver = Groups.Last();
                }

                // CG
                else if (nextKeyType == FamosFileKeyType.CG)
                    Fields.Add(new FamosFileField(Reader, CodePage));

                // CT
                else if (nextKeyType == FamosFileKeyType.CT)
                {
                    _texts.Add(new FamosFileText(Reader, CodePage));
                    propertyInfoReceiver = _texts.Last();
                }

                // CI
                else if (nextKeyType == FamosFileKeyType.CI)
                {
                    _singleValues.Add(new FamosFileSingleValue.Deserializer(Reader, CodePage).Deserialize());
                    propertyInfoReceiver = _singleValues.Last();
                }

                // CS 
                else if (nextKeyType == FamosFileKeyType.CS)
                    RawBlocks.Add(new FamosFileRawBlock(Reader));

                // Nv - only for data manager
                else if (nextKeyType == FamosFileKeyType.Nv)
                    SkipKey();

                // Np
                else if (nextKeyType == FamosFileKeyType.Np)
                {
                    var propertyInfo = new FamosFilePropertyInfo(Reader, CodePage);

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
                    SkipKey();
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
                var group = Groups.FirstOrDefault(group => group.Index == groupIndex);

                if (group != null)
                    getGroupCollection.Invoke(group).Add(value);
                else
                    throw new FormatException("The referenced group does not exist.");
            }
        }

        internal override void AfterDeserialize()
        {
            // prepare data fields
            foreach (var field in Fields)
            {
                field.AfterDeserialize();
            }

            // check if group indices are consistent
            base.CheckIndexConsistency("group", Groups, current => current.Index);
            Groups = Groups.OrderBy(x => x.Index).ToList();

            // check if raw block indices are consistent
            base.CheckIndexConsistency("raw block", RawBlocks, current => current.Index);
            RawBlocks = RawBlocks.OrderBy(x => x.Index).ToList();

            // assign text to group
            foreach (var text in _texts)
            {
                AssignToGroup(text.GroupIndex, text, group => group.Texts, () => Texts);
            }

            // assign single value to group
            foreach (var singleValue in _singleValues)
            {
                AssignToGroup(singleValue.GroupIndex, singleValue, group => group.SingleValues, () => SingleValues);
            }

            // assign channel info to group
            foreach (var channel in GetItemsByComponents(component => component.Channels))
            {
                AssignToGroup(channel.GroupIndex, channel, group => group.Channels, () => Channels);
            }

            // assign raw block to buffer
            foreach (var buffer in GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                buffer.RawBlock = RawBlocks.First(rawBlock => rawBlock.Index == buffer.RawBlockIndex);
            }
        }

        #endregion
    }
}
