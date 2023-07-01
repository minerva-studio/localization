using Minerva.Localizations.Components;
using Minerva.Localizations.EscapePatterns;
using Minerva.Module;
using System;
using System.Collections.Generic;
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
        public string region;

        public bool disableEmptyEntries;
        public MissingKeySolution missingKeySolution;

        public L10nDataManager manager;
        public Dictionary<string, string> dictionary;



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
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="manager">The localization Data Manager</param>
        /// <param name="region">Region to set</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void Load(L10nDataManager manager, string region = DEFAULT_REGION)
        {
            Instance.Instance_Load(manager, region);
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="DEFAULT_REGION"/> would be used
        /// </para>
        /// </summary>
        /// <param name="manager">The localization Data Manager</param>
        /// <param name="region">Region to set</param>
        /// <exception cref="NullReferenceException"></exception>
        private void Instance_Load(L10nDataManager manager, string region = DEFAULT_REGION)
        {
            this.manager = manager;
            this.missingKeySolution = manager.missingKeySolution;
            this.disableEmptyEntries = manager.disableEmptyEntry;
            Instance_Load(region);
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
            instance.Instance_Load(region);
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
            initialized = true;

            IEnumerable<string> items = dictionary.Keys.ShallowClone();
            foreach (var item in items) dictionary[item] = EscapePattern.ReplaceColorEscape(dictionary[item]);
            foreach (var item in items) dictionary[item] = EscapePattern.ReplaceKeyEscape(dictionary[item]);

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
            if (initialized && !string.IsNullOrEmpty(key) && dictionary.TryGetValue(key, out var value) && value != null)
            {
                if (!disableEmptyEntries || !string.IsNullOrEmpty(value))
                {
                    return value;
                }
                else
                {
                    Debug.LogWarning($"Key {key} has empty entry!");
                }
            }
            else
            {
                Debug.LogWarning($"Key {key} does not appear in the localization file {region}. The key will be added to localization manager if this happened in editor runtime.");
#if UNITY_EDITOR
                manager.AddMissingKey(key);
#endif    
            }

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
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(string key, params string[] param)
        {
            var rawString = GetRawContent(key);
            rawString = EscapePattern.ReplaceKeyEscape(rawString);
            rawString = EscapePattern.ReplaceDynamicValueEscape(rawString, null, param);
            rawString = EscapePattern.ReplaceColorEscape(rawString);
            return rawString;
        }
    }
}