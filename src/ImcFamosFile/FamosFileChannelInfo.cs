namespace ImcFamosFile
{
    public class FamosFileChannelInfo
    {
        #region Constructors

        public FamosFileChannelInfo()
        {
            this.Name = string.Empty;
            this.Comment = string.Empty;
        }

        #endregion

        #region Properties

        public int GroupIndex { get; set; }
        public int BitIndex { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }

        #endregion
    }
}
