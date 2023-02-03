using Minerva.Module;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Amlos.Localizations.EscapePatterns
{
    /// <summary>
    /// Handlers for escape patterns in localization
    /// </summary>
    public static class EscapePattern
    {
        public const string DYNAMIC_VALUE_PATTERN = @"(\[.*?\])+";
        public const string CONTENT_REFERENCE_PATTERN = @"(\$(.*?)\$)+";
        public const string COLOR_PATTERN = @"(§(.)(.*?)§)+";

        /// <summary>
        /// replace §C...§ with color code
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceColorEscape(string rawString)
        {
            string pattern;
            MatchCollection escapes;
            pattern = COLOR_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                var colorCode = item.Result("$2");
                var entry = item.Result("$3");
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
                var key = item.Result("$2");
                rawString = rawString.Replace(item.Value, Localization.GetContent(key));
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
            pattern = DYNAMIC_VALUE_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            var dictionary = GetDynamicValue(param);
            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                if (!dictionary.TryGetValue(item.Value[1..^1].Trim(), out string replacement))
                    replacement = obj.GetEscapeValue(item.Value[1..^1].Trim(), param);
                ReplaceKeyEscape(replacement);
                rawString = rawString.Replace(item.Value, replacement);
            }

            return rawString;
        }

        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        public static string ReplaceDynamicValueEscape(string rawString, params string[] param)
        {
            // nothing to be replaced
            if (param.Length == 0)
            {
                return rawString;
            }
            var dictionary = GetDynamicValue(param);
            // nothing to be replaced
            if (dictionary.Count == 0)
            {
                return rawString;
            }

            string pattern;
            MatchCollection escapes;
            //Debug.Log("Try replace escape");
            pattern = DYNAMIC_VALUE_PATTERN;
            escapes = Regex.Matches(rawString, pattern);

            for (int i = 0; i < escapes.Count; i++)
            {
                Match item = escapes[i];
                if (!dictionary.TryGetValue(item.Value[1..^1].Trim(), out string replacement)) continue;
                //Debug.Log($"Replace {item.Value} with {replacement}");
                rawString = rawString.Replace(item.Value, replacement);
            }

            return rawString;
        }

        public static Dictionary<string, string> GetDynamicValue(params string[] param)
        {
            Dictionary<string, string> dictionary = new();
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
            return dictionary;
        }
    }
}