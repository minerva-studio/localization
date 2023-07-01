using Minerva.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Handlers for escape patterns in localization
    /// </summary>
    internal static class EscapePattern
    {
        public const string DYNAMIC_VALUE_ARG_PATTERN = @"\[([^\[\]\n:,$§]*)(?:\:(?:([^\[\]\n:,$§]*),?)+)*\]";
        public const string CONTENT_REFERENCE_PATTERN = @"\$([^\$\n]*?)\$";
        public const string COLOR_SIMPLE_PATTERN = @"§(.)([^§\n]*?)§";
        public const string COLOR_CODE_PATTERN = @"§(#[0-9A-Fa-f]{6})([^§\n]*?)§";

        /// <summary>
        /// replace §C...§ or §#FFFFFF...§ with color code
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceColorEscape(string rawString)
        {
            string pattern;
            MatchCollection escapes;

            pattern = COLOR_CODE_PATTERN;
            escapes = Regex.Matches(rawString, pattern);
            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                var colorCode = item.Result("$1");
                var entry = item.Result("$2");
                rawString = rawString.Replace(item.Value, entry.UGUIColor(colorCode));
            }

            pattern = COLOR_SIMPLE_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                var colorCode = item.Result("$1");
                var entry = item.Result("$2");
                rawString = rawString.Replace(item.Value, entry.UGUIColor(ColorCode.GetColorHex(colorCode[0])));
            }
            return rawString;
        }


        /// <summary>
        /// Replace $...$ with a content
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceKeyEscape(string rawString)
        {
            string pattern;
            MatchCollection escapes;
            pattern = CONTENT_REFERENCE_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                var key = item.Result("$1");
                rawString = rawString.Replace(item.Value, L10n.GetRawContent(key));
            }
            return rawString;
        }


        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        public static string ReplaceDynamicValueEscape(string rawString, ILocalizable obj, params string[] param)
        {
            string pattern;
            MatchCollection escapes;
            //Debug.Log("Try replace escape");

            Dictionary<string, string> cachedOptions = new();
            GetDynamicValue(cachedOptions, param);

            pattern = DYNAMIC_VALUE_ARG_PATTERN;
            escapes = Regex.Matches(rawString, pattern);
            for (int i = 0; i < escapes.Count; i++)
            {
                Match match = escapes[i];
                string key = match.Groups[1].Value;
                string[] localParam = param;
                Dictionary<string, string> localOptions = cachedOptions;
                // has custom param
                if (match.Groups[2].Success)
                {
                    GetLocalParam(match.Groups[2], ref localParam, ref localOptions);
                }
                if (!localOptions.TryGetValue(key, out string replacement) && obj != null)
                    replacement = obj.GetEscapeValue(key, localParam);
                replacement = ReplaceKeyEscape(replacement);
                rawString = rawString.Replace(match.Value, replacement);
            }

            escapes = Regex.Matches(rawString, pattern);
            // nested dynamic value
            if (escapes.Count > 0)
            {
                return ReplaceDynamicValueEscape(rawString, obj, param);
            }
            return rawString;
        }


        private static void GetLocalParam(Group group, ref string[] localParam, ref Dictionary<string, string> table)
        {
            string[] globalParam = localParam;
            Dictionary<string, string> oldTable = table;

            HashSet<string> strings = new();
            table = new Dictionary<string, string>();
            for (int k = 0; k < group.Captures.Count; k++)
            {
                string value = group.Captures[k].Value.Trim();
                if (string.IsNullOrEmpty(value)) continue;
                if (value[0] == L10nSymbols.PARAM_REF_SYMBOL)
                {
                    strings.UnionWith(globalParam);
                }
                else if (value == L10nSymbols.VARIABLE_SYMBOL.ToString())
                {
                    strings.UnionWith(oldTable.Select(p => $"{p.Key}={p.Value}"));
                    foreach (var item in oldTable)
                    {
                        table[item.Key] = item.Value;
                    };
                }
                else if (value[0] == L10nSymbols.VARIABLE_SYMBOL)
                {
                    var key = value[1..];
                    if (oldTable.TryGetValue(key, out var result))
                    {
                        strings.Add(result);
                        table[key] = result;
                    }
                }
                else
                {
                    strings.Add(value);
                }
            }
            localParam = strings.ToArray();
        }

        /// <summary>
        /// Replace dynamic value escape strings, only replace by argument
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        [Obsolete]
        public static string ReplaceDynamicValueEscapeBase(string rawString, params string[] param)
        {
            // nothing to be replaced
            if (param.Length == 0)
            {
                return rawString;
            }
            Dictionary<string, string> cachedOptions = new();
            GetDynamicValue(cachedOptions, param);
            // nothing to be replaced
            if (cachedOptions.Count == 0)
            {
                return rawString;
            }

            string pattern;
            MatchCollection escapes;
            //Debug.Log("Try replace escape");
            pattern = DYNAMIC_VALUE_ARG_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                string key = item.Groups[1].Value;
                string[] localParam = param;
                Dictionary<string, string> localOptions = cachedOptions;
                // has custom param
                if (item.Groups[2].Success)
                {
                    GetLocalParam(item.Groups[2], ref localParam, ref localOptions);
                }

                if (!localOptions.TryGetValue(key, out string replacement)) continue;
                replacement = ReplaceKeyEscape(replacement);
                rawString = rawString.Replace(item.Value, replacement);
            }
            return rawString;
        }

        public static Dictionary<string, string> GetDynamicValue(params string[] param)
        {
            Dictionary<string, string> dictionary = new();
            GetDynamicValue(dictionary, param);
            return dictionary;
        }

        public static void GetDynamicValue(Dictionary<string, string> dictionary, params string[] param)
        {
            foreach (var p in param)
            {
                // not a valid arg
                int index = p.IndexOf("=");
                if (index == -1) continue;

                // key exist already
                var key = p[..index];
                if (dictionary.ContainsKey(key)) continue;

                // add to dictionary
                var value = p[(index + 1)..];
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Make given string as an content of given key as escape
        /// </summary>
        /// <param name="baseKey"></param>
        /// <returns></returns>
        public static string AsKeyEscape(string baseKey, params string[] args)
        {
            return $"${Localizable.AppendKey(baseKey, args)}$";
        }
    }
}