using Minerva.Localizations.EscapePatterns;

namespace Minerva.Localizations
{
    /// <summary>
    /// Provides localization operations bound to one loaded region.
    /// </summary>
    public sealed class RegionL10n
    {
        #region State

        private readonly L10nRuntime runtime;
        private readonly string region;

        public string Region => region;
        public L10nDataManager Manager => runtime.Manager;
        public string ListDelimiter => runtime.ForRegionData(region).ListDelimiter;
        public string WordSpace => runtime.ForRegionData(region).WordSpace;

        #endregion

        #region Construction

        /// <summary>
        /// Creates a region-bound translator from the shared runtime.
        /// </summary>
        internal RegionL10n(L10nRuntime runtime, string region)
        {
            this.runtime = runtime;
            this.region = region ?? string.Empty;
        }

        #endregion

        #region Raw Content

        /// <summary>
        /// Gets raw localized content from this region with shared fallback support.
        /// </summary>
        public string GetRawContent(string key, MissingKeySolution? solution = null)
        {
            var result = runtime.GetRawContent(region, key);
            return L10n.ValidateValue(key, result, solution ?? L10n.MissingKeySolution);
        }

        /// <summary>
        /// Gets raw localized content from this region with shared fallback support.
        /// </summary>
        public string GetRawContent(Key key, MissingKeySolution? solution = null)
        {
            var result = runtime.GetRawContent(region, key);
            return L10n.ValidateValue(key, result, solution ?? L10n.MissingKeySolution);
        }

        #endregion

        #region Key Lookup & Overrides

        /// <summary>
        /// Checks whether this region contains a localization key.
        /// </summary>
        public bool Contains(string key, bool includeFallback = false)
        {
            return runtime.Contains(region, key, includeFallback);
        }

        /// <summary>
        /// Checks whether this region contains a localization key.
        /// </summary>
        public bool Contains(Key key, bool includeFallback = false)
        {
            return runtime.Contains(region, key, includeFallback);
        }

        /// <summary>
        /// Writes an in-memory localization override for this region.
        /// </summary>
        public bool Write(string key, string value)
        {
            return runtime.Write(region, key, value);
        }

        /// <summary>
        /// Writes an in-memory localization override for this region.
        /// </summary>
        public bool Write(Key key, string value)
        {
            return runtime.Write(region, key, value);
        }

        #endregion

        #region Translation API

        /// <summary>
        /// Translates a localization key inside this region.
        /// </summary>
        public string Tr(string key, L10nParams parameters)
        {
            return Tr(key, L10n.MissingKeySolution, parameters);
        }

        /// <summary>
        /// Translates a localization key inside this region with a custom missing-key policy.
        /// </summary>
        public string Tr(string key, MissingKeySolution solution, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = GetRawContent(fullKey, solution);
            using (L10n.UseRegionContext(this))
            {
                rawString = EscapePattern.Escape(rawString, null, parameters);
            }
            L10n.InvokeOnTranslating(fullKey, ref rawString);
            return rawString;
        }

        /// <summary>
        /// Translates a localization key inside this region.
        /// </summary>
        public string Tr(Key key, L10nParams parameters)
        {
            var fullKey = Key.Join(in key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            using (L10n.UseRegionContext(this))
            {
                rawString = EscapePattern.Escape(rawString, null, parameters);
            }
            L10n.InvokeOnTranslating(fullKey, ref rawString);
            return rawString;
        }

        /// <summary>
        /// Translates a localizable object inside this region.
        /// </summary>
        public string Tr(object context, L10nParams parameters)
        {
            return context switch
            {
                string str => Tr(str, parameters),
                ILocalizableContext localizable => Tr(localizable, parameters),
                _ => Tr(L10nContext.Of(context), parameters),
            };
        }

        /// <summary>
        /// Translates a localizable context inside this region.
        /// </summary>
        public string Tr(ILocalizableContext context, L10nParams parameters)
        {
            string value;
            using (L10n.UseRegionContext(this))
            {
                value = Localizable.Tr(context, parameters);
            }
            var key = context.GetLocalizationKey(parameters);
            L10n.InvokeOnTranslating(key, ref value);
            return value;
        }

        /// <summary>
        /// Translates a localizable context with an override key inside this region.
        /// </summary>
        public string TrKey(string key, ILocalizableContext context, L10nParams parameters)
        {
            string value;
            using (L10n.UseRegionContext(this))
            {
                value = Localizable.TrKey(key, context, parameters);
            }
            L10n.InvokeOnTranslating(key, ref value);
            return value;
        }

        /// <summary>
        /// Translates raw localization content inside this region.
        /// </summary>
        public string TrRaw(string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            string value;
            using (L10n.UseRegionContext(this))
            {
                value = Localizable.TrRaw(rawContent, context, parameters);
            }
            L10n.InvokeOnTranslating(string.Empty, ref value);
            return value;
        }

        /// <summary>
        /// Translates raw localization content and returns parser diagnostics.
        /// </summary>
        public L10nTranslationResult TryTrRaw(string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            L10nTranslationResult translationResult;
            using (L10n.UseRegionContext(this))
            {
                translationResult = EscapePattern.TryEscape(rawContent, context, parameters);
            }
            var result = translationResult.TranslatedText;
            L10n.InvokeOnTranslating(string.Empty, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        /// <summary>
        /// Translates a localization key and returns parser diagnostics.
        /// </summary>
        public L10nTranslationResult TryTr(string key, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            L10nTranslationResult translationResult;
            using (L10n.UseRegionContext(this))
            {
                translationResult = EscapePattern.TryEscape(rawString, null, parameters);
            }
            var result = translationResult.TranslatedText;
            L10n.InvokeOnTranslating(fullKey, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        /// <summary>
        /// Translates a localization key and returns parser diagnostics.
        /// </summary>
        public L10nTranslationResult TryTr(Key key, L10nParams parameters)
        {
            var fullKey = Key.Join(in key, parameters.Options);
            var rawString = GetRawContent(fullKey);
            L10nTranslationResult translationResult;
            using (L10n.UseRegionContext(this))
            {
                translationResult = EscapePattern.TryEscape(rawString, null, parameters);
            }
            var result = translationResult.TranslatedText;
            L10n.InvokeOnTranslating(fullKey, ref result);
            translationResult.TranslatedText = result;
            return translationResult;
        }

        #endregion

        #region Legacy API

        /// <summary>
        /// Translates a localization key inside this region.
        /// </summary>
        public string Tr(string key, params string[] param) => Tr(key, L10nParams.FromStrings(param));

        /// <summary>
        /// Translates a localization key inside this region with a custom missing-key policy.
        /// </summary>
        public string Tr(string key, MissingKeySolution solution, params string[] param) => Tr(key, solution, L10nParams.FromStrings(param));

        /// <summary>
        /// Translates a localization key inside this region.
        /// </summary>
        public string Tr(Key key, params string[] param) => Tr(key, L10nParams.FromStrings(param));

        /// <summary>
        /// Translates raw localization content inside this region.
        /// </summary>
        public string TrRaw(string rawContent, ILocalizableContext context, params string[] param) => TrRaw(rawContent, context, L10nParams.FromStrings(param));

        #endregion
    }
}
