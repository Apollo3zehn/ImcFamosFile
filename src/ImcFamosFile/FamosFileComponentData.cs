using System;
using System.Runtime.InteropServices;

namespace ImcFamosFile
{
    /// <summary>
    /// Base class for component data, which provides the actual data and additional information to interpret these.
    /// </summary>
    public abstract class FamosFileComponentData
    {
        #region Constructors

        private protected FamosFileComponentData(FamosFileComponent component,
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

        /// <summary>
        /// Gets the component type.
        /// </summary>
        public FamosFileComponentType Type { get; }

        /// <summary>
        /// Gets the x-axis scaling of this component.
        /// </summary>
        public FamosFileXAxisScaling? XAxisScaling { get; }

        /// <summary>
        /// Gets the z-axis scaling of this component.
        /// </summary>
        public FamosFileZAxisScaling? ZAxisScaling { get; }

        /// <summary>
        /// Gets the trigger time this component.
        /// </summary>
        public FamosFileTriggerTime? TriggerTime { get; }

        /// <summary>
        /// Gets the pack info containing a description of this components data layout.
        /// </summary>
        public FamosFilePackInfo PackInfo { get; }

        /// <summary>
        /// Gets the display info to describe how to display the data.
        /// </summary>
        public FamosFileDisplayInfo? DisplayInfo { get; }

        /// <summary>
        /// Gets the event reference containing a description of related events.
        /// </summary>
        public FamosFileEventReference? EventReference { get; }

        /// <summary>
        /// Gets the raw data (bytes).
        /// </summary>
        public byte[] RawData { get; }

        #endregion
    }

    /// <summary>
    /// This type provides the actual data of type <typeparamref name="T"/> and additional information to interpret these.
    /// </summary>
    /// <typeparam name="T">The data type parameter.</typeparam>
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

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/>.
        /// </summary>
        public Span<T> Data => MemoryMarshal.Cast<byte, T>(this.RawData.AsSpan());

        #endregion
    }
}
