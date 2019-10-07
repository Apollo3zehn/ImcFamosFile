using System;

namespace ImcFamosFile
{
    public class FamosFileRawData
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        public FamosFileRawData()
        {
            //
        }

        internal FamosFileRawData(int index)
        {
            this.Index = index;
        }

        #endregion

        #region Properties

        internal int Index
        {
            get { return _index; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected index > '0', got '{value}'.");

                _index = value;
            }
        }

        public long Length { get; set; }
        public long FileOffset { get; set; }

        #endregion
    }
}
