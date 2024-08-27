using System;

namespace Minerva.Localizations
{
    [System.AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class L10nFlagEnumAttribute : Attribute
    {
        public L10nFlagEnumAttribute() { }
    }
}
