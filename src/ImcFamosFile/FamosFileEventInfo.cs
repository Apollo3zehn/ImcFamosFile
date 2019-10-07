using System;

namespace ImcFamosFile
{
    public class FamosFileEventInfo
    {
        #region Fields

        private int _offset;
        private int _groupSize;
        private int _gapSize;
        private int _eventCount;

        #endregion

        #region Properties

        public int FirstEventIndex { get; set; }

        public int Offset
        {
            get { return _offset; }
            private set
            {
                if (value < 0)
                    throw new FormatException($"Expected offset >= '0', got '{value}'.");

                _offset = value;
            }
        }

        public int GroupSize
        {
            get { return _groupSize; }
            private set
            {
                if (value <= 0)
                    throw new FormatException($"Expected group size >= '1', got '{value}'.");

                _groupSize = value;
            }
        }

        public int GapSize
        {
            get { return _gapSize; }
            private set
            {
                if (!(0 <= value && value <= 1000))
                    throw new FormatException($"Expected gap size '0..1000', got '{value}'.");

                _gapSize = value;
            }
        }

        public int EventCount
        {
            get { return _eventCount; }
            private set
            {
                if (value < 0)
                    throw new FormatException($"Expected offset >= '0', got '{value}'.");

                _eventCount = value;
            }
        }

        public FamosFileValidNTType ValidNT { get; set; }
        public FamosFileValidCDType ValidCD { get; set; }
        public FamosFileValidCR1Type ValidCR1 { get; set; }
        public FamosFileValidCR2Type ValidCR2 { get; set; }

        #endregion
    }
}
