using static Minerva.Localizations.EscapePatterns.EscapePattern;
using System;
using System.Linq;

namespace Minerva.Localizations
{
    /// <summary>
    /// Extensions for localizable interface
    /// </summary>
    internal static class Localizable
    {
        /// <summary>
        /// Localize a localizable object with params
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizable context, params string[] param)
        {
            var rawString = context.GetRawContent(param);
            return Escape(rawString, context, param);
        }

        /// <summary>
        /// Localize a localizable object with params
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizable context, params string[] param)
        {
            var rawString = context.GetRawContentWithKey(key, param);
            return Escape(rawString, context, param);
        }

        /// <summary>
        /// Translating any value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(object value, params string[] param)
        {
            // null value, return emtpy string
            if (value == null)
            {
                return string.Empty;
            }
            // raw value, return value directly
            if (IsRawValue(value))
            {
                return value.ToString();
            }

            // is localizer 
            if (value is ILocalizer localizer)
            {
                return localizer.Tr(param);
            }
            // is localizable 
            if (value is ILocalizable localizable)
            {
                return Tr(localizable, param);
            }
            // custom content defined
            if (IsL10nContextDefined(value, out var contentType))
            {
                return ((L10nContext)Activator.CreateInstance(contentType, value)).Tr(param);
            }

            // return unlocalized contents
            return AsKeyEscape(value.GetType().FullName);
        }

        /// <summary>
        /// Check whether the value is raw value to l10n
        /// </summary>
        /// <remarks> The raw values are all primitives and <see cref="decimal"/> <br/>
        /// Examples: <br/>
        /// - <see cref="string"/> <br/>
        /// - <see cref="int"/> <br/>
        /// - <see cref="long"/> <br/>
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsRawValue(object value)
        {
            return value is string || value is decimal || value.GetType().IsPrimitive && value is not bool and not char and not IntPtr and not UIntPtr;
        }

        /// <summary>
        /// append given key
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string AppendKey(string baseKey, params string[] param)
        {
            var extensions = param.Where(p => !p.Contains('=') && !string.IsNullOrEmpty(p)).Prepend(baseKey);
            return string.Join(L10nSymbols.KEY_SEPARATOR, extensions);
        }

        public static bool IsL10nContextDefined(object value, out Type contextType)
        {
            return CustomContextAttribute.HasContextTypeDefined(value?.GetType(), out contextType);
        }
    }
}