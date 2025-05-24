using Minerva.Localizations.EscapePatterns;
using Minerva.Module;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Minerva.Localizations
{
    public delegate void OnTranslating(string key, ref string value);

    /// <summary>
    /// The Localization main class
    /// </summary>
    [Serializable]
    public class L10n
    {
        public const string DEFAULT_REGION = "EN_US";
        public const int MAX_RECURSION = 30;

        [SerializeField]
        private bool loaded = false;

        [SerializeField]
        private L10nDataManager manager;
        [SerializeField]
        private string region;
        [SerializeField]
        private bool disableEmptyEntries;
        [SerializeField]
        private ReferenceImportOption tooltipImportOption;
        [SerializeField]
        private ReferenceImportOption referenceImportOption;
        [SerializeField]
        private UnderlineResolverOption useUnderlineResolver;
        [SerializeField]
        private MissingKeySolution missingKeySolution;

        [SerializeField]
        private string wordSpace;
        [SerializeField]
        private string listDelimiter;




        // dictionary is for fast-lookup
        private Dictionary<string, TranslationEntry> dictionary;
        private Dictionary<string, TranslationEntry> fallback;
        // trie is for hierachy search
        private Tries<TranslationEntry> trie;

#if UNITY_EDITOR
        private HashSet<string> missing = new();
#endif



        public static event Action OnLocalizationLoaded;
        public static event Action<string> OnKeyMissing;
        public static event OnTranslating OnTranslating;
        private static L10n instance;


        /// <summary> instance of localization model </summary>
        public static L10n Instance => instance ??= new();
        /// <summary> manager </summary>
        public static L10nDataManager Manager => instance?.manager;
        /// <summary> whether any localization is loaded </summary>
        public static bool IsLoaded => instance?.loaded == true;
        /// <summary> whether a manager is provided </summary>
        public static bool IsInitialized => instance?.manager != null;
        /// <summary> Region </summary>
        public static string Region => instance?.region ?? string.Empty;
        /// <summary> Regions </summary>
        public static string[] Regions => instance?.manager != null ? instance.manager.regions.ToArray() : Array.Empty<string>();
        /// <summary> Should discard empty entries? </summary>
        public static bool DisableEmptyEntries { get { return instance?.disableEmptyEntries ?? false; } set { instance.disableEmptyEntries = value; } }
        /// <summary> Missing key solution </summary>
        public static MissingKeySolution MissingKeySolution { get { return instance?.missingKeySolution ?? 0; } set { instance.missingKeySolution = value; } }
        /// <summary> Tooltip Import Option </summary>
        public static ReferenceImportOption TooltipImportOption { get { return instance?.tooltipImportOption ?? 0; } set { instance.tooltipImportOption = value; } }
        /// <summary> Reference Import Option </summary>
        public static ReferenceImportOption ReferenceImportOption { get { return instance?.referenceImportOption ?? 0; } set { instance.referenceImportOption = value; } }
        /// <summary> Underline resolver to fix the tag conflict between content </summary>
        public static UnderlineResolverOption UseUnderlineResolver { get { return instance?.useUnderlineResolver ?? 0; } set { instance.useUnderlineResolver = value; } }

        public static string ListDelimiter => instance?.listDelimiter ?? string.Empty;
        public static string WordSpace => instance?.wordSpace ?? string.Empty;

        Dictionary<string, TranslationEntry> Dictionary { get => dictionary; }
        Tries<TranslationEntry> Trie { get => trie ??= new Tries<TranslationEntry>(Dictionary); }





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
            this.tooltipImportOption = manager.tooltipImportOption;
            this.referenceImportOption = manager.referenceImportOption;
            this.useUnderlineResolver = manager.useUnderlineResolver;

            string defaultRegion = string.IsNullOrEmpty(manager.defaultRegion) ? DEFAULT_REGION : manager.defaultRegion;
            LanguageFile languageFile = manager.GetLanguageFile(defaultRegion);
            if (!languageFile)
            {
                Debug.LogException(new NullReferenceException("Default region not found"));
            }
            else this.fallback = languageFile.GetTranslationDictionary();
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

            listDelimiter = languageFile.listDelimiter;
            wordSpace = languageFile.wordSpace;
            dictionary = languageFile.GetTranslationDictionary();
            trie = null;
            // fallback
            foreach (var item in fallback)
            {
                if (dictionary.ContainsKey(item.Key)) continue;
                dictionary.Add(item.Key, fallback[item.Key]);
            }
            //string[] items = Dictionary.Keys.ToArray();
            //foreach (var item in items) dictionary[item] = EscapePattern.ReplaceKeyEscape(dictionary[item], null);
            loaded = true;

            // safely invoke all method in delegate
            foreach (var item in OnLocalizationLoaded.GetInvocationList())
            {
                try { item?.DynamicInvoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }

            Debug.Log($"Localization Loaded. (Region: {region}, Entry Count: {Dictionary.Count})");
        }

        public static void ReloadIfInitialized()
        {
            if (instance == null || !instance.loaded) return;
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
        /// Deinitialize current loaded localization
        /// </summary>
        public static void DeInitialize()
        {
            Debug.Log("deinit");
            // very fast
            instance = null;
        }







        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        private string Instance_GetRawContent(string key, MissingKeySolution? solution)
        {
            MissingKeySolution finalSolution = solution ?? this.missingKeySolution;
            if (!loaded)
            {
                Debug.LogWarning($"L10n not initialize");
                return Instance_ResolveMissing(key, finalSolution);
            }

            bool hasValue = Dictionary.TryGetValue(key, out var entry);
            return Instance_ValidateValue(key, entry, hasValue, finalSolution);
        }

        /// <summary>
        /// Get the matched display language from dictionary by key
        /// </summary>
        /// <param name="key"></param> 
        /// <returns></returns>
        private string Instance_GetRawContent(Key key, MissingKeySolution? solution)
        {
            MissingKeySolution finalSolution = solution ?? this.missingKeySolution;
            if (!loaded)
            {
                Debug.LogWarning($"L10n not initialize");
                return Instance_ResolveMissing(key, finalSolution);
            }

            bool hasValue = Trie.TryGetValue(key.Section, out var entry);
            return Instance_ValidateValue(key, entry, hasValue, finalSolution);
        }



        private string Instance_ValidateValue(string key, TranslationEntry entry, bool hasValue, MissingKeySolution missingKeySolution)
        {
            if (string.IsNullOrEmpty(key) || !hasValue || entry.value == null)
            {
#if UNITY_EDITOR
                if (missing.Add(key))
                {
                    Debug.LogWarning($"Key {key} does not appear in the localization file {region}. The key will be added to localization manager if this happened in editor.");
                }
                OnKeyMissing?.Invoke(key);
#endif
                return Instance_ResolveMissing(key, missingKeySolution);
            }
            if (disableEmptyEntries && string.IsNullOrEmpty(entry.value))
            {
                Debug.LogWarning($"Key {key} has empty entry!");
                return Instance_ResolveMissing(key, missingKeySolution);
            }

            // late replace
            if (!entry.colorReplaced)
            {
                entry.value = EscapePattern.ReplaceColorEscape(entry.value);
                entry.colorReplaced = true;
                Dictionary[key] = entry;
                Trie[key] = entry;
            }
            return entry.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string Instance_ResolveMissing(string key, MissingKeySolution missingKeySolution)
        {
            string value;
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
                case MissingKeySolution.Fallback:
                    value = key;
                    break;
            }

            return value;
        }



        /// <summary>
        /// Direclty get the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetRawContent(Key key, MissingKeySolution? solution = null)
        {
            // localization not loaded
            if (instance == null)
            {
                return key;
            }
            // localization not initialized, no language pack loaded
            if (!instance.loaded)
            {
                return key;
            }

            return instance.Instance_GetRawContent(key, solution);
        }

        /// <summary>
        /// Direclty get the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetRawContent(string key, MissingKeySolution? solution = null)
        {
            // localization not loaded
            if (instance == null)
            {
                return key;
            }
            // localization not initialized, no language pack loaded
            if (!instance.loaded)
            {
                return key;
            }

            return instance.Instance_GetRawContent(key, solution);
        }




        /// <summary>
        /// Check whether given key is present in current localization file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Contains(string key)
        {
            return Instance?.Instance_Contains(key) == true;
        }

        public static bool Contains(Key key)
        {
            return Instance?.Instance_Contains(key) == true;
        }

        private bool Instance_Contains(string key)
        {
            return Dictionary.ContainsKey(key);
        }

        private bool Instance_Contains(Key key)
        {
            return Trie.ContainsKey(key.Section);
        }





        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(string key, string value)
        {
            if (instance == null || !instance.loaded)
            {
                return false;
            }

            instance.Dictionary[key] = value;
            instance.Trie[key] = value;
            return true;
        }

        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(Key key, string value)
        {
            if (instance == null || !instance.loaded)
            {
                return false;
            }

            instance.Dictionary[key] = value;
            instance.Trie[key.Section] = value;
            return true;
        }





        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static string[] OptionOf(string partialKey, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.loaded) { return Array.Empty<string>(); }
            instance.Instance_OptionOf(partialKey, out var result, firstLevelOnly);
            return result;
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static string[] OptionOf(Key partialKey, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.loaded) { return Array.Empty<string>(); }
            instance.Instance_OptionOf(partialKey, out var result, firstLevelOnly);
            return result;
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        private bool Instance_OptionOf(string partialKey, out string[] result, bool firstLevelOnly = false)
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
        private bool Instance_OptionOf(Key partialKey, out string[] result, bool firstLevelOnly = false)
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
        public static bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.loaded) { return false; }
            return instance.Instance_CopyOptions(partialKey, strings, firstLevelOnly);
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.loaded) { return false; }
            return instance.Instance_CopyOptions(partialKey, strings, firstLevelOnly);
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        private bool Instance_CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
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
        private bool Instance_CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
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






        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(string key, params string[] param) => Tr(key, instance?.missingKeySolution ?? 0, param);

        public static string Tr(string key, MissingKeySolution solution, params string[] param)
        {
            var fullKey = Localizable.AppendKey(key, param);
            var rawString = GetRawContent(fullKey, solution);
            rawString = EscapePattern.Escape(rawString, null, 0, param);
            OnTranslating?.Invoke(fullKey, ref rawString);
            return rawString;
        }

        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(Key key, params string[] param)
        {
            var fullKey = Localizable.AppendKey(key, param);
            var rawString = GetRawContent(fullKey);
            rawString = EscapePattern.Escape(rawString, null, 0, param);
            OnTranslating?.Invoke(fullKey, ref rawString);
            return rawString;
        }

        public static string Tr(object context, params string[] param)
        {
            switch (context)
            {
                case string str:
                    return Tr(str, param);
                case ILocalizableContext localizable:
                    return Tr(localizable, param);
                default:
                    return Tr(L10nContext.Of(context), param);
            }
        }

        /// <summary>
        /// Direct localization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, params string[] param)
        {
            var value = Localizable.Tr(context, 0, param);
            var key = context.GetLocalizationKey(param);
            OnTranslating?.Invoke(key, ref value);
            return value;
        }

        /// <summary>
        /// Direct localization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, params string[] param)
        {
            var value = Localizable.TrKey(key, context, 0, param);
            OnTranslating?.Invoke(key, ref value);
            return value;
        }

        /// <summary>
        /// Direct localization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrRaw(string rawContent, ILocalizableContext context, params string[] param)
        {
            var value = Localizable.TrRaw(rawContent, context, param);
            OnTranslating?.Invoke(string.Empty, ref value);
            return value;
        }
    }
}