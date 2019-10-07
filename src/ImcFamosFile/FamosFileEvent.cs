using System;

namespace ImcFamosFile
{
    public class FamosFileEvent
    {
        #region Fields

        private int _index;

        #endregion

        #region Constructors

        public FamosFileEvent()
        {
            //
        }

        internal FamosFileEvent(int index)
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

        public ulong Offset { get; set; }
        public ulong Length { get; set; }
        public double Time { get; set; }
        public double AmplitudeOffset0 { get; set; }
        public double AmplitudeOffset1 { get; set; }
        public double x0 { get; set; }
        public double AmplificationFactor0 { get; set; }
        public double AmplificationFactor1 { get; set; }
        public double dx { get; set; }

        #endregion
    }
}
