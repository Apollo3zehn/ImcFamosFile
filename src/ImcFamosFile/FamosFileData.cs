namespace ImcFamosFile
{
    public class FamosFileData
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <seealso cref="FamosFileData"/> which contains metadata and data of a certain variable.
        /// </summary>
        /// <param name="variable">The variable containing the metadata.</param>
        /// <param name="buffer">The buffer containing the actual data</param>
        public FamosFileData(FamosFileVariable variable, double[] buffer)
        {
            this.Variable = variable;
            this.Buffer = buffer;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The variable containing the metadata.
        /// </summary>
        public FamosFileVariable Variable { get; set; }

        /// <summary>
        /// The buffer containing the actual data.
        /// </summary>
        public double[] Buffer { get; set; }

        #endregion
    }

    public class FamosFileData<T>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <seealso cref="FamosFileData{T}"/> which contains metadata and data of a certain variable.
        /// </summary>
        /// <param name="variable">The variable containing the metadata.</param>
        /// <param name="buffer">The buffer containing the actual data</param>
        public FamosFileData(FamosFileVariable variable, T[] buffer)
        {
            this.Variable = variable;
            this.Buffer = buffer;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The variable containing the metadata.
        /// </summary>
        public FamosFileVariable Variable { get; set; }
        
        /// <summary>
        /// The buffer containing the actual data.
        /// </summary>
        public T[] Buffer { get; set; }

        #endregion
    }
}
