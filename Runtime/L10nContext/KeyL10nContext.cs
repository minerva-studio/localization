namespace Minerva.Localizations
{
    /// <summary>
    /// L10n content directly use given string as base key, for short term translation, try to use <see cref="L10n.Tr(string, string[])"/> instead
    /// </summary>
    public class KeyL10nContext : L10nContext
    {
        public KeyL10nContext() : base() { }
        public KeyL10nContext(string value)
        {
            BaseKeyString = (string)value;
        }

        protected override void Parse(object value)
        {
            BaseKeyString = (string)value;
        }

        public override object GetEscapeValue(string escapeKey, L10nParams parameters)
        {
            return escapeKey;
        }
    }
}