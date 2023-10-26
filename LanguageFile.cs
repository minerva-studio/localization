using Minerva.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations
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

        [SerializeField] private string tag;
        [ReadOnly][SerializeField] private bool isReadOnly;
        [SerializeField] private bool isMasterFile;
        [SerializeField] private LanguageFile masterFile;
        [SerializeField] private List<LanguageFile> childFiles = new();
        [SerializeField] private string region = string.Empty;
        [SerializeField] private List<Entry> entries = new();

        /// <summary> Region of the file represent </summary>
        public string Region { get => isMasterFile ? region : masterFile.region; }
        public string Tag { get => tag; set => tag = value; }
        public LanguageFile MasterFile { get => masterFile; set => masterFile = value; }
        public List<LanguageFile> ChildFiles { get => childFiles; set => childFiles = value; }
        public bool IsMasterFile => isMasterFile;
        public bool IsReadOnly => isReadOnly;

        /// <summary>
        /// Get a trie of the language file
        /// </summary>
        /// <returns></returns>
        public Tries<string> GetTrie()
        {
            var dictionary = new Tries<string>();
            GetDictionary(dictionary);
            return dictionary;
        }

        /// <summary>
        /// get the dictionary of the language file
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetDictionary()
        {
            var dictionary = new Dictionary<string, string>();
            GetDictionary(dictionary);
            return dictionary;
        }

        void GetDictionary(IDictionary<string, string> dictionary)
        {
            int duplicate = 0;

            Import(entries);
            foreach (var item in childFiles)
            {
                Import(item.entries);
            }

            if (duplicate > 0)
            {
                Debug.LogErrorFormat("{0} duplicate keys found in the language file", duplicate);
            }

            void Import(List<Entry> entries)
            {
                foreach (var entry in entries)
                {
                    if (dictionary.ContainsKey(entry.Key))
                    {
                        Debug.LogWarningFormat("Duplicate key found: {0}, override with {1}", entry.Key, entry.Value);
                        duplicate++;
                    }
                    dictionary[entry.Key] = entry.Value;
                }
            }
        }

