﻿using Minerva.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Minerva.Localizations.EscapePatterns.ExpressionParser;
using static Minerva.Localizations.EscapePatterns.Regexes;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Handlers for escape patterns in localization
    /// </summary>
    internal static class EscapePattern
    {
        /// <summary>
        /// Resolve all escape characters
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Escape(string rawString, ILocalizableContext context, int depth = 0, params string[] param)
        {
            if (rawString == null) return string.Empty;
            rawString = ReplaceKeyEscape(rawString, context, depth, param);
            rawString = ReplaceDynamicValueEscape(rawString, context, depth, param);
            rawString = ReplaceColorEscape(rawString);
            rawString = ReplaceBackSlash(rawString);
            if (L10n.UseUnderlineResolver == UnderlineResolverOption.Always) return SplitUnderlineByColor(rawString);
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
            if (rawString == null) return string.Empty;
            return BACKSLASH_PATTERN.Replace(rawString, "$1");
        }

        /// <summary>
        /// replace §C...§ or §#FFFFFF...§ with color code
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceColorEscape(string rawString)
        {
            if (rawString == null) return string.Empty;
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
        public static string ReplaceKeyEscape(string rawString, ILocalizableContext context, int depth = 0, params string[] param)
        {
            if (!LoopCheck(depth, rawString)) return rawString;
            if (rawString == null) return string.Empty;
            var n = CONTENT_REFERENCE_PATTERN.Replace(rawString, (m) =>
            {
                string replacing = m.Groups[1].Value;
                string key = m.Groups[2].Value;

                bool isTooltip = replacing.StartsWith("$@");

                string rawContent = L10n.GetRawContent(key);

                string[] localParam;

                // has custom param 
                if (m.Groups[3].Success) (localParam, _) = GetLocalParam(m.Groups[3], param);
                else localParam = param;
                // result value could contains new reference
                rawContent = ReplaceKeyEscape(rawContent, context, depth + 1, param);
                // result value could contains dynamic value
                rawContent = ReplaceDynamicValueEscape(rawContent, context, depth + 1, localParam);
                // result value could contains color tag
                rawContent = ReplaceColorEscape(rawContent);
                // get actual value
                string newValue = ReplaceReference(key, rawContent, isTooltip ? L10n.TooltipImportOption : L10n.ReferenceImportOption);
                return m.Value.Replace(replacing, newValue);
            });
            return n;
        }

        private static string ReplaceReference(string key, string rawContent, ReferenceImportOption referenceImportOption)
        {
            bool withLink = referenceImportOption.HasFlag(ReferenceImportOption.WithLinkTag);
            bool withUnderline = referenceImportOption.HasFlag(ReferenceImportOption.WithUnderline);
            bool splitUnderline = withUnderline && L10n.UseUnderlineResolver == UnderlineResolverOption.WhileLinking;

            // Fast path: no modification needed
            if (!withLink && !withUnderline)
                return rawContent;

            string content = rawContent;

            if (splitUnderline)
            {
                content = SplitUnderlineDirect(content);
            }
            else if (withUnderline)
            {
                content = $"<u>{content}</u>";
            }

            if (withLink)
            {
                content = $"<link={key}>{content}</link>";
            }

            return content;
        }



        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        //[Obsolete]
        //public static string ReplaceDynamicValueEscape_Default(string rawString, ILocalizableContext context, params string[] param)
        //{
        //    if (rawString == null) return string.Empty;
        //    rawString = DYNAMIC_VALUE_ARG_PATTERN.Replace(rawString, (m) =>
        //    {
        //        // we can't really guarantee context can correctly provide replacements
        //        try
        //        {
        //            string key = m.Groups[2].Value;
        //            string[] localParam;
        //            string result;
        //            Dictionary<string, string> localOptions;

        //            // has custom param
        //            if (m.Groups[3].Success) (localParam, localOptions) = GetLocalParam(m.Groups[3], param);
        //            else (localParam, localOptions) = (param, ParseDynamicValue(new(), false, param));

        //            // if defined, then use it, otherwise ask context
        //            if (!localOptions.TryGetValue(key, out string replacement) && context != null)
        //                replacement = context.GetEscapeValue(key, localParam).ToString();

        //            // no replacement if not found
        //            if (replacement == key) result = m.Value;
        //            else result = m.Value.Replace(m.Groups[1].Value, ReplaceKeyEscape(replacement, context, depth + 1, localParam));

        //            return result;
        //        }
        //        catch (System.Exception e)
        //        {
        //            Debug.LogException(e);
        //            return m.Value;
        //        }
        //    });
        //    return rawString;
        //}


        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        public static string ReplaceDynamicValueEscape(string rawString, ILocalizableContext context, int depth, params string[] param)
        {
            if (!LoopCheck(depth, rawString)) return rawString;
            if (rawString == null) return string.Empty;

            Dictionary<string, string> globalValue = null;
            rawString = DYNAMIC_EXPR_BRACKET_ARG_PATTERN.Replace(rawString, (m) =>
            {
                // we can't really guarantee context can correctly provide replacements
                try
                {
                    string expr = m.Groups[2].Value;
                    var parser = new Parser(expr);
                    Node ast = parser.ParseExpression();
                    var result = ast.Run(VariableParser);

                    // float result uses format
                    string format;
                    try
                    {
                        if (m.Groups.Count > 3) format = m.Groups[3].Value;
                        else format = "";
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        format = "";
                    }
                    if (IsNumeric(result))
                    {
                        return NumberToString(result, format);
                    }
                    else if (long.TryParse(result.ToString(), out long l))
                    {
                        return NumberToString(l, format);
                    }
                    else if (double.TryParse(result.ToString(), out double r))
                    {
                        return NumberToString(r, format);
                    }
                    else if (result is not string)
                    {
                        result = Localizable.Tr(result, depth + 1, param) ?? "null";
                    }
                    // string result could be key escape
                    return ReplaceKeyEscape(result.ToString(), context, depth + 1, param);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    return m.Value;
                }
            });
            return rawString;

            /// Getting the variable value from the context
            object VariableParser(ReadOnlyMemory<char> expr)
            {
                string input = expr.ToString();
                var m = DYNAMIC_ARG_PATTERN.Match(input);
                // we can't really guarantee context can correctly provide replacements
                try
                {
                    string key = m.Groups[1].Value;
                    string[] localParam;
                    Dictionary<string, string> localValue;
                    // has custom param
                    if (m.Groups[2].Success) (localParam, localValue) = GetLocalParam(m.Groups[2], param, GetGlobalValue());
                    else (localParam, localValue) = (param, GetGlobalValue());
                    //Debug.Log($"parsed form:\t{input}<{string.Join(",", localParam)}>");

                    // if defined, then use it, otherwise ask context
                    if (!localValue.TryGetValue(key, out string replacement) && context != null)
                    // no replacement if not found
                    {
                        return context.GetEscapeValue(key, localParam);
                    }
                    var value = ReplaceKeyEscape(replacement, context, depth + 1, localParam);
                    //Debug.Log($"{{{key}: {replacement}}} replaced to {value}");
                    return value;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    return m.Value;
                }
            }
            Dictionary<string, string> GetGlobalValue()
            {
                return globalValue ??= ParseDynamicValue(new(), false, param);
            }
        }

        public static string SplitUnderlineByColor(string input)
        {
            // Match: <u>(...multiple <color> tags...)</u>
            return UNDERLINE_TAG.Replace(
                input,
                match =>
                {
                    string inside = match.Groups[1].Value;
                    // Wrap each <color=...>...</color> with underline
                    string fixedContent = COLOR_TAG.Replace(
                        inside,
                        m => $"{m.Groups[1].Value}<u>{m.Groups[2].Value}</u>{m.Groups[3].Value}"
                    );

                    return fixedContent;
                }
            );
        }

        public static string SplitUnderlineDirect(string input)
        {
            // If no color tags, apply one underline to the entire input
            if (!input.Contains("<color"))
                return $"<u>{input}</u>";

            // Inner underlines only for <color> blocks
            return COLOR_TAG.Replace(input, m =>
                $"{m.Groups[1].Value}<u>{m.Groups[2].Value}</u>{m.Groups[3].Value}");
        }





        public static bool LoopCheck(int depth, string context)
        {
            if (depth >= L10n.MAX_RECURSION)
            {
                Debug.LogException(new StackOverflowException(context));
                return false;
            }
            return true;
        }






        private static (string[] localParam, Dictionary<string, string> localVariable) GetLocalParam(Group group, string[] globalParam, Dictionary<string, string> globalValue = null)
        {
            Dictionary<string, string> localVariables = new();
            string[] localParams;
            HashSet<string> strings = new();
            for (int k = 0; k < group.Captures.Count; k++)
            {
                string value = group.Captures[k].Value.Trim();
                Debug.Log(value);
                if (string.IsNullOrEmpty(value)) continue;
                if (value == L10nSymbols.PARAM_REF_SYMBOL)
                {
                    globalValue ??= ParseDynamicValue(new(), false, globalParam);
                    strings.UnionWith(globalParam);
                    continue;
                }
                else if (value[0] == L10nSymbols.VARIABLE_SYMBOL)
                {
                    // exact match
                    if (value.Length == 1)
                    {
                        globalValue ??= ParseDynamicValue(new(), false, globalParam);
                        strings.UnionWith(globalValue.Select(p => $"{p.Key}={p.Value}"));
                        foreach (var item in globalValue)
                        {
                            localVariables[item.Key] = item.Value;
                        }
                    }
                    // ~var
                    else
                    {
                        var key = value[1..];
                        globalValue ??= ParseDynamicValue(new(), false, globalParam);
                        if (globalValue.TryGetValue(key, out var result))
                        {
                            strings.Add($"{key}={result}");
                            localVariables[key] = result;
                        }
                    }
                }
                else
                {
                    strings.Add(value);
                    int idx = value.IndexOf('=');
                    if (idx > 0)
                    {
                        localVariables[value[..idx]] = value[(idx + 1)..];
                    }
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
        /// Check whether the value is raw value to l10n dynamic value
        /// </summary>
        /// <remarks> The raw values are all primitives and <see cref="decimal"/> <br/>
        /// Examples: <br/>
        /// - <see cref="string"/> <br/>
        /// - <see cref="int"/> <br/>
        /// - <see cref="long"/> <br/>
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsRawValue(object value)
        {
            return value is string || value is decimal || value.GetType().IsPrimitive && value is not bool and not char and not IntPtr and not UIntPtr;
        }

        public static bool IsNumeric(object value)
        {
            switch (value)
            {
                case int:
                case long:
                case float:
                case double:
                case short:
                case decimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// use format section to format number to string
        /// </summary>
        /// <param name="numberLike"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string NumberToString(object numberLike, string format)
        {
            var number = AsNumber(numberLike);
            if (!string.IsNullOrEmpty(format))
            {
                return number.ToString(format);
            }

            return numberLike switch
            {
                int or long or short => numberLike.ToString(),
                _ => number.ToString("F1"),
            };
        }

        /// <summary>
        /// try cast objec to number
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double AsNumber(object value)
        {
            switch (value)
            {
                case byte b:
                    return b;
                case int i:
                    return i;
                case float f:
                    return f;
                case long l:
                    return l;
                case short s:
                    return s;
                case decimal c:
                    return (double)c;
                case double d:
                    return d;
                default:
                    break;
            }
            return 0;
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

        /// <summary>
        /// Make given string as an content of given key as escape
        /// </summary>
        /// <param name="baseKey"></param>
        /// <returns></returns>
        public static string AsTooltipKeyEscape(string baseKey, params string[] args)
        {
            return $"$@{Localizable.AppendKey(baseKey, args)}$";
        }
    }
}