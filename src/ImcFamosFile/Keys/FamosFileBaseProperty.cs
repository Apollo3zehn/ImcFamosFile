﻿using System.IO;

namespace ImcFamosFile
{
    public abstract class FamosFileBaseProperty : FamosFileBaseExtended
    {
        #region Constructors

        public FamosFileBaseProperty()
        {
            //
        }

        public FamosFileBaseProperty(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            //
        }

        #endregion

        #region Properties

        public FamosFilePropertyInfo? PropertyInfo { get; set; }

        #endregion

        #region Methods

        internal override void Serialize(StreamWriter writer)
        {
            this.PropertyInfo?.Serialize(writer);
        }

        #endregion
    }
}