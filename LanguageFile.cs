using Minerva.Module;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Amlos.Localizations
{
    /// <summary>
    /// The language file
    /// </summary>
    [CreateAssetMenu(fileName = "Lang_NAME", menuName = "Localization/Text File")]
    public class LanguageFile : ScriptableObject
    {
        public const string IS_MASTER_FILE_NAME = nameof(isMasterFile);
        public const string MASTER_FILE_NAME = nameof(masterFile);
        public const string CHILD_FILE_NAME = nameof(childFiles);
        public const string ENTRIES_NAME = nameof(entries);

        [SerializeField] private bool isMasterFile;
        [SerializeField] private LanguageFile masterFile;
        [SerializeField] private List<LanguageFile> childFiles = new();
        [SerializeField] private string region = string.Empty;
        [SerializeField] private List<Entry> entries = new();

        /// <summary> Region of the file represent </summary>
        public string Region { get => region; set => region = value; }
        /// <summary> All entries in the file </summary>
        public List<Entry> Entries => entries;

        public LanguageFile MasterFile { get => masterFile; set => masterFile = value; }
        public List<LanguageFile> ChildFiles { get => childFiles; set => childFiles = value; }
        public bool IsMasterFile => isMasterFile;

        /// <summary>
        /// Get a trie of the language file
        /// </summary>
        /// <returns></returns>
        public Trie<string> GetTrie()
        {
            var dictionary = new Trie<string>();
            foreach (var entry in entries)
            {
                dictionary[entry.Key] = entry.Value;
            }
            foreach (var item in childFiles)
            {
                foreach (var entry in item.entries)
                {
                    dictionary[entry.Key] = entry.Value;
                }
            }
            return dictionary;
        }

        /// <summary>
        /// get the dictionary of the language file
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetDictionary()
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                dictionary[entry.Key] = entry.Value;
            }
            foreach (var item in childFiles)
            {
                foreach (var entry in item.entries)
                {
                    dictionary[entry.Key] = entry.Value;
                }
            }
            return dictionary;
        }

