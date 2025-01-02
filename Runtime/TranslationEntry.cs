namespace Minerva.Localizations
{
    internal struct TranslationEntry
    {
        public string value;
        public bool colorReplaced;

        public static implicit operator TranslationEntry(string value)
        {
            return new TranslationEntry
            {
                value = value,
                colorReplaced = false
            };
        }
    }

}