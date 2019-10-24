using System;

namespace ImcFamosFile
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    sealed class HideFromApiAttribute : Attribute
    {
        public HideFromApiAttribute()
        {
            //
        }
    }
}
