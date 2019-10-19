using System;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    public abstract class FamosFileComponentData
    {
        #region Constructors

        internal FamosFileComponentData(FamosFileComponent component, byte[] data)
        {
            this.XAxisScaling = component.XAxisScaling;
            this.ZAxisScaling = component.ZAxisScaling;
            this.TriggerTime = component.TriggerTime;

            this.PackInfo = component.PackInfo;

            this.DisplayInfo = component.DisplayInfo;
            this.EventReference = component.EventReference;

            this.RawData = data;
        }

        #endregion

        #region Properties

        public FamosFileXAxisScaling? XAxisScaling { get; }
        public FamosFileZAxisScaling? ZAxisScaling { get; }
        public FamosFileTriggerTime? TriggerTime { get; }

        public FamosFilePackInfo PackInfo { get; }

        public FamosFileDisplayInfo? DisplayInfo { get; }
        public FamosFileEventReference? EventReference { get; }

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
