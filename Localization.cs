using Minerva.Module;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amlos.Localizations
{
    /// <summary>
    /// The Localization main class
    /// </summary>
    [Serializable]
    public class Localization
    {
        public const char KEY_SEPARATOR = '.';

        public bool initialized = false;
        public string region;
        public KeyMissingSolution keyMissingSolution;
        public LocalizationDataManager manager;
        public Dictionary<string, string> dictionary;



        private static Localization instance;
        /// <summary> instance of localization model </summary>
        public static Localization Instance => instance ??= new();



        /// <summary>
        /// Construct a localization model
        /// </summary>
        /// <param name="languageType"></param>
        private Localization(string languageType = LocalizationRegion.DEFAULT)
        {
            region = languageType;
            instance = this;
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="LocalizationRegion.DEFAULT"/> would be used
        /// </para>
        /// </summary>
        /// <param name="manager">The localization Data Manager</param>
        /// <param name="region">Default <see cref="LocalizationRegion.DEFAULT"/></param>
        /// <exception cref="NullReferenceException"></exception>
        public void Load(LocalizationDataManager manager, string region = LocalizationRegion.DEFAULT)
        {
            this.manager = manager;
            keyMissingSolution = manager.missingKeySolution;
            Load(region);
        }

        /// <summary>
        /// Load given type of language
        /// <para>
        /// If the given language is not found in manager, then <see cref="LocalizationRegion.DEFAULT"/> would be used
        /// </para>
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void Load(string region)
        {
            this.region = region;
            if (!manager) throw new NullReferenceException("The localization manager has not yet initialized");

            LanguageFile languageFile = manager.GetLanguageFile(this.region);
            if (!languageFile) languageFile = manager.GetLanguageFile(LocalizationRegion.DEFAULT);
            if (!languageFile) throw new NullReferenceException("The localization manager has initialized, but given language type could not be found and default region is not available");
            dictionary = languageFile.GetDictionary();
            initialized = true;

            foreach (var item in dictionary.Keys.ShallowClone()) dictionary[item] = EscapePatterns.ReplaceColorEscape(dictionary[item]);
            foreach (var item in dictionary.Keys.ShallowClone()) dictionary[item] = EscapePatterns.ReplaceKeyEscape(dictionary[item]);

            TextLocalizerBase.ReloadAll();
        }

        /// <summary>
        /// reload current localization
        /// </summary>
        public void Reload()
        {
            keyMissingSolution = manager.missingKeySolution;
            Load(region);
        }

        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        internal string GetContent(string key)
        {
            if (initialized && !string.IsNullOrEmpty(key) && dictionary.TryGetValue(key, out var value) && value != null)
            {
                return value;
            }

            Debug.LogWarning($"Key {key} does not appear in the localization file {region}. The key will be added to localization manager if this happened in editor runtime.");
#if UNITY_EDITOR
            manager.AddMissingKey(key);
#endif    
            switch (keyMissingSolution)
            {
                default:
                case KeyMissingSolution.RawDisplay:
                    value = key;
                    break;
                case KeyMissingSolution.Empty:
                    value = string.Empty;
                    break;
                case KeyMissingSolution.ForceDisplay:
                    int index = key.LastIndexOf('.');
                    value = index > -1 ? key[index..] : key;
                    break;
            }
            return value;
        }

        /// <summary>
        /// Direclty get the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetContent(string key, params string[] param)
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

            string content;
            content = instance.GetContent(key);
            content = EscapePatterns.ReplaceDynamicValueEscape(content, param);
            return content;
        }
    }
}