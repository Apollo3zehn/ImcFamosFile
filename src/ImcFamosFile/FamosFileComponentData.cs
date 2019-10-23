using System;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    public abstract class FamosFileComponentData
    {
        #region Constructors

        protected FamosFileComponentData(FamosFileComponent component,
                                         FamosFileXAxisScaling? xAxisScaling,
                                         FamosFileZAxisScaling? zAxisScaling,
                                         FamosFileTriggerTime? triggerTime,
                                         byte[] data)
        {
            this.Type = component.Type;

            this.XAxisScaling = xAxisScaling?.Clone();
            this.ZAxisScaling = zAxisScaling?.Clone();
            this.TriggerTime = triggerTime?.Clone();

            this.PackInfo = component.PackInfo;

            this.DisplayInfo = component.DisplayInfo;
            this.EventReference = component.EventReference;

            this.RawData = data;
        }

        #endregion

        #region Properties

        public FamosFileComponentType Type { get; }

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

        internal FamosFileComponentData(FamosFileComponent component,
                                        FamosFileXAxisScaling? xAxisScaling,
                                        FamosFileZAxisScaling? zAxisScaling,
                                        FamosFileTriggerTime? triggerTime,
                                        byte[] buffer) : base(component, xAxisScaling, zAxisScaling, triggerTime, buffer)
        {
            //
        }

        #endregion

        #region Properties

        public Span<T> Data => MemoryMarshal.Cast<byte, T>(this.RawData.AsSpan());

        #endregion
    }
}
