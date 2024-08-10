using System;

namespace Minerva.Localizations
{
    /// <summary>
    /// L10n content directly use given string as base key
    /// </summary>
    public class EnumL10nContext : L10nContext
    {
        public EnumL10nContext() { }

        public EnumL10nContext(Enum value) : base(value) { }

        protected override void Parse(object value)
        {
            BaseKey = new Key(value.GetType().FullName, value.ToString());
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