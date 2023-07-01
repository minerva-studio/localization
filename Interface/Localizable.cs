using Minerva.Localizations.EscapePatterns;
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
        /// <param name="obj"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizable obj, params string[] param)
        {
            var rawString = obj.GetRawContent(param);
            rawString = EscapePattern.ReplaceKeyEscape(rawString);
            rawString = EscapePattern.ReplaceDynamicValueEscape(rawString, obj, param);
            rawString = EscapePattern.ReplaceColorEscape(rawString);
            return rawString;
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
            if (value == null) return string.Empty;
            // raw value, return value directly
            if (IsRawValue(value)) return value.ToString();

            if (value is ILocalizer localizer)
            {
                return localizer.Tr(param);
            }
            // custom content defined
            else if (IsL10nContentDefined(value, out var contentType))
            {
                return ((L10nContent)Activator.CreateInstance(contentType, value)).Tr(param);
            }
            else if (value is ILocalizable localizable)
            {
                return Tr(localizable, param);
            }
            // return unlocalized contents
            else return EscapePattern.AsKeyEscape(value.GetType().FullName);
        }

        /// <summary>
        /// Check whether the value is raw value to l10n
        /// </summary>
        /// <remarks> The raw values are all primitives and <see cref="decimal"/></remarks>
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

        public static bool IsL10nContentDefined(object value, out Type type)
        {
            return (type = CustomContentAttribute.GetContentType(value?.GetType())) != null;
        }
    }
}