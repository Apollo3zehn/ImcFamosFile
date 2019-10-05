using System.Collections.Generic;

namespace ImcFamosFile
{
    public class FamosFileText
    {
        public FamosFileText(List<string> texts) : base()
        {
            this.Texts.AddRange(texts);
        }

        public FamosFileText()
        {
            this.Name = string.Empty;
            this.Text = string.Empty;
            this.Comment = string.Empty;
            this.Texts = new List<string>();
        }

        public string Name { get; set; }
        public string Text { get; set; }
        public string Comment { get; set; }
        public List<string> Texts { get; private set; }
    }
}
