using System;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Disposable scope for temporarily switching to legacy parser mode
    /// </summary>
    public readonly struct LegacyParserScope : IDisposable
    {
        private readonly bool previousMode;

        public LegacyParserScope(bool useLegacy)
        {
            previousMode = EscapePattern.UseLegacyParser;
            EscapePattern.UseLegacyParser = useLegacy;
        }

        public void Dispose()
        {
            EscapePattern.UseLegacyParser = previousMode;
        }
    }
}