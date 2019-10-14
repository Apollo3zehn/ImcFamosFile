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
            //
        }

        private FamosFile(string filePath) : base(new BinaryReader(File.OpenRead(filePath)), 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
        public List<FamosFileChannelInfo> Channels { get; private set; } = new List<FamosFileChannelInfo>();

        public List<FamosFileCustomKey> CustomKeys { get; private set; } = new List<FamosFileCustomKey>();
        public List<FamosFileGroup> Groups { get; private set; } = new List<FamosFileGroup>();
        public List<FamosFileDataField> DataFields { get; private set; } = new List<FamosFileDataField>();
        public List<FamosFileRawData> RawData { get; private set; } = new List<FamosFileRawData>();


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

            // check if buffer's raw data is part of this instance
            foreach (var buffer in this.GetItemsByComponents(component => component.BufferInfo.Buffers))
            {
                if (!this.RawData.Contains(buffer.RawData))
                {
                    throw new FormatException("The buffers' raw data must be part of the famos file's raw data collection.");
                };
            }
        }

        public void Dispose()
        {
            this.Reader.Dispose();
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
            foreach (var buffer in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.BufferInfo.Buffers)))
            {
                var rawDataIndex = this.RawData.IndexOf(buffer.RawData) + 1;
                buffer.RawDataIndex = rawDataIndex;
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

#warning TODO: Write CS key data.

            // Nv - do nothing

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

            while (true)
            {
                if (this.Reader.BaseStream.Position >= this.Reader.BaseStream.Length)
                    return;

                var nextKeyType = this.DeserializeKeyType();

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
                    this.Groups.Add(new FamosFileGroup(this.Reader, this.CodePage));

                // CG
                else if (nextKeyType == FamosFileKeyType.CG)
                    this.DataFields.Add(new FamosFileDataField(this.Reader, this.CodePage));

                // CT
                else if (nextKeyType == FamosFileKeyType.CT)
                    _texts.Add(new FamosFileText(this.Reader, this.CodePage));

                // CI
                else if (nextKeyType == FamosFileKeyType.CI)
                    _singleValues.Add(new FamosFileSingleValue(this.Reader, this.CodePage));

                // CS 
                else if (nextKeyType == FamosFileKeyType.CS)
                    this.RawData.Add(new FamosFileRawData(this.Reader));

                // Nv - only for data manager
                else if (nextKeyType == FamosFileKeyType.Nv)
                    this.SkipKey();

                else
                    //throw new FormatException($"Unexpected key '{keyType}'.");
                    this.SkipKey();
            }
        }

        private void AssignToGroup<T>(int groupIndex, T value, Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>> getDefaultCollection)
        {
            var group = this.Groups.FirstOrDefault(group => group.Index == groupIndex);

            if (group is null)
                getDefaultCollection.Invoke().Add(value);
            else
                getGroupCollection.Invoke(group).Add(value);
        }

        internal override void AfterDeserialize()
        {
            // prepare data fields
            foreach (var dataField in this.DataFields)
            {
                dataField.AfterDeserialize();
            }

            // check if group indices are consistent
            foreach (var group in this.Groups)
            {
                var expected = group.Index;
                var actual = this.Groups.IndexOf(group) + 1;

                if (expected != actual)
                    throw new FormatException($"The group indices are not consistent. Expected '{expected}', got '{actual}'.");
            }

            this.Groups = this.Groups.OrderBy(x => x.Index).ToList();

            // check if raw data indices are consistent
            foreach (var rawData in this.RawData)
            {
                var expected = rawData.Index;
                var actual = this.RawData.IndexOf(rawData) + 1;

                if (expected != actual)
                    throw new FormatException($"The raw data indices are not consistent. Expected '{expected}', got '{actual}'.");
            }

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

            // assign buffers to pack info
            var buffers = this.GetItemsByComponents(component => component.BufferInfo.Buffers);

            foreach (var packInfo in this.DataFields.SelectMany(dataField => dataField.Components.Select(component => component.PackInfo)))
            {
                packInfo.Buffers.AddRange(buffers.Where(buffer => buffer.Reference == packInfo.BufferReference));
            }

            // assign raw data to buffer
            foreach (var buffer in buffers)
            {
                buffer.RawData = this.RawData.FirstOrDefault(rawData => rawData.Index == buffer.RawDataIndex);
            }
        }

        #endregion
    }
}
