using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ImcFamosFile
{
    /// <summary>
    /// Represents an imc FAMOS file in a hierachical structure containing texts, single values, channels and more.
    /// </summary>
    public class FamosFileHeader : FamosFileBaseExtended
    {
        #region Fields

        private protected const int SUPPORTED_VERSION = 2;

        private int _processor = 1;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileHeader"/> class.
        /// </summary>
        public FamosFileHeader()
        {
            this.Initialize();
        }

        private protected FamosFileHeader(BinaryReader reader) : base(reader, 0)
        {
            this.Initialize();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the language info containing data about the code page and language.
        /// </summary>
        public FamosFileLanguageInfo? LanguageInfo { get; set; }

        /// <summary>
        /// Gets or sets the data origin info.
        /// </summary>
        public FamosFileOriginInfo? OriginInfo { get; set; }

        /// <summary>
        /// Gets a list of texts.
        /// </summary>
        public List<FamosFileText> Texts { get; private set; } = new List<FamosFileText>();

        /// <summary>
        /// Gets a list of single values.
        /// </summary>
        public List<FamosFileSingleValue> SingleValues { get; private set; } = new List<FamosFileSingleValue>();

        /// <summary>
        /// Gets a list of channels.
        /// </summary>
        public List<FamosFileChannel> Channels { get; private set; } = new List<FamosFileChannel>();

        /// <summary>
        /// Gets a list of custom keys which must be unique globally.
        /// </summary>
        public List<FamosFileCustomKey> CustomKeys { get; private set; } = new List<FamosFileCustomKey>();

        /// <summary>
        /// Gets a list of groups.
        /// </summary>
        public List<FamosFileGroup> Groups { get; internal set; } = new List<FamosFileGroup>();

        /// <summary>
        /// Gets a list of fields.
        /// </summary>
        public List<FamosFileField> Fields { get; private set; } = new List<FamosFileField>();

        /// <summary>
        /// Gets a list of raw data blocks.
        /// </summary>
        public List<FamosFileRawBlock> RawBlocks { get; internal set; } = new List<FamosFileRawBlock>();

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CF;

        private int Processor
        {
            get { return _processor; }
            set
            {
                if (value != 1)
                    throw new FormatException($"Expected processor value '1', got '{value}'.");

                _processor = value;
            }
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Aligns the buffers of all components of this instance to the provided <paramref name="rawBlock"/> instance. This is done automatically, if the functionality is not being disabled in the call to 'famosFile.Save(...)'.
        /// </summary>
        /// <param name="rawBlock">The raw block instance, where the actual data is stored.</param>
        /// <param name="alignmentMode">The buffer alignment mode.</param>
        public void AlignBuffers(FamosFileRawBlock rawBlock, FamosFileAlignmentMode alignmentMode)
        {
            this.AlignBuffers(rawBlock, alignmentMode, this.Fields.SelectMany(field => field.Components).ToList());
        }

        /// <summary>
        /// Aligns the buffers of all <paramref name="components"/> to the provided <paramref name="rawBlock"/> instance. This is done automatically, if the functionality is not being disabled in the call to 'famosFile.Save(...)'.
        /// </summary>
        /// <param name="rawBlock">The raw block instance, where the actual data is stored.</param>
        /// <param name="alignmentMode">The buffer alignment mode.</param>
        /// <param name="components">The list of components to align.</param>
        public void AlignBuffers(FamosFileRawBlock rawBlock, FamosFileAlignmentMode alignmentMode, List<FamosFileComponent> components)
        {
            var actualComponents = this.Fields.SelectMany(field => field.Components);

            if (!this.RawBlocks.Contains(rawBlock))
                throw new InvalidOperationException("The passed raw block instance is not a member of this instance.");

            if (!components.Any(component => actualComponents.Contains(component)))
                throw new InvalidOperationException("One or more passed component instances are not a member of this instance.");

            switch (alignmentMode)
            {
                case FamosFileAlignmentMode.Continuous:

                    var offset = 0;

                    foreach (var component in components)
                    {
                        var packInfo = component.PackInfo;
                        var buffer = packInfo.Buffers.First();

                        packInfo.GroupSize = 1;
                        packInfo.ByteGapSize = 0;

                        buffer.RawBlock = rawBlock;
                        buffer.RawBlockOffset = offset;

                        offset += buffer.Length;
                    }

                    rawBlock.Length = offset;

                    break;

                case FamosFileAlignmentMode.Interlaced:

                    if (components.Any())
                    {
                        var commonSize = components.First().GetSize();

                        if (components.Skip(1).Any(component => component.GetSize() != commonSize))
                            throw new InvalidOperationException("In interlaced mode, all components must be of the same size.");
                    }
                    else
                    {
                        return;
                    }

                    var valueSizes = components.Select(component => component.PackInfo.ValueSize);
                    var totalSize = valueSizes.Sum();
                    var currentOffset = 0;
                    var rawBlockLength = 0L;

                    foreach (var component in components)
                    {
                        var packInfo = component.PackInfo;
                        var buffer = packInfo.Buffers.First();

                        packInfo.Offset = currentOffset;
                        packInfo.GroupSize = 1;
                        packInfo.ByteGapSize = totalSize - packInfo.ValueSize;

                        buffer.RawBlock = rawBlock;
                        buffer.RawBlockOffset = 0;

                        currentOffset += packInfo.ValueSize;
                        rawBlockLength += buffer.Length;
                    }

                    if (rawBlockLength > 2 * Math.Pow(10, 9))
                        throw new InvalidOperationException("In interlaced mode, all buffers are combined into a single large buffer. This buffer would exceed the maximum allowed length of '2 * 10^9' bytes.");

                    foreach (var component in components)
                    {
                        var buffer = component.PackInfo.Buffers.First();
                        buffer.Length = (int)rawBlockLength;
                        buffer.ConsumedBytes = (int)rawBlockLength;
                    }

                    rawBlock.Length = rawBlockLength;

                    break;

                default:
                    throw new InvalidOperationException($"The alignment mode '{alignmentMode}' parameter is unknown.");
            }
        }

        /// <summary>
        /// Finds the parent field and component of the provided channel.
        /// </summary>
        /// <param name="channel">The channel that belongs to the component and field to be searched for.</param>
        /// <returns>Returns the found <see cref="FamosFileField"/> and <see cref="FamosFileComponent"/>.</returns>
        public (FamosFileField Field, FamosFileComponent Component) FindFieldAndComponent(FamosFileChannel channel)
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

            return (foundField, foundComponent);
        }

        /// <summary>
        /// Finds the parent field of the provided channel.
        /// </summary>
        /// <param name="channel">The channel that belongs to the component to be searched for.</param>
        /// <returns>Returns the found <see cref="FamosFileField"/>.</returns>
        public FamosFileField FindField(FamosFileChannel channel)
        {
            return this.FindFieldAndComponent(channel).Field;
        }

        /// <summary>
        /// Finds the parent component of the provided channel.
        /// </summary>
        /// <param name="channel">The channel that belongs to the component to be searched for.</param>
        /// <returns>Returns the found <see cref="FamosFileComponent"/>.</returns>
        public FamosFileComponent FindComponent(FamosFileChannel channel)
        {
            return this.FindFieldAndComponent(channel).Component;
        }

        /// <inheritdoc />
        public override void Validate()
        {
            // validate data fields
            foreach (var field in this.Fields)
            {
                field.Validate();
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

            // check if buffer's raw block is part of this instance
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                if (!this.RawBlocks.Contains(buffer.RawBlock))
                {
                    throw new FormatException("The buffers' raw block must be part of the famos file's raw block collection.");
                };
            }

            /* check unique region */
            if (this.Groups.Count != this.Groups.Distinct().Count())
                throw new FormatException("A group must be added only once.");

            if (this.Fields.Count != this.Fields.Distinct().Count())
                throw new FormatException("A field must be added only once.");

            if (this.CustomKeys.Count != this.CustomKeys.Distinct().Count())
                throw new FormatException("A custom key must be added only once.");

            if (this.RawBlocks.Count != this.RawBlocks.Distinct().Count())
                throw new FormatException("A raw block must be added only once.");

            /* not yet supported region */
            foreach (var rawBlock in this.RawBlocks)
            {
                if (rawBlock.CompressionType != FamosFileCompressionType.Uncompressed)
                    throw new InvalidOperationException("This implementation does not support processing compressed data yet. Please send a sample file to the package author to find a solution.");
            }
        }

        private protected List<T> GetItemsByGroups<T>(Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>> getDefaultCollection)
        {
            return getDefaultCollection().Concat(this.Groups.SelectMany(group => getGroupCollection(group))).ToList();
        }

        private protected List<T> GetItemsByComponents<T>(Func<FamosFileComponent, List<T>> getComponentCollection)
        {
            return this.Fields.SelectMany(field => field.Components.SelectMany(component => getComponentCollection(component))).ToList();
        }

        private protected List<T> GetItemsByComponents<T>(Func<FamosFileComponent, T> getComponentValue)
        {
            return this.Fields.SelectMany(field => field.Components.Select(component => getComponentValue(component))).ToList();
        }

        private void Initialize()
        {
            if (!BitConverter.IsLittleEndian)
                throw new NotSupportedException("Only little-endian systems are supported.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes the current instance to disk.
        /// </summary>
        /// <param name="filePath">The path of the file on disk.</param>
        /// <param name="writeData">The action that is being called (when the file header has been written) to now write the actual data.</param>
        /// <param name="autoAlign">A boolean indicating if all component buffers and pack infos should be aligned automatically.</param>
        public void Save(string filePath, Action<BinaryWriter> writeData, bool autoAlign = true)
        {
            this.Save(filePath, FileMode.CreateNew, writeData, autoAlign);
        }

        /// <summary>
        /// Serializes the current instance to disk.
        /// </summary>
        /// <param name="filePath">The path of the file on disk.</param>
        /// <param name="fileMode">Specified the mode with which the file should be opened.</param>
        /// <param name="writeData">The action that is being called (when the file header has been written) to now write the actual data.</param>
        /// <param name="autoAlign">A boolean indicating if all component buffers and pack infos should be aligned automatically.</param>
        public void Save(string filePath, FileMode fileMode, Action<BinaryWriter> writeData, bool autoAlign = true)
        {
            using (var stream = File.Open(filePath, fileMode, FileAccess.Write))
            {
                this.Save(stream, writeData, autoAlign);
            }
        }

        /// <summary>
        /// Serializes the current instance to the passed <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream where the data will be written to.</param>
        /// <param name="writeData">The action that is being called (when the file header has been written) to now write the actual data.</param>
        /// <param name="autoAlign">A boolean indicating if all component buffers and pack infos should be aligned automatically.</param>
        public void Save(Stream stream, Action<BinaryWriter> writeData, bool autoAlign = true)
        {
            if (!stream.CanWrite || !stream.CanSeek)
                throw new InvalidOperationException("The stream must be writeable and seekable.");

            if (autoAlign)
            {
                var rawBlock = new FamosFileRawBlock();

                this.RawBlocks.Clear();
                this.RawBlocks.Add(rawBlock);

                this.AlignBuffers(rawBlock, FamosFileAlignmentMode.Continuous);
            }

            this.Validate();
            this.BeforeSerialize();

            var codePage = this.LanguageInfo is null ? 1252 : this.LanguageInfo.CodePage;
            var encoding = Encoding.GetEncoding(codePage);

            using (var writer = new BinaryWriter(stream, encoding, leaveOpen: true))
            {
                // Serialize header and leave CK key open.
                this.Serialize(writer);

                // Serialize data.
                writeData.Invoke(writer);

                // Close CK.
                writer.BaseStream.Seek(20, SeekOrigin.Begin);
                writer.Write('1');
            }

            this.AfterSerialize();
        }

        /// <summary>
        /// This method should be called in the action block that has been passed to the 'famosFile.Save(...)' or <see cref="FamosFile.Edit(Action{BinaryWriter})"/> method to write the actual data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The data type parameter.</typeparam>
        /// <param name="writer">The binary writer that has been provided in the action block.</param>
        /// <param name="component">The component that describes the data to write.</param>
        /// <param name="data">The actual data of type <typeparamref name="T"/>.</param>
        public void WriteSingle<T>(BinaryWriter writer, FamosFileComponent component, T[] data) where T : unmanaged
        {
            this.WriteSingle<T>(writer, component, 0, data.AsSpan());
        }

        /// <summary>
        /// This method should be called in the action block that has been passed to the 'famosFile.Save(...)' or <see cref="FamosFile.Edit(Action{BinaryWriter})"/> method to write the actual data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The data type parameter.</typeparam>
        /// <param name="writer">The binary writer that has been provided in the action block.</param>
        /// <param name="component">The component that describes the data to write.</param>
        /// <param name="data">The actual data of type <typeparamref name="T"/>.</param>
        public void WriteSingle<T>(BinaryWriter writer, FamosFileComponent component, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.WriteSingle(writer, component, 0, data);
        }

        /// <summary>
        /// This method should be called in the action block that has been passed to the 'famosFile.Save(...)' or <see cref="FamosFile.Edit(Action{BinaryWriter})"/> method to write the actual data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The data type parameter.</typeparam>
        /// <param name="writer">The binary writer that has been provided in the action block.</param>
        /// <param name="component">The component that describes the data to write.</param>
        /// <param name="start">The number of values to skip.</param>
        /// <param name="data">The actual data of type <typeparamref name="T"/>.</param>
        public void WriteSingle<T>(BinaryWriter writer, FamosFileComponent component, int start, T[] data) where T : unmanaged
        {
            this.WriteSingle<T>(writer, component, start, data.AsSpan());
        }

        /// <summary>
        /// This method should be called in the action block that has been passed to the 'famosFile.Save(...)' or <see cref="FamosFile.Edit(Action{BinaryWriter})"/> method to write the actual data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The data type parameter.</typeparam>
        /// <param name="writer">The binary writer that has been provided in the action block.</param>
        /// <param name="component">The component that describes the data to write.</param>
        /// <param name="start">The number of values to skip.</param>
        /// <param name="data">The actual data of type <typeparamref name="T"/>.</param>
        public void WriteSingle<T>(BinaryWriter writer, FamosFileComponent component, int start, ReadOnlySpan<T> data) where T : unmanaged
        {
            if (!this.Fields.Any(field => field.Components.Contains(component)))
                throw new FormatException($"The provided component is not part of any {nameof(FamosFileField)} instance.");

            var dataByteLength = data.Length * Marshal.SizeOf<T>();

            var bufferValueLength = component.GetSize(start);
            var bufferByteLength = bufferValueLength * component.PackInfo.ValueSize;

            // data:     [0000] [0000] [0000] [0000] [0000]
            // buffer:   [0000] [0000] [0000] [0000]
            if (dataByteLength > bufferByteLength)
                throw new InvalidOperationException("The start offset plus the size of the provided data array exceed the size of the component's buffer.");

            // data:     [0000] [0000] [000]
            // buffer:   [0000] [0000] [0000] [0000]
            if (dataByteLength % component.PackInfo.ValueSize != 0)
                throw new Exception("The length of the provided data array is not aligned to the component's buffer length, i.e. an incomplete value of the component's data type would be written to file.");

            this.WriteComponentData(writer, component, start, MemoryMarshal.Cast<T, byte>(data), dataByteLength / component.PackInfo.ValueSize);
        }

        private void WriteComponentData(BinaryWriter writer, FamosFileComponent component, int start, ReadOnlySpan<byte> data, int length)
        {
            var packInfo = component.PackInfo;
            var buffer = packInfo.Buffers.First();
            var fileOffset = buffer.RawBlock.FileOffset + buffer.RawBlockOffset + buffer.Offset + packInfo.Offset;

            var valueLength = component.GetSize(start, length);

            // write all data at once
            if (packInfo.IsContiguous)
            {
                var valueOffset = start * packInfo.ValueSize;

                writer.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);
                writer.Write(data);
            }

            // write grouped data
            else
            {
                var bufferByteLength = buffer.ConsumedBytes - buffer.Offset - packInfo.Offset;
                var valueOffset = start * packInfo.ByteRowSize;

                writer.BaseStream.Seek(fileOffset + valueOffset, SeekOrigin.Begin);

                var bytePosition = 0;
                var valuePosition = 0;

                while (true)
                {
                    // write x subsequent values
                    for (int j = 0; j < packInfo.GroupSize; j++)
                    {
                        // write a single value
                        if (valueLength - valuePosition >= 1)
                        {
                            for (int k = 0; k < packInfo.ValueSize; k++)
                            {
                                var position = valuePosition * packInfo.ValueSize + k;
                                writer.Write(data[position]);
                            }

                            bytePosition += packInfo.ValueSize;
                            valuePosition += 1;
                        }
                    }

                    // skip x bytes
                    if (bufferByteLength - bytePosition >= packInfo.ByteGapSize)
                    {
                        writer.BaseStream.Seek(packInfo.ByteGapSize, SeekOrigin.Current);
                        bytePosition += packInfo.ByteGapSize;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        internal override void BeforeSerialize()
        {
            // prepare data fields
            foreach (var field in this.Fields)
            {
                field.BeforeSerialize();
            }

            // update raw block indices
            foreach (var rawBlock in this.RawBlocks)
            {
                rawBlock.Index = this.RawBlocks.IndexOf(rawBlock) + 1;
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

            // update raw block index of buffers
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                var rawBlockIndex = this.RawBlocks.IndexOf(buffer.RawBlock) + 1;
                buffer.RawBlockIndex = rawBlockIndex;
            }

            // assign monotonous increasing buffer references to pack infos.
            var i = 1;

            foreach (var packInfo in this.GetItemsByComponents(component => component.PackInfo))
            {
                packInfo.BufferReference = i;

                foreach (var buffer in packInfo.Buffers)
                {
                    buffer.Reference = i;
                }

                i++;
            }
        }

        internal override void AfterSerialize()
        {
            // special case: revert combination of component properties
            foreach (var field in this.Fields)
            {
                field.AfterSerialize();
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
            this.OriginInfo?.Serialize(writer);

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
            foreach (var field in this.Fields)
            {
                field.Serialize(writer);
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
            foreach (var rawBlock in this.RawBlocks)
            {
                rawBlock.Serialize(writer);
                writer.Flush();
            }
        }

        #endregion
    }
}
