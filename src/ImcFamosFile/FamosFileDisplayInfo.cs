using System;
using System.IO;

namespace ImcFamosFile
{
    public class FamosFileDisplayInfo : FamosFileBase
    {
        #region Constructors

        public FamosFileDisplayInfo()
        {
            //
        }

        internal FamosFileDisplayInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.R = this.DeserializeInt32();
                this.G = this.DeserializeInt32();
                this.B = this.DeserializeInt32();
                this.YMin = this.DeserializeFloat64();
                this.YMax = this.DeserializeFloat64();
            });
        }

        #endregion

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

        public double YMin { get; set; }
        public double YMax { get; set; }

        #endregion

        #region Methods

        internal override void Validate()
        {
            if (this.YMin >= this.YMax)
                throw new FormatException("YMin must be < YMax.");
        }

        #endregion
    }
}
