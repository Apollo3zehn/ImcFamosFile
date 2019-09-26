using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FamosFile.NET
{
    public class FamosFile : FamosFileBase, IDisposable
    {
#warning TODO: CV

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
            this.Groups2 = new List<FamosFileGroup2>();

            this.ParseFile();
        }

        #endregion

        #region Properties

        public int FormatVersion { get; set; }
        public int Processor { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Language { get; set; }
        public FamosFileDataOrigin DataOrigin { get; set; }
        public List<FamosFileGroup> Groups { get; set; }
        public List<FamosFileGroup2> Groups2 { get; set; }

        #endregion

        #region "Methods"

        /// <summary>
        /// Closes and disposes the FamosFile reader and the underlying file stream.
        /// </summary>
        public void Dispose()
        {
            this.Reader.Dispose();
        }

        private FamosFileGroup GetOrCreateGroup(int id)
        {
            var group = this.Groups.FirstOrDefault(current => current.Index == id);

            if (group == null)
                this.Groups.Add(new FamosFileGroup(id));

            return group;
        }

        #endregion

        #region KeyParsing

        // parse file
        private void ParseFile()
        {
            // CF
            this.ParseCF();

            // CK
            this.ParseCK();

            //
            this.DataOrigin = FamosFileDataOrigin.Unknown;
            this.Name = string.Empty;
            this.Comment = string.Empty;

            var nextKeyType = FamosFileKeyType.Unknown;
            var parseKey = true;

            while (true)
            {
                // if key has not been parsed yet
                if (parseKey)
                {
                    nextKeyType = this.ParseKeyType();
                    parseKey = true;
                }

                // Unknown
                if (nextKeyType == FamosFileKeyType.Unknown)
                {
                    this.SkipKey();
                    continue;
                }

                // NO
                else if (nextKeyType == FamosFileKeyType.NO)
                    this.ParseNO();

                // NL 
                else if (nextKeyType == FamosFileKeyType.NL)
                    this.ParseNL();

                // CB
                else if (nextKeyType == FamosFileKeyType.CB)
                    this.ParseCB();

                // CT
                else if (nextKeyType == FamosFileKeyType.CT)
                    this.ParseCT();

                // CI
                else if (nextKeyType == FamosFileKeyType.CI)
                    this.ParseCI();

                // CG
                else if (nextKeyType == FamosFileKeyType.CG)
                {
                    var group = new FamosFileGroup2(this.Reader, this.CodePage);

                    nextKeyType = group.Parse();
                    parseKey = false;

                    this.Groups2.Add(group);
                }

                else
                {
                    this.SkipKey();
                    //throw new FormatException($"Unexpected key '{keyType}'.");
                }
            }
        }

        // Format version and processor type.
        private void ParseCF()
        {
            this.ParseKey(FamosFileKeyType.CF, expectedKeyVersion: SUPPORTED_VERSION, () =>
            {
                this.FormatVersion = 2;
                this.Processor = this.ParseInteger();
            });
        }

        // Starts a group of keys.
        private void ParseCK()
        {
            this.ParseKey(FamosFileKeyType.CK, expectedKeyVersion: 1, () =>
            {
                var unknown = this.ParseInteger();
                var keyGroupIsClosed = this.ParseInteger() == 1;

                if (!keyGroupIsClosed)
                    throw new FormatException($"The key group is not closed. This may be a hint to an interruption that occured while writing the file content to disk.");
            });
        }

        // Origin of data.
        private void ParseNO()
        {
            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                this.DataOrigin = (FamosFileDataOrigin)this.ParseInteger();
                this.Name = this.ParseString();
                this.Comment = this.ParseString();
            });
        }

        // Code page.
        private void ParseNL()
        {
            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                this.CodePage = this.ParseInteger();
                this.Language = this.ParseString();
            });
        }

        // Group definition.
        private void ParseCB()
        {
            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var groupIndex = this.ParseInteger();
                var group = this.GetOrCreateGroup(groupIndex);

                group.Name = this.ParseString();
                group.Comment = this.ParseString();
            });
        }

        // Text definition.
        private void ParseCT()
        {
            var keyVersion = this.ParseInteger();

            if (keyVersion == 1)
            {
                this.ParseKey(() =>
                {
                    var groupIndex = this.ParseInteger();
                    var group = this.GetOrCreateGroup(groupIndex);

                    group.Texts.Add(new FamosFileText()
                    {
                        Name = this.ParseString(),
                        Text = this.ParseString(),
                        Comment = this.ParseString()
                    });
                });
            }
            else if (keyVersion == 2)
            {
                this.ParseKey(() =>
                {
                    var blockIndex = this.ParseInteger();
                    var group = this.GetOrCreateGroup(blockIndex);

                    var name = this.ParseString();
                    var texts = this.ParseStringArray();
                    var comment = this.ParseString();

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
        private void ParseCI()
        {
            this.ParseKey(expectedKeyVersion: 1, () =>
            {
                var groupIndex = this.ParseInteger();
                var group = this.GetOrCreateGroup(groupIndex);

                var dataType = (FamosFileDataType)this.ParseInteger();
                var name = this.ParseString();
                var value = this.ParseNumber();
                var unit = this.ParseString();
                var comment = this.ParseString();
                var time = this.ParseDouble();

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

        #endregion
    }
}
