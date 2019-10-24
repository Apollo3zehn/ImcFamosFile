using System.IO;

namespace ImcFamosFile
{
    /// <summary>
    /// A base class for (de)serializable keys that allows having additional properties assigned.
    /// </summary>
    public abstract class FamosFileBaseProperty : FamosFileBaseExtended
    {
        #region Constructors

        protected FamosFileBaseProperty()
        {
            //
        }

        protected FamosFileBaseProperty(BinaryReader reader, int codePage) : base(reader, codePage)
        {
            //
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="FamosFilePropertyInfo", which contains a list of custom properties./>
        /// </summary>
        public FamosFilePropertyInfo? PropertyInfo { get; set; }

        #endregion

        #region Methods

        internal override void Serialize(BinaryWriter writer)
        {
            this.PropertyInfo?.Serialize(writer);
        }

        #endregion
    }
}
