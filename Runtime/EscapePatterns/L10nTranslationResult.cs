#nullable enable

namespace Minerva.Localizations.EscapePatterns
{
    public struct L10nTranslationResult
    {
        public readonly static L10nTranslationResult Empty = new L10nTranslationResult(string.Empty);

        public string TranslatedText { get; internal set; }
        public L10nEvaluationDiagnostics? Diagnostics { get; internal set; }


        public L10nTranslationResult(string translatedText) : this()
        {
            this.TranslatedText = translatedText;
        }

        public L10nTranslationResult(string translatedText, L10nEvaluationDiagnostics? diagnostics)
        {
            TranslatedText = translatedText;
            Diagnostics = diagnostics;
        }
    }
}