using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Minerva.Localizations.EscapePatterns.EscapePattern;

namespace Minerva.Localizations
{
    /// <summary>
    /// Extensions for localizable interface
    /// </summary>
    internal static class Localizable
    {
        #region New API (L10nParams)

        /// <summary>
        /// Localize a localizable object with L10nParams
        /// </summary>
        /// <param name="context">The localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, L10nParams parameters)
        {
            var rawString = context.GetRawContent(parameters);
            return Escape(rawString, context, parameters.IncreaseDepth());
        }

        /// <summary>
        /// Localize a localizable object with custom key and L10nParams
        /// </summary>
        /// <param name="key">Override localization key</param>
        /// <param name="context">The localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, L10nParams parameters)
        {
            var rawString = context.GetRawContentWithKey(key, parameters);
            return Escape(rawString, context, parameters.IncreaseDepth());
        }

        /// <summary>
        /// Localize raw content with L10nParams
        /// </summary>
        /// <param name="rawContent">Raw localization string</param>
        /// <param name="context">The localization context</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string TrRaw(string rawContent, ILocalizableContext context, L10nParams parameters)
        {
            return Escape(rawContent, context, parameters);
        }

        /// <summary>
        /// Translating any value with L10nParams
        /// </summary>
        /// <param name="value">Value to translate</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        public static string Tr(object value, L10nParams parameters)
        {
            // null value, return empty string
            if (value == null)
            {
                return string.Empty;
            }

            // Check recursion depth
            if (!LoopCheck(parameters.Depth, value))
            {
                return value.ToString();
            }

            // Try format as number
            if (TryFormatNumber(value, out var result, ""))
            {
                return result;
            }

            // raw value, return value directly
            if (IsRawValue(value))
            {
                return value.ToString();
            }

            // list of items, return list with list delimiter
            if (value is IList list)
            {
                return string.Join(L10n.ListDelimiter, Iterate());
                IEnumerable<string> Iterate()
                {
                    foreach (var item in list)
                    {
                        yield return Tr(item, parameters.IncreaseDepth());
                    }
                }
            }

            // is localizer 
            if (value is ILocalizer localizer)
            {
                return localizer.Tr(parameters.ToLegacy());
            }

            // is localizable 
            if (value is ILocalizableContext localizable)
            {
                return Tr(localizable, parameters.IncreaseDepth());
            }

            // custom content defined
            if (IsL10nContextDefined(value))
            {
                var context = L10nContext.Of(value);
                return Tr(context, parameters.IncreaseDepth());
            }

            // return unlocalized contents
            string fullName = value?.GetType().FullName;
            Debug.LogWarning("Unhandled object type in localization: " + fullName);
            return value?.ToString();
        }

        #endregion

        #region Legacy API (string[])

        /// <summary>
        /// Localize a localizable object with params (legacy)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="depth">Recursion depth</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, int depth, params string[] param)
        {
            var parameters = L10nParams.FromLegacy(param, depth);
            return Tr(context, parameters);
        }

        /// <summary>
        /// Localize a localizable object with params (legacy)
        /// </summary>
        /// <param name="key">Override key</param>
        /// <param name="context"></param>
        /// <param name="depth">Recursion depth</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, int depth, params string[] param)
        {
            var parameters = L10nParams.FromLegacy(param, depth);
            return TrKey(key, context, parameters);
        }

        /// <summary>
        /// Localize a localizable object with params (legacy)
        /// </summary>
        /// <param name="rawContent">Raw content</param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrRaw(string rawContent, ILocalizableContext context, params string[] param)
        {
            var parameters = L10nParams.FromLegacy(param);
            return TrRaw(rawContent, context, parameters);
        }

        /// <summary>
        /// Translating any value (legacy)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="depth">Recursion depth</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(object value, int depth, params string[] param)
        {
            var parameters = L10nParams.FromLegacy(param, depth);
            return Tr(value, parameters);
        }

        #endregion

        #region Key Helpers

        /// <summary>
        /// Append given key with option
        /// </summary>
        /// <param name="baseKey">Base key</param>
        /// <param name="option">Option to append</param>
        /// <returns></returns>
        public static string AppendKey(string baseKey, string option)
        {
            if (string.IsNullOrEmpty(option))
                return baseKey;

            if (Key.VALID_KEY_MEMBER.IsMatch(option))
                return baseKey + L10nSymbols.KEY_SEPARATOR + option;

            return baseKey;
        }

        /// <summary>
        /// Append given key (legacy)
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string AppendKey(string baseKey, params string[] param) => AppendKey(baseKey, (IReadOnlyList<string>)param);

        /// <summary>
        /// Append given key (legacy)
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string AppendKey(string baseKey, IReadOnlyList<string> param)
        {
            var extensions = param.Where(p => Key.VALID_KEY_MEMBER.IsMatch(p)).Prepend(baseKey);
            return string.Join(L10nSymbols.KEY_SEPARATOR, extensions);
        }

        /// <summary>
        /// Append given key with option
        /// </summary>
        /// <param name="baseKey">Base key</param>
        /// <param name="option">Option to append</param>
        /// <returns></returns>
        public static Key AppendKey(Key baseKey, string option)
        {
            if (!string.IsNullOrEmpty(option) && Key.VALID_KEY_MEMBER.IsMatch(option))
                baseKey += option;
            return baseKey;
        }

        /// <summary>
        /// Append given key (legacy)
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Key AppendKey(Key baseKey, params string[] param)
        {
            foreach (var item in param)
            {
                if (Key.VALID_KEY_MEMBER.IsMatch(item))
                    baseKey += item;
            }
            return baseKey;
        }

        #endregion

        #region Context Helpers

        /// <summary>
        /// Check if L10nContext is defined for given object type
        /// </summary>
        public static bool IsL10nContextDefined(object value)
        {
            return ContextTable.HasContextTypeDefined(value?.GetType());
        }

        #endregion
    }
}