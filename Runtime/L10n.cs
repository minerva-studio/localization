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
        internal const string DEFAULT_REGION = "EN_US";
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
        public static void Init() => Init(LocalizationSettings.GetOrCreateSettings().manager);

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
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void Load(string region)
        {
            Instance.Load(region);

            LanguageFile languageFile = manager.GetLanguageFile(region);
            wordSpace = languageFile.wordSpace;
            listDelimiter = languageFile.listDelimiter;

            OnLocalizationLoaded?.Invoke();
        }

        /// <summary>
        /// Init and Load given type of language
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
        public static void Reload()
        {
            instance.Reload();
            OnLocalizationLoaded?.Invoke();
        }

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
            return ValidateValue(key, result, solution ?? missingKeySolution);
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
            return ValidateValue(key, result, solution ?? missingKeySolution);
        }

        private static string ValidateValue(string key, string result, MissingKeySolution missingKeySolution)
        {
            if (string.IsNullOrEmpty(key) || result == null)
            {
                OnKeyMissing?.Invoke(key);
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
        public static bool Contains(string key) => Instance?.Contains(key, false) == true;

        /// <summary>
        /// Check whether given key is present in any localization file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exist(string key) => Instance?.Contains(key, true) == true;

        /// <summary>
        /// Check whether given key is present in current localization file (without fallback)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Contains(Key key) => Instance?.Contains(key, false) == true;

        /// <summary>
        /// Check whether given key is present in any localization file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exist(Key key) => Instance?.Contains(key, true) == true;




        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(string key, string value) => instance != null && instance.IsLoaded && instance.Write(key, value);

        /// <summary>
        /// Override given key's entry to value, written value will be lost if localization reloaded
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Write(Key key, string value) => instance != null && instance.IsLoaded && instance.Write(key, value);





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




        #region Translation API - New (L10nParams)

        /// <summary>
        /// Direct localization from key with L10nParams
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(string key, L10nParams parameters)
        {
            return Tr(key, missingKeySolution, parameters);
        }

        /// <summary>
        /// Direct localization from key with L10nParams and custom solution
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="solution">Missing key solution</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(string key, MissingKeySolution solution, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = GetRawContent(fullKey, solution);
            rawString = EscapePattern.Escape(rawString, null, parameters);
            OnTranslating?.Invoke(fullKey, ref rawString);
            return rawString;
        }

        /// <summary>
        /// Direct localization from Key with L10nParams
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(Key key, L10nParams parameters)
        {
            var fullKey = Key.Join(in key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            rawString = EscapePattern.Escape(rawString, null, parameters);
            OnTranslating?.Invoke(fullKey, ref rawString);
            return rawString;
        }

        /// <summary>
        /// Direct localization from object with L10nParams
        /// </summary>
        /// <param name="context">Object to localize</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(object context, L10nParams parameters)
        {
            switch (context)
            {
                case string str:
                    return Tr(str, parameters);
                case ILocalizableContext localizable:
                    return Tr(localizable, parameters);
                default:
                    return Tr(L10nContext.Of(context), parameters);
            }
        }

        /// <summary>
        /// Direct localization with L10nParams
        /// </summary>
        /// <param name="context">Localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, L10nParams parameters)
        {
            var value = Localizable.Tr(context, parameters);
            var key = context.GetLocalizationKey(parameters);
            OnTranslating?.Invoke(key, ref value);
            return value;
        }

        /// <summary>
        /// Direct localization with custom key and L10nParams
        /// </summary>
        /// <param name="key">Override localization key</param>
        /// <param name="context">Localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, L10nParams parameters)
        {
            var value = Localizable.TrKey(key, context, parameters);
            OnTranslating?.Invoke(key, ref value);
            return value;
        }

        /// <summary>
        /// Direct localization of raw content with L10nParams
        /// </summary>
        /// <param name="rawContent">Raw localization string</param>
        /// <param name="context">Localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string TrRaw(string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            var value = Localizable.TrRaw(rawContent, context, parameters);
            OnTranslating?.Invoke(string.Empty, ref value);
            return value;
        }

        #endregion

        #region Translation API - Legacy (string[])

        /// <summary>
        /// Direct localization from key (legacy)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(string key, params string[] param) => Tr(key, missingKeySolution, param);

        /// <summary>
        /// Direct localization from key with custom solution (legacy)
        /// </summary>
        public static string Tr(string key, MissingKeySolution solution, params string[] param)
        {
            return Tr(key, solution, L10nParams.FromStrings(param));
        }

        /// <summary>
        /// Direct localization from key (legacy)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(Key key, params string[] param)
        {
            return Tr(key, L10nParams.FromStrings(param));
        }

        /// <summary>
        /// Direct localization from object (legacy)
        /// </summary>
        public static string Tr(object context, params string[] param)
        {
            return Tr(context, L10nParams.FromStrings(param));
        }

        /// <summary>
        /// Direct localization (legacy)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, params string[] param)
        {
            return Tr(context, L10nParams.FromStrings(param));
        }

        /// <summary>
        /// Direct localization with custom key (legacy)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, params string[] param)
        {
            return TrKey(key, context, L10nParams.FromStrings(param));
        }

        /// <summary>
        /// Direct localization of raw content (legacy)
        /// </summary>
        /// <param name="rawContent"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrRaw(string rawContent, ILocalizableContext context, params string[] param)
        {
            return TrRaw(rawContent, context, L10nParams.FromStrings(param));
        }

        #endregion
    }
}