#if  UNITY_EDITOR

        //public void OnValidate()
        //{
        //    UpdateFileState();
        //    EditorUtility.SetDirty(this);
        //}





        public IEnumerable<string> Keys => GetKeys();
        private IEnumerable<string> GetKeys()
        {
            foreach (var item in entries)
            {
                yield return item.Key;
            }
            if (isMasterFile)
            {
                foreach (var file in childFiles)
                {
                    foreach (var item in file.entries)
                    {
                        yield return item.Key;
                    }
                }
            }
        }




        /// <summary>
        /// Get value from the localization file
        /// <para>
        /// if localiztion file is a master file and key is not present in the file, it will search from the child files
        /// </para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            return TryGet(key, out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Try get the value from localization file
        /// <para>
        /// if localiztion file is a master file and key is not present in the file, it will search from the child files
        /// </para> 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Whether file contains the key/value</returns>
        public bool TryGet(string key, out string value)
        {
            value = entries.Find(e => e.Key == key)?.Value;

            if (value != null) return true;
            if (!isMasterFile) return false;

            foreach (var item in childFiles)
            {
                if (item.TryGet(key, out value)) return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Write the value by key
        /// <para>
        /// If the key appears in the file or child file, it will override existing value
        /// <br></br>
        /// If the key has not yet appears in the file, a new entry will be added to this file
        /// </para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns> string of the old entry, null if it is a new entry </returns>
        public string Write(string key, string value = "", bool updateAssets = false)
        {
            EditorUtility.SetDirty(this);
            //Debug.Log($"Write Entry " + key + " with value " + value);
            var entry = GetEntry(key);
            var oldVal = entry?.Value;
            if (entry != null)
            {
                entry.Value = value;
                if (updateAssets) AssetDatabase.SaveAssets();
                return oldVal;
            }
            else
            {
                entries.Add(new Entry(key, value));
            }
            if (updateAssets) AssetDatabase.SaveAssets();
            return oldVal;
        }

        /// <summary>
        /// Add value by key
        /// <para>
        /// If the key appears in the file or child file, it will not override existing value, do nothing and return false.
        /// </para>
        /// <para>
        /// If the key has not yet appears in the file, a new entry will be added to this file
        /// </para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns> Whether given key is in the file already </returns>
        public bool Add(string key, string value = "", bool updateAssets = false)
        {
            EditorUtility.SetDirty(this);
            Debug.Log($"Write Entry " + key + " with value " + value);
            var entry = GetEntry(key);
            if (entry != null) return false;
            else entries.Add(new Entry(key, value));
            if (updateAssets) AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// Determine whether the key exist in the file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="searchInChild">whether search for key in child</param>
        /// <returns></returns>
        public bool HasKey(string key, bool searchInChild = false)
        {
            bool haskey = entries.Any(p => p.Key == key);
            if (!haskey && searchInChild)
            {
                haskey = childFiles.Any(f => f.HasKey(key, false));
            }
            return haskey;
        }





        /// <summary>
        /// Get entry from the file and child file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Entry GetEntry(string key)
        {
            return GetEntryOnSelf(key) ?? GetEntryFromChild(key);
        }

        private Entry GetEntryFromChild(string key)
        {
            foreach (var item in childFiles)
            {
                Entry entry = item.GetEntryOnSelf(key);
                if (entry != null) return entry;
            }
            return null;
        }

        private Entry GetEntryOnSelf(string key)
        {
            return entries.FirstOrDefault(p => p.Key == key);
        }





        /// <summary>
        /// Sort entries
        /// </summary>
        /// <param name="searchInChild">also sort childs</param>
        public void Sort(bool searchInChild = false)
        {
            entries.Sort();
            if (searchInChild) childFiles.ForEach(f => f.Sort(false));
        }

        /// <summary>
        /// remove key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="searchInChild"></param>
        public void RemoveKey(string key, bool searchInChild = false, bool updateAssets = false)
        {
            entries.RemoveAll(p => p.Key == key);
            if (searchInChild) childFiles.ForEach(f => f.RemoveKey(key, false));
            if (updateAssets) AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// move given key
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        /// <param name="searchInChild"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void MoveKey(string oldKey, string newKey, bool searchInChild = false, bool updateAssets = false)
        {
            EditorUtility.SetDirty(this);
            var entry = searchInChild ? GetEntry(oldKey) : GetEntryOnSelf(oldKey);
            if (entry == null) throw new KeyNotFoundException();
            entry.Key = newKey;
            if (updateAssets) AssetDatabase.SaveAssets();
        }
        /// <summary>
        /// move given key
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        /// <param name="searchInChild"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public bool TryMoveKey(string oldKey, string newKey, bool searchInChild = false, bool updateAssets = false)
        {
            EditorUtility.SetDirty(this);
            var entry = searchInChild ? GetEntry(oldKey) : GetEntryOnSelf(oldKey);
            if (entry == null) return false;
            entry.Key = newKey;
            if (updateAssets) AssetDatabase.SaveAssets();
            return true;
        }




        /// <summary>
        /// Find the matched keys from the file
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="searchInChild">Whether search child file</param>
        /// <returns></returns>
        public List<string> FindMatchedKeys(string partialKey, bool searchInChild = false)
        {
            List<string> result = new();
            FindMatchedKeys_Internal(partialKey, result);
            if (!searchInChild)
            {
                return result;
            }
            foreach (var child in childFiles)
            {
                child.FindMatchedKeys_Internal(partialKey, result);
            }
            return result;
        }

        /// <summary>
        /// internal method to get matched keys
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="result"></param>
        private void FindMatchedKeys_Internal(string partialKey, List<string> result)
        {
            if (string.IsNullOrEmpty(partialKey))
            {
                result.AddRange(entries.Select(s => s.Key));
                return;
            }
            foreach (var item in entries)
            {
                if (item.Key.Contains(partialKey) && item.Key != partialKey) result.Add(item.Key);
            }
        }




        /// <summary>
        /// Update localiztion file state
        /// </summary>
        internal void UpdateFileState()
        {
            if (isMasterFile)
            {
                foreach (var item in childFiles)
                {
                    if (item) item.SetMasterFile(this);
                }
            }
            else if (masterFile)
            {
                SetMasterFile(masterFile);
            }
        }

        /// <summary>
        /// set the master file of this language file
        /// </summary>
        /// <param name="languageFile"></param>
        public void SetMasterFile(LanguageFile languageFile)
        {
            isMasterFile = false;
            masterFile = languageFile;
            region = languageFile.region;
        }





        /// <summary>
        /// Clear entries in the file
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }






        [ContextMenu("Export to Yaml")]
        public void ExportToYaml()
        {
            string fileName = name;
            //string fileName = masterFile ? region : $"{name}-{region}";
            string path = EditorUtility.SaveFilePanel("Save yaml file", AssetDatabase.GetAssetPath(this), fileName, "yml");

            //Debug.Log(p);
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path + "_old")) File.Copy(path, path + "_old", true);

            File.WriteAllText(path, string.Empty);
            File.AppendAllText(path, $"{region}:\n");
            var lines = entries
                .Where(e => !string.IsNullOrEmpty(e.Key) && !string.IsNullOrWhiteSpace(e.Key))
                .Select(e => $" {e.Key}: \"{ToProperString(e.Value)}\"");
            File.AppendAllLines(path, lines);

            AssetDatabase.Refresh();

            static string ToProperString(string str)
            {
                return str.Replace("\n", "\\n");
            }
        }

        [ContextMenu("Load From Yaml")]
        public void ImportFromYaml()
        {
            var path = EditorUtility.OpenFilePanel("Select yml file", AssetDatabase.GetAssetPath(this), "yml");
            if (string.IsNullOrEmpty(path)) return;

            entries.Clear();
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    region = lines[i].Replace(":", string.Empty);
                    continue;
                }
                //Debug.Log(lines[i]);
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string item = lines[i].Trim();
                if (string.IsNullOrEmpty(item) || item.StartsWith('#')) continue;

                int spliter = item.IndexOf(':');
                string key = item[..spliter].Trim();
                string value = item[(spliter + 1)..].Trim()[1..^1];
                entries.Add(new Entry(key, value.Replace("\\n", "\n")));
            }
        }

        [ContextMenu("Create Child File")]
        public void CreateChildFile()
        {
            string fileName = $"Subfile-{region}";
            string path = EditorUtility.SaveFilePanel("Save yaml file", AssetDatabase.GetAssetPath(this), fileName, "asset");
            if (string.IsNullOrEmpty(path)) return;
            Debug.Log(path);
            var subfile = CreateInstance<LanguageFile>();
            childFiles.Add(subfile);
            subfile.SetMasterFile(this);
            AssetDatabase.CreateAsset(subfile, "Assets" + path[Application.dataPath.Length..]);
            AssetDatabase.Refresh();
        }
#endif
    }
}