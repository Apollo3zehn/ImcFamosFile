using System.Collections.Generic;

namespace FamosFile.NET
{
    public class FamosFileBufferInformation
    {
        #region Constructors

        public FamosFileBufferInformation()
        {
            this.Buffers = new List<FamosFileBuffer>();
        }

        #endregion

        #region Properties

        public List<FamosFileBuffer> Buffers { get; private set; }
        

        #endregion
    }
}
