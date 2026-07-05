using Minerva.Localizations.EscapePatterns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Minerva.Localizations
{
    /// <summary>
    /// Holds the loaded lookup data for one localization region.
    /// </summary>
    internal sealed class L10nRegionData
    {
        #region State

        private readonly L10nDataManager manager;
        private readonly string region;
        private Dictionary<string, TranslationEntry> dictionary = new();
        private L10nTries<TranslationEntry> trie;
        private string listDelimiter;
        private string wordSpace;

        public string Region => region;
        public string ListDelimiter => listDelimiter ?? string.Empty;
        public string WordSpace => wordSpace ?? string.Empty;
        public bool IsLoaded => dictionary != null;
        public int EntryCount => dictionary?.Count ?? 0;

        private L10nTries<TranslationEntry> Trie => trie ??= new(dictionary);

        #endregion

        #region Loading

        /// <summary>
        /// Creates region data and immediately loads its lookup table.
        /// </summary>
        public L10nRegionData(L10nDataManager manager, string region)
        {
            this.manager = manager ? manager : throw new ArgumentNullException(nameof(manager));
            this.region = region ?? string.Empty;
            Reload();
        }

        /// <summary>
        /// Rebuilds this region's dictionary and formatting options from its language file.
        /// </summary>
        public void Reload()
        {
            LanguageFile languageFile = manager.GetLanguageFile(region);
            if (!languageFile)
            {
                throw new NullReferenceException($"Localization region '{region}' could not be found.");
            }

            dictionary = languageFile.GetTranslationDictionary();
            trie = null;
            listDelimiter = languageFile.listDelimiter;
            wordSpace = languageFile.wordSpace;
        }

        #endregion

        #region Raw Content

        /// <summary>
        /// Gets localized raw content from this region and then from the shared fallback.
        /// </summary>
        public string GetRawContent(string key, L10nRegionData fallback)
        {
            if (TryGetLocal(key, out string value))
            {
                return value;
            }

            return fallback != null && fallback != this && fallback.TryGetLocal(key, out value) ? value : null;
        }

        /// <summary>
        /// Gets localized raw content from this region and then from the shared fallback.
        /// </summary>
        public string GetRawContent(Key key, L10nRegionData fallback)
        {
            if (TryGetLocal(key, out string value))
            {
                return value;
            }

            return fallback != null && fallback != this && fallback.TryGetLocal(key, out value) ? value : null;
        }

        #endregion

        #region Key Lookup & Overrides

        /// <summary>
        /// Checks whether this region contains a key, optionally including shared fallback.
        /// </summary>
        public bool Contains(string key, L10nRegionData fallback)
        {
            return dictionary.ContainsKey(key) || (fallback != null && fallback != this && fallback.dictionary.ContainsKey(key));
        }

        /// <summary>
        /// Checks whether this region contains a key, optionally including shared fallback.
        /// </summary>
        public bool Contains(Key key, L10nRegionData fallback)
        {
            return Trie.ContainsKey(key) || (fallback != null && fallback != this && fallback.Trie.ContainsKey(key));
        }

        /// <summary>
        /// Writes an in-memory override for this loaded region.
        /// </summary>
        public void Write(string key, string value)
        {
            dictionary[key] = value;
            Trie[key] = value;
        }

        /// <summary>
        /// Writes an in-memory override for this loaded region.
        /// </summary>
        public void Write(Key key, string value)
        {
            dictionary[key] = value;
            Trie[key] = value;
        }

        /// <summary>
        /// Gets matching localization key options from this region.
        /// </summary>
        public bool OptionOf(string partialKey, out string[] result, bool firstLevelOnly = false)
        {
            result = Array.Empty<string>();
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
            {
                return false;
            }

            result = firstLevelOnly ? subTrie.FirstLayerKeys.ToArray() : subTrie.Keys.ToArray();
            return true;
        }

        /// <summary>
        /// Gets matching localization key options from this region.
        /// </summary>
        public bool OptionOf(Key partialKey, out string[] result, bool firstLevelOnly = false)
        {
            result = Array.Empty<string>();
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
            {
                return false;
            }

            result = firstLevelOnly ? subTrie.FirstLayerKeys.ToArray() : subTrie.Keys.ToArray();
            return true;
        }

        /// <summary>
        /// Copies matching localization key options from this region.
        /// </summary>
        public bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
            {
                return false;
            }

            if (firstLevelOnly)
            {
                strings.AddRange(subTrie.FirstLayerKeys);
            }
            else
            {
                strings.AddRange(subTrie.Keys);
            }
            return true;
        }

        /// <summary>
        /// Copies matching localization key options from this region.
        /// </summary>
        public bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
            {
                return false;
            }

            if (firstLevelOnly)
            {
                strings.AddRange(subTrie.FirstLayerKeys);
            }
            else
            {
                strings.AddRange(subTrie.Keys);
            }
            return true;
        }

        #endregion

        #region Private Helpers

        private bool TryGetLocal(string key, out string value)
        {
            if (!dictionary.TryGetValue(key, out TranslationEntry entry))
            {
                value = null;
                return false;
            }

            value = PrepareEntry(key, entry);
            return true;
        }

        private bool TryGetLocal(Key key, out string value)
        {
            if (!Trie.TryGetValue(key, out TranslationEntry entry))
            {
                value = null;
                return false;
            }

            value = PrepareEntry(key, entry);
            return true;
        }

        private string PrepareEntry(string key, TranslationEntry entry)
        {
            if (!entry.colorReplaced)
            {
                entry.value = EscapePattern.ReplaceColorEscape(entry.value);
                entry.colorReplaced = true;
                dictionary[key] = entry;
                Trie[key] = entry;
            }

            return entry.value;
        }

        private string PrepareEntry(Key key, TranslationEntry entry)
        {
            if (!entry.colorReplaced)
            {
                entry.value = EscapePattern.ReplaceColorEscape(entry.value);
                entry.colorReplaced = true;
                dictionary[key] = entry;
                Trie[key] = entry;
            }

            return entry.value;
        }

        #endregion
    }
}
