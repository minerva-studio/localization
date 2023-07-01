using System;

namespace Minerva.Localizations
{
    /// <summary>
    /// L10n content directly use given string as base key
    /// </summary>
    [CustomContent(typeof(Enum))]
    public class EnumL10nContent : L10nContent
    {
        public EnumL10nContent(Enum value) : base(value)
        {
            baseKey = Localizable.AppendKey(baseKey, value.ToString());
        }

        /// <summary>
        /// Only returns the escape key because escape value cannot be retrieve from enum
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        public override string GetEscapeValue(string escapeKey, params string[] param)
        {
            return escapeKey;
        }
    }
}