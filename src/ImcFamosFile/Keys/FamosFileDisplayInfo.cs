using System;
using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// Contains information about how to display a component.
    /// </summary>
    public class FamosFileDisplayInfo : FamosFileBase
    {
        #region Fields

        private int _r;
        private int _g;
        private int _b;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FamosFileDisplayInfo"/> class.
        /// </summary>
        /// <param name="ymin">The lower y-axis display limit.</param>
        /// <param name="ymax">The upper y-axis display limit.</param>
        public FamosFileDisplayInfo(decimal ymin, decimal ymax)
        {
            this.YMin = ymin;
            this.YMax = ymax;

            this.InternalValidate();
        }

        internal FamosFileDisplayInfo(BinaryReader reader) : base(reader)
        {
            this.DeserializeKey(expectedKeyVersion: 1, keySize =>
            {
                this.R = this.DeserializeInt32();
                this.G = this.DeserializeInt32();
                this.B = this.DeserializeInt32();
                this.YMin = this.DeserializeReal();
                this.YMax = this.DeserializeReal();
            });

            this.InternalValidate();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the red part of the display color (0..255).
        /// </summary>
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

        /// <summary>
        /// Gets or sets the green part of the display color (0..255).
        /// </summary>
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

        /// <summary>
        /// Gets or sets the blue part of the display color (0..255).
        /// </summary>
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

        /// <summary>
        /// Gets the lower y-axis display limit.
        /// </summary>
        public decimal YMin { get; private set; }

        /// <summary>
        /// Gets the upper y-axis display limit.
        /// </summary>
        public decimal YMax { get; private set; }

        protected override FamosFileKeyType KeyType => FamosFileKeyType.ND;

        #endregion

        #region Methods

        private void InternalValidate()
        {
            if (this.YMin >= this.YMax)
                throw new FormatException("YMin must be < YMax.");
        }

        #endregion

        #region Serialization

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                this.R,
                this.G,
                this.B,
                this.YMin,
                this.YMax
            };

            this.SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
