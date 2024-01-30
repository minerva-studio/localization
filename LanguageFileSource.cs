using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations
{
    public class LanguageFileSource : ScriptableObject
    {
        public string tag;
        [SerializeField] private List<string> keys = new();

        /// <summary>
        /// Create an (inspector) read only lang file
        /// </summary>
        /// <returns></returns> 
        public static LanguageFileSource NewLangFile()
        {
            var file = CreateInstance<LanguageFileSource>();
            return file;
        }

        public void ImportFromYaml(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string item = lines[i].Trim();
                if (string.IsNullOrEmpty(item) || item.StartsWith('#')) continue;

                int spliter = item.IndexOf(':');
                // some reason it is not right, skip line
                if (spliter == -1) continue;

                string key = item[..spliter].Trim();
                keys.Add(key);
            }
        }
    }
}