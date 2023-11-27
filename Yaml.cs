using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// simple yaml exporter (really simple one)
    /// </summary>
    public static class Yaml
    {
        public static void Export(Dictionary<string, string> keyValuePairs, string path)
        {
            File.WriteAllText(path, string.Empty);
            var lines = keyValuePairs
                .Where(e => !string.IsNullOrEmpty(e.Key) && !string.IsNullOrWhiteSpace(e.Key))
                .Select(e => $"{e.Key}: \"{ToProperString(e.Value)}\"");
            File.AppendAllLines(path, lines);


            static string ToProperString(string str)
            {
                return str.Replace("\n", "\\n").Replace("\r", "");
            }
        }

        public static void Export<T>(List<T> keyValuePairs, Func<T, string> key, Func<T, string> value, string path)
        {
            File.WriteAllText(path, string.Empty);
            var lines = keyValuePairs
                .Where(e => !string.IsNullOrEmpty(key(e)) && !string.IsNullOrWhiteSpace(value(e)))
                .Select(e => $"{key(e)}: \"{ToProperString(value(e))}\"");
            File.AppendAllLines(path, lines);

            static string ToProperString(string str)
            {
                return str.Replace("\n", "\\n").Replace("\r", "");
            }
        }
    }
}