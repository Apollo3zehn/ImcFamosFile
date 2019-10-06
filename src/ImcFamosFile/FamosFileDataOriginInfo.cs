namespace ImcFamosFile
{
    public class FamosFileDataOriginInfo
    {
        #region Constructors

        public FamosFileDataOriginInfo()
        {
            this.Name = string.Empty;
            this.Comment = string.Empty;
            this.DataOrigin = FamosFileDataOrigin.Original;
        }

        #endregion

        #region Properties

        public string Name { get; set; }
        public string Comment { get; set; }

        public FamosFileDataOrigin DataOrigin { get; set; }

        #endregion
    }
}
