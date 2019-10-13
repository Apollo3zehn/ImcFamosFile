using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileText : FamosFileBaseExtended
    {
        #region Fields

        private int _groupIndex;

        #endregion

        #region Constructors

        public FamosFileText(string text)
        {
            this.Text = text;
            this.Version = 1;
        }

        public FamosFileText(List<string> texts)
        {
            foreach (var text in texts)
            {
                if (text.Length > int.MaxValue - 1) // = 2^31 - 2
                    throw new FormatException("The text exceeds the maximum length of 2^31 - 2");
            }

            this.Texts.AddRange(texts);
            this.Version = 2;
        }

        internal FamosFileText(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            var keyVersion = this.DeserializeInt32();

            if (keyVersion == 1)
            {
                this.DeserializeKey(keySize =>
                {
                    this.GroupIndex = this.DeserializeInt32();

                    this.Name = this.DeserializeString();
                    this.Text = this.DeserializeString();
                    this.Comment = this.DeserializeString();
                });
            }
            else if (keyVersion == 2)
            {
                this.DeserializeKey(keySize =>
                {
                    this.GroupIndex = this.DeserializeInt32();

                    this.Name = this.DeserializeString();
                    this.Texts.AddRange(this.DeserializeStringArray());
                    this.Comment = this.DeserializeString();
                });
            }
            else
            {
                throw new FormatException($"Expected key version '1' or '2', got '{keyVersion}'.");
            }
        }

        #endregion

        #region Properties

        public string Name { get; set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<string> Texts { get; } = new List<string>();

        internal int GroupIndex
        {
            get { return _groupIndex; }
            set
            {
                if (value < 0)
                    throw new FormatException($"Expected group index >= '0', got '{value}'.");

                _groupIndex = value;
            }
        }

        protected int Version { get; }
        protected override FamosFileKeyType KeyType => FamosFileKeyType.CT;

        #endregion

        #region Serialization

        internal override void Serialize(StreamWriter writer)
        {
            var data = new List<object>
            {
                this.GroupIndex,
                this.Name.Length, this.Name,
            };

            if (this.Version == 1)
            {
                data.Add(this.Text.Length);
                data.Add(this.Text);
            }
            else
            {
                data.Add(this.Texts.Count);

                foreach (var text in this.Texts)
                {
                    data.Add(text.Length);
                    data.Add(text);
                }
            }

            data.Add(this.Comment.Length);
            data.Add(this.Comment);

            this.SerializeKey(writer, this.Version, data.ToArray());
        }

        #endregion
    }
}
