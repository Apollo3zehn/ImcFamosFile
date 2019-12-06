using System;
using System.Collections.Generic;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains a named text or a list of texts.
    /// </summary>
    public class FamosFileText : FamosFileBaseProperty
    {
        #region Fields

        private int _groupIndex;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileText"/> class.
        /// </summary>
        /// <param name="name">The name of the text.</param>
        /// <param name="text">A single text.</param>
        public FamosFileText(string name, string text)
        {
            this.Name = name;
            this.Text = text;
            this.Version = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileText"/> class.
        /// </summary>
        /// <param name="name">The name of the text.</param>
        /// <param name="texts">A list of texts.</param>
        public FamosFileText(string name, List<string> texts)
        {
            this.Name = name;

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

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the text.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of texts.
        /// </summary>
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

        private protected int Version { get; }

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CT;

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
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
            base.Serialize(writer);
        }

        #endregion
    }
}
