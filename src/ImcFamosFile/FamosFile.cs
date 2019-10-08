using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImcFamosFile
{
    public class FamosFile : FamosFileBaseExtended
    {
        #region Fields

        private const int SUPPORTED_VERSION = 2;

        private int _processor;

        private List<FamosFileText> _texts = new List<FamosFileText>();
        private List<FamosFileSingleValue> _singleValues = new List<FamosFileSingleValue>();

        #endregion

        #region Constructors

        public FamosFile()
        {
#warning TODO: Improve SingleValue
#warning TODO: Solve issue that EndOfStreamException stops current key processing.
            //
        }

        public FamosFile(BinaryReader reader) : base(reader, 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!this.Reader.BaseStream.CanSeek)
                throw new NotSupportedException("The underlying stream must be seekable.");

            this.Deserialize();
            this.Prepare();
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

        public void Save(string filePath, int codePage = 0)
        {
            this.Validate();

#warning TODO: implement Save()
        }

        private void AssignToGroup<T>(int groupIndex, T value, Func<FamosFileGroup, List<T>> getGroupCollection, Func<List<T>>? getDefaultCollection = null)
        {
            var group = this.Groups.FirstOrDefault(group => group.Index == groupIndex);

            if (group is null)
            {
                if (getDefaultCollection is null)
                {
                    throw new FormatException($"The requested group with index '{groupIndex}' does not exist.");
                }
                else
                {
                    getDefaultCollection.Invoke().Add(value);
                    return;
                }
            }

            getGroupCollection.Invoke(group).Add(value);
        }

        private void Deserialize()
        {
            // CF - Format version and processor type.
            this.DeserializeKey(FamosFileKeyType.CF, expectedKeyVersion: SUPPORTED_VERSION, keySize =>
            {
                var processor = this.DeserializeInt32();
                this.Processor = processor;
            });

            // CK - Starts a group of keys.
            this.DeserializeKey(FamosFileKeyType.CK, expectedKeyVersion: 1, keySize =>
            {
                var unknown = this.DeserializeInt32();
                var keyGroupIsClosed = this.DeserializeInt32() == 1;

                if (!keyGroupIsClosed)
                    throw new FormatException($"The key group is not closed. This may be a hint to an interruption that occured while writing the file content to disk.");
            });

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
                        this.DataOriginInfo = new FamosFileDataOriginInfo(this.Reader, this.CodePage);

                    // NL
                    else if (nextKeyType == FamosFileKeyType.NL)
                    {
                        this.LanguageInfo = new FamosFileLanguageInfo(this.Reader);
                        this.CodePage = this.LanguageInfo.CodePage;
                    }

                    // CB
                    else if (nextKeyType == FamosFileKeyType.CB)
                        this.Groups.Add(new FamosFileGroup(this.Reader, this.CodePage));

                    // CT
                    else if (nextKeyType == FamosFileKeyType.CT)
                        _texts.Add(new FamosFileText(this.Reader, this.CodePage));

                    // CI
                    else if (nextKeyType == FamosFileKeyType.CI)
                        _singleValues.Add(new FamosFileSingleValue(this.Reader, this.CodePage));

                    // CV
                    else if (nextKeyType == FamosFileKeyType.CV)
                    {
                        this.DeserializeKey(expectedKeyVersion: 1, keySize =>
                        {
                            var eventCount = this.DeserializeInt32();

                            for (int i = 0; i < eventCount; i++)
                            {
                                var @event = new FamosFileEvent(this.Reader);
                                this.Events.Add(@event);
                            }
                        });
                    }

                    // CS 
                    else if (nextKeyType == FamosFileKeyType.CS)
                        this.RawData.Add(new FamosFileRawData(this.Reader));

                    // CG
                    else if (nextKeyType == FamosFileKeyType.CG)
                        this.DataFields.Add(new FamosFileDataField(this.Reader, this.CodePage));

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

        internal override void Prepare()
        {
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
            foreach (var channelInfo in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.ChannelInfos)))
            {
                this.AssignToGroup(channelInfo.GroupIndex, channelInfo, group => group.ChannelInfos);
            }

            // assign raw data to buffer
            foreach (var buffer in this.DataFields.SelectMany(dataField => dataField.Components.SelectMany(component => component.Buffers)))
            {
                buffer.RawData = this.RawData.FirstOrDefault(rawData => rawData.Index == buffer.RawDataReference);
            }

            // sort
            this.Groups = this.Groups.OrderBy(x => x.Index).ToList();
            this.Events = this.Events.OrderBy(x => x.Index).ToList();
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

            // check if event indices are consistent
            foreach (var @event in this.Events)
            {
                var expected = @event.Index;
                var actual = this.Events.IndexOf(@event) + 1;

                if (expected != actual)
                    throw new FormatException($"The event indices are not consistent. Expected '{expected}', got '{actual}'.");
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
                if (buffer.RawData is null)
                    throw new FormatException("The buffer's raw data must be provided.");

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
    }
}
