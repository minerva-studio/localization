using Minerva.Localizations.EscapePatterns;
using Minerva.Module;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Minerva.Localizations.Tests
{
    public class LocalizationRegexTest
    {
        public static readonly Regex DYNAMIC_VALUE_ARG_PATTERN = new(@"(?<!\\)(?:\\{2})*(\{([\w.]*)(?::(?:([\w.~]+),?)*)?\})");
        public static readonly Regex CONTENT_REFERENCE_PATTERN = new(@"(?<!\\)(?:\\{2})*(\$([\w.]*?)\$)");
        public static readonly Regex COLOR_SIMPLE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(.)([\s\S]*?)§");
        public static readonly Regex COLOR_CODE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(#[0-9A-Fa-f]{6})([\s\S]*?)§");
        public static readonly Regex BACKSLASH_PATTERN = new(@"\\(.)");

        public const int MAX_NUM_PER_SEGMENT = 5;
        public const int MAX_SEGMENT_NUM = 10;

        private bool Test(char specChar, Regex pattern)
        {
            for (int i = 0; i < 100; i++)
            {
                int seg = Random.Range(1, MAX_SEGMENT_NUM + 1);
                string s = "";
                for (int i2 = 0; i2 < seg / 2; i2++)
                {
                    int r = Random.Range(1, MAX_NUM_PER_SEGMENT + 1);
                    if (r % 2 == 0) r++;
                    for (int i3 = 0; i3 < r; i3++)
                        s += @"\\";
                    s += "\\" + specChar;
                }
                s += @"\\" + specChar + "ABC" + specChar + @"\\";
                for (int i2 = 0; i2 < seg / 2; i2++)
                {
                    int r = Random.Range(1, MAX_NUM_PER_SEGMENT + 1);
                    if (r % 2 == 0) r++;
                    for (int i3 = 0; i3 < r; i3++)
                        s += @"\\";
                    s += "\\" + specChar;
                }

                MatchCollection escapes = pattern.Matches(s);
                Debug.Log(s);
                Debug.Log(escapes[0].Result("$2"));
                if (escapes.Count != 1 || escapes[0].Result("$2") != "ABC")
                    return false;
            }
            return true;
        }


        [Test]
        public async Task BackSlashFilterTest()
        {
            await Awaitable.NextFrameAsync();
            Assert.IsTrue(Test('$', new Regex(@"(?<!\\)(?:\\{2})*(\$([\w.]*?)\$)")));
        }

        [TestCase(@"A is $A.b$|A is Content")]
        [TestCase(@"A is $A.b$ $A.c$|A is Content Content")]
        [TestCase(@"A is $A.b$$A.c$|A is ContentContent")]
        [TestCase(@"A is \\$A.b$|A is \Content")]
        [TestCase(@"A is \$A.b$|A is $A.b$")]
        [TestCase(@"\$A is $A.b$|$A is Content")]
        [TestCase(@"\$A is \$A.b\$|$A is $A.b$")]
        [TestCase(@"$A is $A.b$|$A is Content")]    // $...$ cannot have whitespace in between
        public void ReplaceKeyEscapeTest(string test)
        {
            string[] parsed = test.Split("|");
            string v = Escape(parsed[0], null);
            Assert.IsTrue(parsed[1] == v);
        }

        /// <summary>
        /// Resolve all escape characters
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="context"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string Escape(string rawString, ILocalizableContext context, params string[] param)
        {
            rawString = ReplaceKeyEscape(rawString);
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
        public static string ReplaceKeyEscape(string rawString)
        {
            Debug.Log(rawString);
            var n = CONTENT_REFERENCE_PATTERN.Replace(rawString, (m) =>
            {
                string replacing = m.Groups[1].Value;
                string key = m.Groups[2].Value;
                Debug.Log(replacing);
                Debug.Log(key);
                return m.Value.Replace(replacing, "Content");
            });
            return n;
        }


        /// <summary>
        /// Replace dynamic value escape strings
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="param"></param>
        /// <returns></returns>  
        public static string ReplaceDynamicValueEscape(string rawString, ILocalizableContext context, params string[] param)
        {
            Dictionary<string, string> cachedOptions = new();
            GetDynamicValue(cachedOptions, param);
            do
            {
                rawString = DYNAMIC_VALUE_ARG_PATTERN.Replace(rawString, (m) =>
                {
                    string key = m.Groups[2].Value;
                    string[] localParam = param;
                    Dictionary<string, string> localOptions = cachedOptions;
                    // has custom param
                    if (m.Groups[3].Success)
                        GetLocalParam(m.Groups[3], ref localParam, ref localOptions);
                    // if defined, then use it, otherwise ask context
                    if (!localOptions.TryGetValue(key, out string replacement) && context != null)
                        replacement = context.GetEscapeValue(key, L10nParams.FromStrings(localParam)).ToString();
                    replacement = ReplaceKeyEscape(replacement);
                    return m.Value.Replace(m.Groups[1].Value, replacement);
                });
            }
            while (DYNAMIC_VALUE_ARG_PATTERN.Matches(rawString).Count > 0);
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
                if (value == "PARENT_PARAM")
                {
                    strings.UnionWith(globalParam);
                }
                else if (value[0] == '~')
                {
                    // exact match
                    if (value.Length == 1)
                    {
                        strings.UnionWith(oldTable.Select(p => $"{p.Key}={p.Value}"));
                        foreach (var item in oldTable)
                        {
                            table[item.Key] = item.Value;
                        }
                        ;
                    }
                    // ~var
                    else
                    {
                        var key = value[1..];
                        if (oldTable.TryGetValue(key, out var result))
                        {
                            strings.Add(result);
                            table[key] = result;
                        }
                    }
                }
                else
                {
                    strings.Add(value);
                }
            }
            localParam = strings.ToArray();
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
    }
}