using System;
using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileText
    {
        #region Constructors

        private FamosFileText()
        {
            this.Text = string.Empty;
            this.Name = string.Empty;
            this.Comment = string.Empty;
            this.Texts = new List<string>();
        }

        public FamosFileText(string text) : this()
        {
            this.Text = text;
            this.Version = 1;
        }

        public FamosFileText(List<string> texts) : this()
        {
            foreach (var text in texts)
            {
                if (text.Length > int.MaxValue - 1) // = 2^31 - 2
                    throw new FormatException("The text exceeds the maximum lengths of 2^31 - 2");
            }

            this.Texts.AddRange(texts);
            this.Version = 2;
        }

        #endregion

        #region Properties

        protected int Version { get; }
        public string Name { get; set; }
        public string Text { get; }
        public string Comment { get; set; }
        public List<string> Texts { get; }

        #endregion
    }
}
