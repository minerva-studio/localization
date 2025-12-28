using Minerva.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// simple yaml exporter (really simple one)
    /// </summary>
    public static partial class Yaml
    {
        /// <summary>
        /// Export a full-key file (all keys will be full, not in a standard yaml format)
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <param name="path">path fo the file</param>
        public static void ExportFullKey<T>(List<T> keyValuePairs, Func<T, string> key, Func<T, string> value, string path)
        {
            ExportFullKey(keyValuePairs.ToDictionary(key, value), path);
        }

        /// <summary>
        /// Export a full-key file (all keys will be full, not in a standard yaml format)
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <param name="path">path fo the file</param>
        public static void ExportFullKey(Dictionary<string, string> keyValuePairs, string path)
        {
            File.WriteAllText(path, string.Empty);
            File.AppendAllText(path, ExportFullKey(keyValuePairs));
        }

        /// <summary>
        /// Export a full-key file (all keys will be full, not in a standard yaml format)
        /// </summary>
        /// <param name="keyValuePairs"></param> 
        /// <returns>Exported string</returns>
        public static string ExportFullKey(Dictionary<string, string> keyValuePairs)
        {
            string v = string.Join('\n', keyValuePairs
                            .Where(e => !string.IsNullOrEmpty(e.Key) && !string.IsNullOrWhiteSpace(e.Key))
                            .OrderBy(e => e.Key)
                            .Select(e => $"{e.Key}: \"{ToProperString(e.Value)}\""));
            Debug.Log(v);
            return v;
        }

        public static void Export(Dictionary<string, string> keyValuePairs, string path)
        {
            //var lines = keyValuePairs
            //    .Where(e => !string.IsNullOrEmpty(e.Key) && !string.IsNullOrWhiteSpace(e.Key))
            //    .OrderBy(e => e.Key)
            //    .Select(e => $"{e.Key}: \"{ToProperString(e.Value)}\"");
            File.WriteAllText(path, string.Empty);
            File.AppendAllText(path, Export(keyValuePairs));

        }

        public static string Export(Dictionary<string, string> keyValuePairs)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Tries<string> trie = new Tries<string>(keyValuePairs);
            WriteLevel(new Module.TriesSegment<string>(trie), 0, stringBuilder);

            return stringBuilder.ToString();
        }

        private static void WriteLevel(Module.TriesSegment<string> trie, int indent = 0, StringBuilder stringBuilder = null)
        {
            var keys = trie.FirstLayerKeys.ToArray();
            Array.Sort(keys);
            foreach (var key in keys)
            {
                stringBuilder.Append(' ', indent);
                stringBuilder.Append(key);
                stringBuilder.Append(":");
                if (trie.ContainsKey(key))
                {
                    // something below
                    if (trie.TryGetSegment(key, out Module.TriesSegment<string> t) && t.Count > 1)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.Append(' ', indent + 2);
                        stringBuilder.Append(Reader.ObjectSelf);
                        stringBuilder.Append(":");
                    }
                    stringBuilder.AppendLine($" \"{ToProperString(trie[key])}\"");
                }
                else
                {
                    stringBuilder.AppendLine();
                }
                if (trie.TryGetSegment(key, out Module.TriesSegment<string> subTrie))
                {
                    WriteLevel(subTrie, indent + 2, stringBuilder);
                }
            }

        }

        public static void Export<T>(List<T> keyValuePairs, Func<T, string> key, Func<T, string> value, string path)
        {
            Export(keyValuePairs.ToDictionary(key, value), path);
        }

        public static Dictionary<string, string> Import(string str)
        {
            var dictionary = new Dictionary<string, string>();
            var reader = new Reader(str);
            foreach (var (key, value) in reader.ReadAll())
            {
                dictionary[key] = value;
            }
            return dictionary;
        }

        static string ToProperString(string str)
        {
            return str.Replace("\n", "\\n").Replace("\r", "");
        }
    }
}