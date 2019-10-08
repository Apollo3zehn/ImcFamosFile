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

        internal int GroupIndex
        {
            get { return _groupIndex; }
            private set
            {
                if (value < 0)
                    throw new FormatException($"Expected group index >= '0', got '{value}'.");

                _groupIndex = value;
            }
        }

        protected int Version { get; }

        public string Name { get; set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<string> Texts { get; } = new List<string>();

        #endregion
    }
}
