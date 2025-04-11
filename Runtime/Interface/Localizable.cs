using System;
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
        /// <summary>
        /// Localize a localizable object with params
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(ILocalizableContext context, int depth, params string[] param)
        {
            var rawString = context.GetRawContent(param);
            return Escape(rawString, context, depth + 1, param);
        }

        /// <summary>
        /// Localize a localizable object with params
        /// </summary>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string TrKey(string key, ILocalizableContext context, int depth, params string[] param)
        {
            var rawString = context.GetRawContentWithKey(key, param);
            return Escape(rawString, context, depth + 1, param);
        }

        /// <summary>
        /// Translating any value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Tr(object value, int depth, params string[] param)
        {
            // null value, return emtpy string
            if (value == null)
            {
                return string.Empty;
            }
            if (!LoopCheck(depth, value.ToString()))
            {
                return value.ToString();
            }
            if (IsNumber(value))
            {
                return NumberToString(value, param);
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
                        yield return Tr(item, depth + 1, param);
                    }
                }
            }

            // is localizer 
            if (value is ILocalizer localizer)
            {
                return localizer.Tr(param);
            }
            // is localizable 
            if (value is ILocalizableContext localizable)
            {
                return Tr(localizable, depth + 1, param);
            }
            // custom content defined
            if (IsL10nContextDefined(value))
            {
                var context = L10nContext.Of(value);
                return Tr(context, depth + 1, param);
            }
            // return unlocalized contents
            string fullName = value?.GetType().FullName;
            Debug.LogWarning("Unhandled object type in localization: " + fullName);
            return value?.ToString();
        }

        public static bool IsNumber(object value)
        {
            return value is int or float or double or decimal or long or short;
        }

        /// <summary>
        /// use variable argument to format numbers to string
        /// </summary>
        /// <param name="numberLike"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string NumberToString(object numberLike, params string[] param)
        {
            var number = AsNumber(numberLike);
            foreach (var item in param)
            {
                if (item.StartsWith("f_"))
                {
                    return number.ToString(item[2..]);
                }
            }
            switch (numberLike)
            {
                case int:
                case long:
                case short:
                    return numberLike.ToString();
                default:
                    return number.ToString("F1");
            }
        }

        /// <summary>
        /// append given key
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string AppendKey(string baseKey, params string[] param)
        {
            var extensions = param.Where(p => Key.VALID_KEY_MEMBER.IsMatch(p)).Prepend(baseKey);
            return string.Join(L10nSymbols.KEY_SEPARATOR, extensions);
        }

        /// <summary>
        /// append given key
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

        public static bool IsL10nContextDefined(object value)
        {
            return ContextTable.HasContextTypeDefined(value?.GetType());
        }
    }
}