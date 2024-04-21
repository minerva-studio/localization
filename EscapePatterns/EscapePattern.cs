using Minerva.Module;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Handlers for escape patterns in localization
    /// </summary>
    internal static class EscapePattern
    {
        public static readonly Regex DYNAMIC_VALUE_ARG_PATTERN = new(@"(?<!\\)(?:\\{2})*(\{([\w.]*?)(?::(?:([\w.~=]+),?)*)?\})");
        public static readonly Regex CONTENT_REFERENCE_PATTERN = new(@"(?<!\\)(?:\\{2})*(\$([\w.]*?)(?::(?:([\w.~=]+),?)*)?\$)");
        public static readonly Regex COLOR_SIMPLE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(.)([\s\S]*?)§");
        public static readonly Regex COLOR_CODE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(#[0-9A-Fa-f]{6})([\s\S]*?)§");
        public static readonly Regex BACKSLASH_PATTERN = new(@"\\(.)");


        /// <summary>
        /// Resolve all escape characters
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Escape(string rawString, ILocalizable context, params string[] param)
        {
            rawString = ReplaceKeyEscape(rawString, context, param);
            rawString = ReplaceDynamicValueEscape(rawString, context, param);
            rawString = ReplaceColorEscape(rawString);
            rawString = ReplaceBackSlash(rawString);
            return rawString;
        }


        /// <summary>
        /// replace \\ to \
        /// Because \$, \{ is meaningful
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceBackSlash(string rawString)
        {
            return BACKSLASH_PATTERN.Replace(rawString, "$1");
        }

        /// <summary>
        /// replace §C...§ or §#FFFFFF...§ with color code
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceColorEscape(string rawString)
        {
            rawString = COLOR_CODE_PATTERN.Replace(rawString, (m) =>
            {
                var colorCode = m.Groups[1].Value;//.Result("$1");
                var entry = m.Groups[2].Value; ;// m.Result("$2");
                return entry.UGUIColor(colorCode);
            });
            rawString = COLOR_SIMPLE_PATTERN.Replace(rawString, (m) =>
            {
                var colorCode = m.Groups[1].Value;//.Result("$1");
                var entry = m.Groups[2].Value; ;// m.Result("$2");
                return entry.UGUIColor(ColorCode.GetColorHex(colorCode[0]));
            });
            return rawString;
        }


        /// <summary>
        /// Replace $...$ with a content
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceKeyEscape(string rawString, ILocalizable context, params string[] param)
        {
            var n = CONTENT_REFERENCE_PATTERN.Replace(rawString, (m) =>
            {
                string replacing = m.Groups[1].Value;
                string key = m.Groups[2].Value;

                string rawContent = L10n.GetRawContent(key);

                string[] localParam;

                // has custom param 
                if (m.Groups[3].Success) (localParam, _) = GetLocalParam(m.Groups[3], param);
                else localParam = param;
                rawContent = ReplaceDynamicValueEscape(rawContent, context, localParam);
                return m.Value.Replace(replacing, rawContent);
            });
            return n;
        }


        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        public static string ReplaceDynamicValueEscape(string rawString, ILocalizable context, params string[] param)
        {
            rawString = DYNAMIC_VALUE_ARG_PATTERN.Replace(rawString, (m) =>
            {
                // we can't really guarantee context can correctly provide replacements
                try
                {
                    string key = m.Groups[2].Value;
                    string[] localParam;
                    Dictionary<string, string> localOptions;

                    // has custom param
                    if (m.Groups[3].Success) (localParam, localOptions) = GetLocalParam(m.Groups[3], param);
                    else (localParam, localOptions) = (param, ParseDynamicValue(new(), false, param));

                    // if defined, then use it, otherwise ask context
                    if (!localOptions.TryGetValue(key, out string replacement) && context != null)
                        replacement = context.GetEscapeValue(key, localParam);

                    // no replacement if not found
                    if (replacement == key)
                        return m.Value;

                    replacement = ReplaceKeyEscape(replacement, context, localParam);
                    return m.Value.Replace(m.Groups[1].Value, replacement);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    return m.Value;
                }
            });
            return rawString;
        }


        private static (string[] localParam, Dictionary<string, string> localVariable) GetLocalParam(Group group, string[] globalParam)
        {
            Dictionary<string, string> localVariables = new();
            Dictionary<string, string> globalVariables = ParseDynamicValue(new(), false, globalParam);
            string[] localParams;
            HashSet<string> strings = new();

            for (int k = 0; k < group.Captures.Count; k++)
            {
                string value = group.Captures[k].Value.Trim();
                if (string.IsNullOrEmpty(value)) continue;
                if (value == L10nSymbols.PARAM_REF_SYMBOL)
                {
                    strings.UnionWith(globalParam);
                }
                else if (value[0] == L10nSymbols.VARIABLE_SYMBOL)
                {
                    // exact match
                    if (value.Length == 1)
                    {
                        strings.UnionWith(globalVariables.Select(p => $"{p.Key}={p.Value}"));
                        foreach (var item in globalVariables)
                        {
                            localVariables[item.Key] = item.Value;
                        };
                    }
                    // ~var
                    else
                    {
                        var key = value[1..];
                        if (globalVariables.TryGetValue(key, out var result))
                        {
                            strings.Add($"{key}={result}");
                            localVariables[key] = result;
                        }
                    }
                }
                else
                {
                    strings.Add(value);
                }
            }
            localParams = strings.ToArray();
            return (localParams, localVariables);
        }

        public static Dictionary<string, string> ParseDynamicValue(Dictionary<string, string> dictionary, bool overriding = false, params string[] param)
        {
            foreach (var p in param)
            {
                // not a valid arg
                int index = p.IndexOf("=");
                if (index == -1) continue;

                // key exist already
                var key = p[..index];
                if (dictionary.ContainsKey(key) && !overriding) continue;

                // add to dictionary
                var value = p[(index + 1)..];
                dictionary.Add(key, value);
            }
            return dictionary;
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