#if UNITY_EDITOR

        private SerializedObject sobj;
        public SerializedObject serializedObject { get => sobj ??= new(this); }
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
            entries ??= new();
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
        public string Write(string key, string value = "", bool immediateSave = false)
        {
            EditorUtility.SetDirty(this);
            //Debug.Log($"Write Entry " + key + " with value " + value);
            var entry = GetEntry(key);
            var oldVal = entry?.Value;
            if (entry != null)
            {
                entry.Value = value;
            }
            else
            {
                entry = new Entry(key, value);
                entries.Add(entry);
            }
            // L10n is loading current region
            if (L10n.Region == region)
            {
                L10n.Override(key, value);
            }

            if (immediateSave)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.ClearDirty(this);
            }

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
            var entry = GetEntry(key);
            if (entry != null)
            {
                Debug.Log("Entry exist");
                return false;
            }
            else entries.Add(new Entry(key, value));
            serializedObject.Update();
            Debug.Log($"Write Entry " + key + " with value " + value);
            if (updateAssets) AssetDatabase.SaveAssets();
            return true;
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
        public bool AddToFile(string key, string fileTag, string value = "", bool updateAssets = false)
        {
            Debug.Log("Add");
            if (fileTag == tag)
            {
                return Add(key, value, updateAssets);
            }
            if (isMasterFile)
            {
                var file = childFiles.FirstOrDefault(f => f.tag == fileTag);
                if (!file) file = CreateChildFile(fileTag);
                if (!file) return false;

                return file.Add(key, value, updateAssets);
            }

            return false;
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
        internal Entry GetEntry(string key)
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




        public SerializedProperty GetProperty(string key)
        {
            return TryGetProperty(key, out var value) ? value : null;
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
        public bool TryGetProperty(string key, out SerializedProperty value)
        {
            serializedObject.Update();
            value = null;
            var index = entries.FindIndex(e => e.Key == key);
            if (index != -1)
            {
                SerializedProperty entriesProperty = serializedObject.FindProperty(nameof(entries));
                if (entriesProperty.arraySize != 0 && entriesProperty.arraySize > index)
                {
                    value = entriesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("value");
                    return true;
                }

            }
            if (!isMasterFile) return false;

            foreach (var item in childFiles)
            {
                if (item.TryGetProperty(key, out value)) return true;
            }
            return false;
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
            EditorUtility.SetDirty(this);
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
        /// <returns>all full keys that matches given partial key</returns>
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
        /// Find the matched keys from the file
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="searchInChild">Whether search child file</param>
        /// <returns></returns>
        public void FindMatchedKeys(List<string> result, string partialKey, bool searchInChild = false)
        {
            FindMatchedKeys_Internal(partialKey, result);
            if (!searchInChild)
            {
                return;
            }
            foreach (var child in childFiles)
            {
                child.FindMatchedKeys_Internal(partialKey, result);
            }
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
            isMasterFile = !languageFile;
            masterFile = languageFile;
            if (languageFile)
            {
                region = languageFile.region;
            }
        }





        /// <summary>
        /// Clear entries in the file
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }




        [ContextMenu("Clear Duplicate Keys")]
        public void ClearDuplicateKeys()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry item = entries[i];
                for (int j = 0; j < childFiles.Count; j++)
                {
                    LanguageFile item1 = childFiles[j];
                    if (item1.HasKey(item.Key))
                    {
                        entries.Remove(item);
                    }
                }
            }
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

            Yaml.Export(entries, e => e.Key, e => e.Value, path);

            AssetDatabase.Refresh();
        }

        [ContextMenu("Export to Yaml (Source)")]
        public void ExportToYamlAsSource() => ExportToYamlAsSource(true);
        [ContextMenu("Export to Yaml (Source with value)")]
        public void ExportToYamlAsSourceWithVal() => ExportToYamlAsSource(false);
        public void ExportToYamlAsSource(bool noValue = false)
        {
            string fileName = name.Replace(region + "_", "");
            //string fileName = masterFile ? region : $"{name}-{region}";
            string path = EditorUtility.SaveFilePanel("Save yaml file", AssetDatabase.GetAssetPath(this), fileName, "yml");

            //Debug.Log(p);
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path + "_old")) File.Copy(path, path + "_old", true);

            Func<Entry, string> valueSelector = noValue ? e => GetPlaceholder(e.Key) : e => e.Value;
            var entries = new List<Entry>(this.entries);
            entries.Sort();
            Yaml.Export(entries, e => e.Key, valueSelector, path);

            AssetDatabase.Refresh();

            static string GetPlaceholder(string key)
            {
                var entries = key.Split('.');
                string v;
                if (entries.Length >= 2)
                {
                    if (entries[^1] == "name")
                    {
                        v = $"{entries[^2]}";
                    }
                    else
                    {
                        v = $"{entries[^2]}{entries[^1].ToTitleCase()}";
                    }
                }
                else
                {
                    v = $"{entries[0].ToTitleCase()}";
                }

                if (entries[^1] == "desc" || entries[^1] == "msg")
                {
                    v += ".";
                }
                return v;
            }
        }

        [ContextMenu("Load From Yaml")]
        public void ImportFromYaml()
        {
            var path = EditorUtility.OpenFilePanel("Select yml file", AssetDatabase.GetAssetPath(this), "yml");
            if (string.IsNullOrEmpty(path)) return;
            EditorUtility.SetDirty(this);
            entries.Clear();
            var lines = File.ReadAllLines(path);
            ImportFromYaml(lines);

            AssetDatabase.SaveAssets();
        }

        public void ImportFromYaml(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
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
        public LanguageFile CreateChildFile()
        {
            string fileName = $"Lang_{region}_";
            string path = EditorUtility.SaveFilePanel("Save yaml file", AssetDatabase.GetAssetPath(this), fileName, "asset");
            if (string.IsNullOrEmpty(path)) return null;
            int index = path.LastIndexOf('_');
            if (index == -1) index = path.LastIndexOf('/');
            string fileTag = path[(index + 1)..path.LastIndexOf('.')];
            return CreateChildFilePath(fileTag, path);
        }

        public LanguageFile CreateChildFile(string fileTag)
        {
            string fileName = $"Lang_{region}_{fileTag}.asset";
            string path = AssetDatabase.GetAssetPath(this);
            path = path[..path.LastIndexOf('/')] + "/" + fileName;
            if (string.IsNullOrEmpty(path)) return null;
            return CreateChildFilePath(fileTag, path);
        }

        private LanguageFile CreateChildFilePath(string tag, string path)
        {
            Debug.Log(path);
            var subfile = CreateInstance<LanguageFile>();
            subfile.tag = tag;
            childFiles.Add(subfile);
            EditorUtility.SetDirty(this);
            subfile.SetMasterFile(this);
            AssetDatabase.CreateAsset(subfile, "Assets" + path[Application.dataPath.Length..]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return subfile;
        }

        /// <summary>
        /// Create an (inspector) read only lang file
        /// </summary>
        /// <returns></returns> 
        public static LanguageFile NewLangFile()
        {
            var file = CreateInstance<LanguageFile>();
            file.isReadOnly = true;
            return file;
        }
#endif
    }
}