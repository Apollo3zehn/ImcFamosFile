using System;

namespace ImcFamosFile
{
    public class FamosFileDisplayInfo
    {
        #region Fields

        private int _r;
        private int _g;
        private int _b;

        #endregion

        #region Properties

        public int R
        {
            get { return _r; }
            set
            {
                if (!(0 <= value && value < 255))
                    throw new
                        FormatException($"Expected R value '0..255', got '{value}'.");

                _r = value;
            }
        }

        public int G
        {
            get { return _g; }
            set
            {
                if (!(0 <= value && value < 255))
                    throw new
                        FormatException($"Expected G value '0..255', got '{value}'.");

                _g = value;
            }
        }


        public int B
        {
            get { return _b; }
            set
            {
                if (!(0 <= value && value < 255))
                    throw new
                        FormatException($"Expected B value '0..255', got '{value}'.");

                _b = value;
            }
        }

#warning TODO: Validate that ymin < ymax
        public double YMin { get; set; }
        public double YMax { get; set; }

        #endregion
    }
}
