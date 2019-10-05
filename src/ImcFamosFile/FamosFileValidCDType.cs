using System;

namespace ImcFamosFile
{
    [Flags]
    public enum FamosFileValidCDType
    {
        UseDxFromEventList = 0,
        UseX0FromEventList = 1,
        UseZ0FromX0FromEventList = 2
    }
}
