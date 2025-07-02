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



        public static event Action OnLocalizationLoaded;
        public static event Action<string> OnKeyMissing;
        public static event OnTranslating OnTranslating;

        private static L10nDataManager manager;
        private static IL10nHandler instance;
        private static bool disableEmptyEntries;
        private static ReferenceImportOption tooltipImportOption;
        private static ReferenceImportOption referenceImportOption;
        private static UnderlineResolverOption useUnderlineResolver;
        private static MissingKeySolution missingKeySolution;
        private static string wordSpace;
        private static string listDelimiter;


        /// <summary> instance of localization model </summary>
        public static IL10nHandler Instance => instance ??= new L10nHandler();
        /// <summary> manager </summary>
        public static L10nDataManager Manager => instance?.Manager;
        /// <summary> whether any localization is loaded </summary>
        public static bool IsLoaded => instance?.IsLoaded == true;
        /// <summary> whether a manager is provided </summary>
        public static bool IsInitialized => instance?.Manager != null;
        /// <summary> Region </summary>
        public static string Region => instance?.Region ?? string.Empty;
        /// <summary> Regions </summary>
        public static string[] Regions => instance?.Manager != null ? instance.Manager.regions.ToArray() : Array.Empty<string>();
        /// <summary> Should discard empty entries? </summary>
        public static bool DisableEmptyEntries { get { return disableEmptyEntries; } set { disableEmptyEntries = value; } }
        /// <summary> Missing key solution </summary>
        public static MissingKeySolution MissingKeySolution { get { return missingKeySolution; } set { missingKeySolution = value; } }
        /// <summary> Tooltip Import Option </summary>
        public static ReferenceImportOption TooltipImportOption { get { return tooltipImportOption; } set { tooltipImportOption = value; } }
        /// <summary> Reference Import Option </summary>
        public static ReferenceImportOption ReferenceImportOption { get { return referenceImportOption; } set { referenceImportOption = value; } }
        /// <summary> Underline resolver to fix the tag conflict between content </summary>
        public static UnderlineResolverOption UseUnderlineResolver { get { return useUnderlineResolver; } set { useUnderlineResolver = value; } }
        public static string ListDelimiter => listDelimiter ?? string.Empty;
        public static string WordSpace => wordSpace ?? string.Empty;






        /// <summary>
        /// Init L10n
        /// </summary>
        /// <param name="manager"></param>
        public static void Init(L10nDataManager manager)
        {
            L10n.manager = manager;
            disableEmptyEntries = manager.disableEmptyEntry;
            missingKeySolution = manager.missingKeySolution;
            tooltipImportOption = manager.tooltipImportOption;
            referenceImportOption = manager.referenceImportOption;
            useUnderlineResolver = manager.useUnderlineResolver;
            Instance.Init(manager);
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
            Instance.Load(region);

            LanguageFile languageFile = manager.GetLanguageFile(region);
            wordSpace = languageFile.wordSpace;
            listDelimiter = languageFile.listDelimiter;
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

        public static void ReloadIfInitialized()
        {
            if (instance == null || !instance.IsLoaded) return;
            Reload();
        }

        /// <summary>
        /// reload current localization
        /// </summary>
        public static void Reload() => instance.Reload();

        /// <summary>
        /// Set the custom l10n handler
        /// </summary>
        /// <param name="handler"></param>
        public static void SetHandler(IL10nHandler handler)
        {
            instance = handler;
            Instance.Init(manager);
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
            if (!instance.IsLoaded)
            {
                return key;
            }

            var result = instance.GetRawContent(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            return ValidateValue(key, result, missingKeySolution);
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
            if (!instance.IsLoaded)
            {
                return key;
            }

            var result = instance.GetRawContent(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            return ValidateValue(key, result, missingKeySolution);
        }

        private static string ValidateValue(string key, string result, MissingKeySolution missingKeySolution)
        {
            if (string.IsNullOrEmpty(key) || result == null)
            {
                Debug.LogWarning($"Key {key} does not appear in the localization file {Instance.Region}. The key will be added to localization manager if this happened in editor.");
                return ResolveMissing(key, missingKeySolution);
            }
            if (disableEmptyEntries && string.IsNullOrEmpty(result))
            {
                Debug.LogWarning($"Key {key} has empty entry!");
                return ResolveMissing(key, missingKeySolution);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ResolveMissing(string key, MissingKeySolution missingKeySolution)
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
        /// Check whether given key is present in current localization file (without fallback)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Contains(string key)
        {
            return Instance?.Contains(key, false) == true;
        }

        /// <summary>
        /// Check whether given key is present in any localization file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exist(string key)
        {
            return Instance?.Contains(key, true) == true;
        }

        /// <summary>
        /// Check whether given key is present in current localization file (without fallback)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Contains(Key key)
        {
            return Instance?.Contains(key, false) == true;
        }

        /// <summary>
        /// Check whether given key is present in any localization file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exist(Key key)
        {
            return Instance?.Contains(key, true) == true;
        }




        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(string key, string value)
        {
            if (instance == null || !instance.IsLoaded)
            {
                return false;
            }

            return instance.Write(key, value);
        }

        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(Key key, string value)
        {
            if (instance == null || !instance.IsLoaded)
            {
                return false;
            }

            return instance.Write(key, value);
        }





        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static string[] OptionOf(string partialKey, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.IsLoaded) { return Array.Empty<string>(); }
            instance.OptionOf(partialKey, out var result, firstLevelOnly);
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
            if (instance == null || !instance.IsLoaded) { return Array.Empty<string>(); }
            instance.OptionOf(partialKey, out var result, firstLevelOnly);
            return result;
        }




        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.IsLoaded) { return false; }
            return instance.CopyOptions(partialKey, strings, firstLevelOnly);
        }

        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public static bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            if (instance == null || !instance.IsLoaded) { return false; }
            return instance.CopyOptions(partialKey, strings, firstLevelOnly);
        }





        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(string key, params string[] param) => Tr(key, missingKeySolution, param);

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