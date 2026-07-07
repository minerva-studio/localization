using Minerva.Localizations.EscapePatterns;
using Minerva.Localizations.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
        public static event Action<string> OnRegionLoaded;
        public static event Action<string> OnRegionUnloaded;
        public static event Action<string> OnMainRegionChanged;

        private static L10nDataManager manager;
        private static IL10nHandler instance;
        private static readonly AsyncLocal<RegionL10n> scopedRegionL10n = new();
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
        /// <summary> Main region used by legacy translation APIs. </summary>
        public static string MainRegion => Region;
        /// <summary> Whether a non-fallback main region is currently selected. </summary>
        public static bool HasMainRegion => GetRuntime()?.HasMainRegion == true;
        /// <summary> Regions </summary>
        public static string[] Regions => instance?.Manager != null ? instance.Manager.regions.ToArray() : Array.Empty<string>();
        /// <summary> Non-fallback regions currently loaded in memory. </summary>
        public static string[] LoadedRegions => GetRuntime()?.LoadedRegions ?? Array.Empty<string>();
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
        public static string ListDelimiter => scopedRegionL10n.Value?.ListDelimiter ?? listDelimiter ?? string.Empty;
        public static string WordSpace => scopedRegionL10n.Value?.WordSpace ?? wordSpace ?? string.Empty;


        #region Lifecycle

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
        /// Init and Load given type of language
        /// </summary>
        /// <param name="manager">The localization Data Manager</param>
        /// <param name="region">Region to set</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void InitAndLoad(L10nDataManager manager, string region)
        {
            Init(manager);
            Load(region, true);
        }

        public static void ReloadIfInitialized()
        {
            if (instance == null || !instance.IsLoaded) return;
            Reload();
        }

        /// <summary>
        /// Set the custom l10n handler
        /// </summary>
        /// <param name="handler"></param>
        public static void SetHandler(IL10nHandler handler)
        {
            instance = handler;
            if (manager)
            {
                Instance.Init(manager);
            }
        }

        /// <summary>
        /// Deinitialize current loaded localization
        /// </summary>
        public static void DeInitialize()
        {
            Debug.Log("deinit");
            // Clear static runtime state so the next load starts from a fresh manager.
            instance = null;
            manager = null;
            scopedRegionL10n.Value = null;
        }

        #endregion

        #region Region Loading

        /// <summary>
        /// Loads a region as background data unless no main region exists yet.
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void Load(string region) => Load(region, false);

        /// <summary>
        /// Loads a region and optionally selects it as the main region.
        /// </summary>
        /// <param name="region">Region to load.</param>
        /// <param name="asMainRegion">Whether the loaded region should become the main region.</param>
        public static void Load(string region, bool asMainRegion)
        {
            if (Instance is L10nHandler handler)
            {
                var result = handler.Load(region, asMainRegion);
                if (result.RegionLoaded)
                {
                    OnRegionLoaded?.Invoke(result.Region);
                }
                if (result.MainRegionChanged)
                {
                    ApplyMainRegionSettings();
                    OnMainRegionChanged?.Invoke(Region);
                    OnLocalizationLoaded?.Invoke();
                }
                return;
            }

            // Custom handlers keep legacy behavior because they do not expose multi-region state.
            Instance.Load(region);
            OnLocalizationLoaded?.Invoke();
        }

        /// <summary>
        /// reload current localization
        /// </summary>
        public static void Reload()
        {
            if (GetRuntime() != null)
            {
                if (GetRuntime().ReloadMain())
                {
                    ApplyMainRegionSettings();
                    OnLocalizationLoaded?.Invoke();
                }
                return;
            }

            instance?.Reload();
            OnLocalizationLoaded?.Invoke();
        }

        /// <summary>
        /// Reloads a specific loaded region.
        /// </summary>
        /// <param name="region">Region to reload.</param>
        /// <returns>Whether the region was reloaded.</returns>
        public static bool ReloadRegion(string region)
        {
            var runtime = RequireRuntime();
            bool reloaded = runtime.ReloadRegion(region);
            if (reloaded && runtime.MainRegion == region)
            {
                ApplyMainRegionSettings();
                OnLocalizationLoaded?.Invoke();
            }
            return reloaded;
        }

        /// <summary>
        /// Reloads fallback and all loaded non-fallback regions.
        /// </summary>
        public static void ReloadAllLoadedRegions()
        {
            var runtime = RequireRuntime();
            runtime.ReloadAllLoadedRegions();
            ApplyMainRegionSettings();
            OnLocalizationLoaded?.Invoke();
        }

        /// <summary>
        /// Selects a loaded non-fallback region as the main region.
        /// </summary>
        /// <param name="region">Region to make the main region.</param>
        /// <returns>Whether the main region changed.</returns>
        public static bool SetMainRegion(string region)
        {
            var runtime = RequireRuntime();
            bool changed = runtime.SetMainRegion(region);
            if (changed)
            {
                ApplyMainRegionSettings();
                OnMainRegionChanged?.Invoke(Region);
                OnLocalizationLoaded?.Invoke();
            }
            return changed;
        }

        /// <summary>
        /// Unloads a non-main, non-fallback region.
        /// </summary>
        /// <param name="region">Region to unload.</param>
        /// <returns>Whether the region was unloaded.</returns>
        public static bool Unload(string region)
        {
            var runtime = RequireRuntime();
            bool unloaded = runtime.Unload(region);
            if (unloaded)
            {
                OnRegionUnloaded?.Invoke(region);
            }
            return unloaded;
        }

        #endregion

        #region Raw Content Lookup

        /// <summary>
        /// Direclty get the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetRawContent(Key key, MissingKeySolution? solution = null)
        {
            if (scopedRegionL10n.Value != null)
            {
                return scopedRegionL10n.Value.GetRawContent(key, solution);
            }

            // localization not loaded
            if (instance == null)
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
            if (scopedRegionL10n.Value != null)
            {
                return scopedRegionL10n.Value.GetRawContent(key, solution);
            }

            // localization not loaded
            if (instance == null)
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
        /// Directly gets the value by key from the default localization region.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="solution">The optional missing-key resolution policy.</param>
        /// <returns>The default-region raw content when available.</returns>
        internal static string GetDefaultRawContent(Key key, MissingKeySolution? solution = null)
        {
            if (instance == null)
            {
                return key;
            }

            var result = instance.GetDefaultRawContent(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return ValidateValue(key, result, solution ?? missingKeySolution);
        }

        /// <summary>
        /// Directly gets the value by key from the default localization region.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="solution">The optional missing-key resolution policy.</param>
        /// <returns>The default-region raw content when available.</returns>
        internal static string GetDefaultRawContent(string key, MissingKeySolution? solution = null)
        {
            if (instance == null)
            {
                return key;
            }

            var result = instance.GetDefaultRawContent(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return ValidateValue(key, result, solution ?? missingKeySolution);
        }

        #endregion

        #region Key Lookup & Overrides

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

        #endregion

        #region Region Translation API

        /// <summary>
        /// Gets a region-bound translator.
        /// </summary>
        /// <param name="region">Region to translate with.</param>
        /// <returns>A region-bound translator.</returns>
        public static RegionL10n ForRegion(string region)
        {
            return RequireRuntime().ForRegion(region);
        }

        /// <summary>
        /// Enters a temporary region context for static localization lookups.
        /// </summary>
        /// <param name="region">Region to use until the returned scope is disposed.</param>
        /// <returns>A scope that restores the previous region context on dispose.</returns>
        public static IDisposable InRegion(string region)
        {
            return UseRegionContext(ForRegion(region));
        }

        /// <summary>
        /// Checks whether a region is loaded or is the shared fallback.
        /// </summary>
        public static bool IsRegionLoaded(string region)
        {
            return RequireRuntime().IsRegionLoaded(region);
        }

        /// <summary>
        /// Gets raw content through an explicit region.
        /// </summary>
        public static string GetRawContentIn(string region, string key, MissingKeySolution? solution = null)
        {
            return ForRegion(region).GetRawContent(key, solution);
        }

        /// <summary>
        /// Gets raw content through an explicit region.
        /// </summary>
        public static string GetRawContentIn(string region, Key key, MissingKeySolution? solution = null)
        {
            return ForRegion(region).GetRawContent(key, solution);
        }

        /// <summary>
        /// Translates a key through an explicit region.
        /// </summary>
        public static string TrIn(string region, string key, L10nParams parameters)
        {
            return ForRegion(region).Tr(key, parameters);
        }

        /// <summary>
        /// Translates a key through an explicit region with a custom missing-key policy.
        /// </summary>
        public static string TrIn(string region, string key, MissingKeySolution solution, L10nParams parameters)
        {
            return ForRegion(region).Tr(key, solution, parameters);
        }

        /// <summary>
        /// Translates a key through an explicit region.
        /// </summary>
        public static string TrIn(string region, Key key, L10nParams parameters)
        {
            return ForRegion(region).Tr(key, parameters);
        }

        /// <summary>
        /// Translates a localizable context through an explicit region.
        /// </summary>
        public static string TrIn(string region, ILocalizableContext context, L10nParams parameters)
        {
            return ForRegion(region).Tr(context, parameters);
        }

        /// <summary>
        /// Translates a localizable context with an override key through an explicit region.
        /// </summary>
        public static string TrKeyIn(string region, string key, ILocalizableContext context, L10nParams parameters)
        {
            return ForRegion(region).TrKey(key, context, parameters);
        }

        /// <summary>
        /// Translates raw content through an explicit region.
        /// </summary>
        public static string TrRawIn(string region, string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            return ForRegion(region).TrRaw(rawContent, context, parameters);
        }

        /// <summary>
        /// Translates a key through an explicit region and returns parser diagnostics.
        /// </summary>
        public static L10nTranslationResult TryTrIn(string region, string key, L10nParams parameters)
        {
            return ForRegion(region).TryTr(key, parameters);
        }

        /// <summary>
        /// Translates a key through an explicit region and returns parser diagnostics.
        /// </summary>
        public static L10nTranslationResult TryTrIn(string region, Key key, L10nParams parameters)
        {
            return ForRegion(region).TryTr(key, parameters);
        }

        /// <summary>
        /// Translates a localizable context through an explicit region and returns parser diagnostics.
        /// </summary>
        public static L10nTranslationResult TryTrIn(string region, ILocalizableContext context, L10nParams parameters)
        {
            return ForRegion(region).TryTr(context, parameters);
        }

        /// <summary>
        /// Translates raw content through an explicit region and returns parser diagnostics.
        /// </summary>
        public static L10nTranslationResult TryTrRawIn(string region, string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            return ForRegion(region).TryTrRaw(rawContent, context, parameters);
        }

        /// <summary>
        /// Translates a key through an explicit region.
        /// </summary>
        public static string TrIn(string region, string key, params string[] param)
        {
            return ForRegion(region).Tr(key, param);
        }

        #endregion

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

        public static L10nTranslationResult TryTrRaw(string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            var translationResult = EscapePattern.TryEscape(rawContent, context, parameters);
            var result = translationResult.TranslatedText;
            OnTranslating?.Invoke(string.Empty, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        /// <summary>
        /// Try Direct localization from key with L10nParams
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static L10nTranslationResult TryTr(string key, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            var translationResult = EscapePattern.TryEscape(rawString, null, parameters);
            var result = translationResult.TranslatedText;
            OnTranslating?.Invoke(fullKey, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        /// <summary>
        /// Try Direct localization from key with L10nParams
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static L10nTranslationResult TryTr(Key key, L10nParams parameters)
        {
            var fullKey = Key.Join(in key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            var translationResult = EscapePattern.TryEscape(rawString, null, parameters);
            var result = translationResult.TranslatedText;
            OnTranslating?.Invoke(fullKey, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        #endregion

        #region Default Region Translation API

        /// <summary>
        /// Direct localization from the default region with L10nParams.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="parameters">Localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(string key, L10nParams parameters) => TrDefault(key, missingKeySolution, parameters);

        /// <summary>
        /// Direct localization from the default region with L10nParams and custom solution.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="solution">Missing-key solution.</param>
        /// <param name="parameters">Localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(string key, MissingKeySolution solution, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = GetDefaultRawContent(fullKey, solution);
            return EscapePattern.Escape(rawString, null, parameters);
        }

        /// <summary>
        /// Direct localization from the default region with L10nParams.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="parameters">Localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(Key key, L10nParams parameters)
        {
            var fullKey = Key.Join(in key, parameters.Options);
            var rawString = GetDefaultRawContent(fullKey);
            return EscapePattern.Escape(rawString, null, parameters);
        }

        /// <summary>
        /// Direct localization from the default region.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="param">Legacy localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(string key, params string[] param) => TrDefault(key, missingKeySolution, L10nParams.FromStrings(param));

        /// <summary>
        /// Direct localization from the default region with custom solution.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="solution">Missing-key solution.</param>
        /// <param name="param">Legacy localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(string key, MissingKeySolution solution, params string[] param) => TrDefault(key, solution, L10nParams.FromStrings(param));

        /// <summary>
        /// Direct localization from the default region.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <param name="param">Legacy localization parameters.</param>
        /// <returns>The default-region localized value.</returns>
        public static string TrDefault(Key key, params string[] param) => TrDefault(key, L10nParams.FromStrings(param));

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

        #region Internal Helpers

        /// <summary>
        /// Uses a region translator for nested static localization lookups.
        /// </summary>
        internal static IDisposable UseRegionContext(RegionL10n context)
        {
            var previous = scopedRegionL10n.Value;
            scopedRegionL10n.Value = context;
            return new RegionL10nScope(previous);
        }

        /// <summary>
        /// Invokes the translating hook for region-bound APIs.
        /// </summary>
        internal static void InvokeOnTranslating(string key, ref string value)
        {
            OnTranslating?.Invoke(key, ref value);
        }

        /// <summary>
        /// Validates raw lookup results against the active missing-key policy.
        /// </summary>
        internal static string ValidateValue(string key, string result, MissingKeySolution missingKeySolution)
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

        /// <summary>
        /// Resolves a missing localization key into the configured display value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string ResolveMissing(string key, MissingKeySolution missingKeySolution)
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

        private static L10nRuntime GetRuntime()
        {
            return (instance as L10nHandler)?.Runtime;
        }

        private static L10nRuntime RequireRuntime()
        {
            if (GetRuntime() != null)
            {
                return GetRuntime();
            }

            if (!manager)
            {
                Init();
            }
            else
            {
                Instance.Init(manager);
            }

            return GetRuntime() ?? throw new NullReferenceException("The localization manager has not yet initialized.");
        }

        private static void ApplyMainRegionSettings()
        {
            if (GetRuntime()?.MainData == null)
            {
                return;
            }

            wordSpace = GetRuntime().MainData.WordSpace;
            listDelimiter = GetRuntime().MainData.ListDelimiter;
        }

        private sealed class RegionL10nScope : IDisposable
        {
            private readonly RegionL10n previous;
            private bool disposed;

            /// <summary>
            /// Stores the previous scoped translator so it can be restored.
            /// </summary>
            public RegionL10nScope(RegionL10n previous)
            {
                this.previous = previous;
            }

            /// <summary>
            /// Restores the previous scoped localization translator.
            /// </summary>
            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                // Dispose is intentionally idempotent so a stale nested scope cannot restore over a newer context.
                disposed = true;
                scopedRegionL10n.Value = previous;
            }
        }

        #endregion
    }
}
