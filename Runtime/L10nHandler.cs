using Minerva.Localizations.EscapePatterns;
using Minerva.Module;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Minerva.Localizations
{
    public class L10nHandler : IL10nHandler
    {
        [SerializeField]
        private bool loaded = false;

        [SerializeField]
        private L10nDataManager manager;
        [SerializeField]
        private string region;



        // dictionary is for fast-lookup
        private Dictionary<string, TranslationEntry> dictionary;
        private Dictionary<string, TranslationEntry> fallback;
        // trie is for hierachy search
        private Tries<TranslationEntry> trie;

        public L10nDataManager Manager => manager;
        public bool IsLoaded => loaded;
        public string Region => region;


        internal Tries<TranslationEntry> Trie { get => trie ??= new(dictionary); }



        /// <summary>
        /// Construct a localization handler
        /// </summary>
        /// <param name="region"></param>
        public L10nHandler()
        {
        }




        /// <summary>
        /// Init L10n
        /// </summary>
        /// <param name="manager"></param>
        public void Init(L10nDataManager manager)
        {
            this.manager = manager;

            LanguageFile defaultRegion = manager.GetLanguageFile(manager.defaultRegion);
            if (defaultRegion)
            {
                this.fallback = defaultRegion.GetTranslationDictionary();
            }
            else
            {
                this.fallback = new();
                Debug.LogException(new NullReferenceException("Default region not found"));
            }
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void Load(string region)
        {
            this.region = region;
            if (!manager) throw new NullReferenceException("The localization manager has not yet initialized");

            LanguageFile languageFile = manager.GetLanguageFile(this.region);
            if (!languageFile) throw new NullReferenceException("The localization manager has initialized, but given language type could not be found and default region is not available");

            dictionary = languageFile.GetTranslationDictionary();
            trie = null;
            //string[] items = Dictionary.Keys.ToArray();
            //foreach (var item in items) dictionary[item] = EscapePattern.ReplaceKeyEscape(dictionary[item], null);
            loaded = true;
            Debug.Log($"Localization Loaded. (Region: {region}, Entry Count: {dictionary.Count})");
        }

        /// <summary>
        /// reload current localization
        /// </summary>
        public void Reload()
        {
            Load(region);
        }








        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        public string GetRawContent(string key)
        {
            if (!loaded)
            {
                Debug.LogWarning($"L10n not initialize");
                return null;
            }

            bool isPrimitive;
            bool hasValue = isPrimitive = dictionary.TryGetValue(key, out var entry);
            if (!hasValue) hasValue = dictionary.TryGetValue(key, out entry);
            if (!hasValue) hasValue = fallback.TryGetValue(key, out entry);
            if (!hasValue) return null;

            // late replace
            if (!entry.colorReplaced)
            {
                entry.value = EscapePattern.ReplaceColorEscape(entry.value);
                entry.colorReplaced = true;
            }
            // write back
            if (isPrimitive)
            {
                dictionary[key] = entry;
                Trie[key] = entry;
            }
            return entry.value;
        }

        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        public string GetRawContent(Key key)
        {
            if (!loaded)
            {
                Debug.LogWarning($"L10n not initialize");
                return null;
            }

            bool isPrimitive;
            bool hasValue = isPrimitive = Trie.TryGetValue(key.Section, out var entry);
            if (!hasValue) hasValue = dictionary.TryGetValue(key, out entry);
            if (!hasValue) hasValue = fallback.TryGetValue(key, out entry);
            if (!hasValue) return null;

            // late replace
            if (!entry.colorReplaced)
            {
                entry.value = EscapePattern.ReplaceColorEscape(entry.value);
                entry.colorReplaced = true;
            }
            // write back
            if (isPrimitive)
            {
                dictionary[key] = entry;
                Trie[key] = entry;
            }
            return entry.value;
        }



        public bool Contains(string key, bool fallback)
        {
            return dictionary.ContainsKey(key) || (fallback && this.fallback.ContainsKey(key));
        }

        public bool Contains(Key key, bool fallback)
        {
            return Trie.ContainsKey(key.Section) || (fallback && this.fallback.ContainsKey(key));
        }





        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public bool Write(string key, string value)
        {
            dictionary[key] = value;
            Trie[key] = value;
            return true;
        }

        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public bool Write(Key key, string value)
        {
            dictionary[key] = value;
            Trie[key.Section] = value;
            return true;
        }





        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public bool OptionOf(string partialKey, out string[] result, bool firstLevelOnly = false)
        {
            result = Array.Empty<string>();
            if (!loaded) { return false; }
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
                return false;
            if (firstLevelOnly)
                result = subTrie.FirstLayerKeys.ToArray();
            else
            {
                result = new string[subTrie.Count];
                subTrie.Keys.CopyTo(result, 0);
            }
            return true;
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public bool OptionOf(Key partialKey, out string[] result, bool firstLevelOnly = false)
        {
            result = Array.Empty<string>();
            if (!loaded) { return false; }
            if (!Trie.TryGetSegment(partialKey.Section, out TriesSegment<TranslationEntry> subTrie))
                return false;
            if (firstLevelOnly)
                result = subTrie.FirstLayerKeys.ToArray();
            else
            {
                result = new string[subTrie.Count];
                subTrie.Keys.CopyTo(result, 0);
            }
            return true;
        }




        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (!loaded) { return false; }
            if (!Trie.TryGetSegment(partialKey, out TriesSegment<TranslationEntry> subTrie))
                return false;
            if (firstLevelOnly)
                strings.AddRange(subTrie.FirstLayerKeys);
            else
                strings.AddRange(subTrie.Keys);
            return true;
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (!loaded) { return false; }
            if (!Trie.TryGetSegment(partialKey.Section, out TriesSegment<TranslationEntry> subTrie))
                return false;
            if (firstLevelOnly)
                strings.AddRange(subTrie.FirstLayerKeys);
            else
                strings.AddRange(subTrie.Keys);
            return true;
        }
    }
}