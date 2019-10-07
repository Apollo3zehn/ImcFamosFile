using System;

namespace ImcFamosFile
{
    public class FamosFileChannelInfo
    {
        #region Fields

        private int _groupIndex;

        #endregion

        #region Constructors

        public FamosFileChannelInfo()
        {
            this.Name = string.Empty;
            this.Comment = string.Empty;
        }

        internal FamosFileChannelInfo(int groupIndex) : this()
        {
            this.GroupIndex = groupIndex;
        }

        #endregion

        #region Properties

        internal int GroupIndex
        {
            get { return _groupIndex; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected group index > '0', got '{value}'.");

                _groupIndex = value;
            }
        }

        public int BitIndex { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }

        #endregion
    }
}
