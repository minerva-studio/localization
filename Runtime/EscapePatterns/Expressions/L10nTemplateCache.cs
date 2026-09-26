using System.Collections.Concurrent;

namespace Minerva.Localizations.EscapePatterns
{
    internal static class L10nTemplateCache
    {
        private const int MaximumEntries = 4096;
        private static readonly ConcurrentDictionary<string, L10nTemplate> cache = new();

        static L10nTemplateCache()
        {
            L10n.OnLocalizationLoaded += Clear;
            L10n.OnRegionLoaded += Clear;
            L10n.OnRegionUnloaded += Clear;
            L10n.OnMainRegionChanged += Clear;
        }

        public static L10nTemplate Get(string source)
        {
            source ??= string.Empty;
            if (cache.TryGetValue(source, out var template)) return template;
            template = L10nTemplate.Compile(source);
            if (cache.Count >= MaximumEntries) cache.Clear();
            return cache.GetOrAdd(source, template);
        }

        private static void Clear() => cache.Clear();
        private static void Clear(string _) => cache.Clear();
    }
}
