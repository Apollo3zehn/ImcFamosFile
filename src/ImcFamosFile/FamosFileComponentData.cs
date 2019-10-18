using System;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    public abstract class FamosFileComponentData
    {
        #region Constructors

        internal FamosFileComponentData(FamosFileComponent component, byte[] data)
        {
            this.Component = component;
            this.RawData = data;
        }

        #endregion

        #region Properties

        public FamosFileComponent Component { get; }

        public byte[] RawData { get; }

        #endregion
    }

    public class FamosFileComponentData<T> : FamosFileComponentData where T : unmanaged
    {
        #region Constructors

        public FamosFileComponentData(FamosFileComponent component, byte[] buffer) : base(component, buffer)
        {
            //
        }

        #endregion

        #region Properties

        public Span<T> Data => MemoryMarshal.Cast<byte, T>(this.RawData.AsSpan());

        #endregion
    }
}
