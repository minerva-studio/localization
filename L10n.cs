using Minerva.Localizations.Components;
using Minerva.Localizations.EscapePatterns;
using Minerva.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// The Localization main class
    /// </summary>
    [Serializable]
    public class L10n
    {
        public const string DEFAULT_REGION = "EN_US";

        public bool initialized = false;
        public bool disableEmptyEntries;
        public string region;

        public MissingKeySolution missingKeySolution;
        public L10nDataManager manager;

        // dictionary is for fast-lookup
        private Dictionary<string, string> dictionary;
        // trie is for hierachy search
        private Tries<string> trie;



        private static L10n instance;


        /// <summary> instance of localization model </summary>
        public static L10n Instance => instance ??= new();
        public static bool isInitialized => instance?.initialized == true;
        public static string Region => instance?.region ?? string.Empty;
        public static List<string> Regions => instance?.manager != null ? instance.manager.regions : new List<string>();


        /// <summary>
        /// Construct a localization model
        /// </summary>
        /// <param name="languageType"></param>
        private L10n(string languageType = DEFAULT_REGION)
        {
            region = languageType;
            instance = this;
        }


        /// <summary>
        /// Init L10n
        /// </summary>
        /// <param name="manager"></param>
        public static void Init(L10nDataManager manager)
        {
            Instance.Instance_Init(manager);
        }

        /// <summary>
        /// Init L10n
        /// </summary>
        /// <param name="manager"></param>
        private void Instance_Init(L10nDataManager manager)
        {
            this.manager = manager;
            this.missingKeySolution = manager.missingKeySolution;
            this.disableEmptyEntries = manager.disableEmptyEntry;
        }

        /// <summary>
        /// Init and Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="manager">The localization Data Manager</param>
        /// <param name="region">Region to set</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void InitAndLoad(L10nDataManager manager, string region)
        {
            Init(manager);
            Load(region);
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void Load(string region)
        {
            Instance.Instance_Load(region);
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        private void Instance_Load(string region)
        {
            this.region = region;
            if (!manager) throw new NullReferenceException("The localization manager has not yet initialized");

            LanguageFile languageFile = manager.GetLanguageFile(this.region);
            if (!languageFile) languageFile = manager.GetLanguageFile(DEFAULT_REGION);
            if (!languageFile) throw new NullReferenceException("The localization manager has initialized, but given language type could not be found and default region is not available");

            dictionary = languageFile.GetDictionary();
            string[] items = dictionary.Keys.ToArray();
            // directly convert color escape and key escape because they don't change
            foreach (var item in items) dictionary[item] = EscapePattern.ReplaceColorEscape(dictionary[item]);
            foreach (var item in items) dictionary[item] = EscapePattern.ReplaceKeyEscape(dictionary[item], null);
            trie = new Tries<string>(dictionary);
            initialized = true;

            TextLocalizerBase.ReloadAll();
        }

        public static void ReloadIfInitialized()
        {
            if (instance == null || !instance.initialized) return;
            Reload();
        }

        /// <summary>
        /// reload current localization
        /// </summary>
        public static void Reload()
        {
            instance.Instance_Load(instance.region);
        }

        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        private string Instance_GetRawContent(string key)
        {
            if (!initialized)
            {
                Debug.LogWarning($"L10n not initialize");
                goto missing;
            }
            if (string.IsNullOrEmpty(key) || !dictionary.TryGetValue(key, out var value) || value == null)
            {
                Debug.LogWarning($"Key {key} does not appear in the localization file {region}. The key will be added to localization manager if this happened in editor runtime.");
#if UNITY_EDITOR
                manager.AddMissingKey(key);
#endif    
                goto missing;
            }
            if (disableEmptyEntries && string.IsNullOrEmpty(value))
            {
                Debug.LogWarning($"Key {key} has empty entry!");
                goto missing;
            }

            return value;

        missing:
            switch (missingKeySolution)
            {
                default:
                case MissingKeySolution.RawDisplay:
                    value = key;
                    break;
                case MissingKeySolution.Empty:
                    value = string.Empty;
                    break;
                case MissingKeySolution.ForceDisplay:
                    int index = key.LastIndexOf('.');
                    value = index > -1 ? key[index..] : key;
                    value = value.ToTitleCase();
                    break;
            }
            return value;
        }

        /// <summary>
        /// Direclty get the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetRawContent(string key)
        {
            // localization not loaded
            if (instance == null)
            {
                return key;
            }
            // localization not initialized, no language pack loaded
            if (!instance.initialized)
            {
                return key;
            }

            return instance.Instance_GetRawContent(key);
        }

        /// <summary>
        /// Override given key's entry to value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal static void Override(string key, string value)
        {
            instance.dictionary[key] = value;
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static List<string> OptionOf(string partialKey, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.initialized) { return new List<string>(); }
            return instance.Instance_OptionOf(partialKey, firstLevelOnly);
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        private List<string> Instance_OptionOf(string partialKey, bool firstLevelOnly = false)
        {
            if (!initialized) { return new List<string>(); }
            if (firstLevelOnly)
                return trie.GetSubTrie(partialKey).GetFirstLevelKeys();
            else return trie.GetSubTrie(partialKey).Keys;
        }






        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(string key, params string[] param)
        {
            var rawString = GetRawContent(key);
            rawString = EscapePattern.Escape(rawString, null, param);
            return rawString;
        }

        /// <summary>
        /// Direct localization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizable context, params string[] param)
        {
            return Localizable.Tr(context, param);
        }

        /// <summary>
        /// Direct localization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizable context, params string[] param)
        {
            return Localizable.TrKey(key, context, param);
        }
    }
}