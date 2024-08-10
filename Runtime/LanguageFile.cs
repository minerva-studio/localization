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
        [ReadOnly, SerializeField] private bool isReadOnly;
        [ReadOnly, SerializeField] private string path;
        [SerializeField] private bool isMasterFile;
        [SerializeField] private LanguageFile masterFile;
        [SerializeField] private List<LanguageFile> childFiles = new();
        [SerializeField] private string region = string.Empty;
        [SerializeField] private List<Entry> entries = new();

        public string listDelimiter;
        public string wordSpace;

        /// <summary> Region of the file represent </summary>
        public string Region { get => isMasterFile ? region : masterFile.region; }
        public string Tag { get => tag; set => tag = value; }
        public LanguageFile MasterFile { get => masterFile; set => masterFile = value; }
        public List<LanguageFile> ChildFiles { get => childFiles; }
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
        internal Dictionary<string, string> GetDictionary()
        {
            var dictionary = new Dictionary<string, string>();
            GetDictionary(dictionary);
            return dictionary;
        }

        /// <summary>
        /// get the dictionary of the language file
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, TranslationEntry> GetTranslationDictionary()
        {
            var dictionary = new Dictionary<string, TranslationEntry>();
            GetDictionary(dictionary);
            return dictionary;
        }

        void GetDictionary(IDictionary<string, string> dictionary)
        {
            int duplicate = 0;

            Import(entries);
            foreach (var item in ChildFiles)
            {
                if (!item) continue;
                Import(item.entries);
            }

            if (duplicate > 0)
            {
                Debug.LogErrorFormat("{0} duplicate keys found in the language file", duplicate);
            }

            void Import(List<Entry> entries)
            {
                if (entries == null) return;
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

        void GetDictionary(IDictionary<string, TranslationEntry> dictionary)
        {
            int duplicate = 0;

            Import(entries);
            foreach (var item in ChildFiles)
            {
                if (!item) continue;
                Import(item.entries);
            }

            if (duplicate > 0)
            {
                Debug.LogErrorFormat("{0} duplicate keys found in the language file", duplicate);
            }

            void Import(List<Entry> entries)
            {
                if (entries == null) return;
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
        private bool isFileDirty;

        public SerializedObject serializedObject { get => sobj ??= new(this); }
        internal IEnumerable<Entry> Entries
        {
            get
            {
                foreach (var item in entries)
                {
                    yield return item;
                }
                if (isMasterFile)
                {
                    foreach (var file in ChildFiles)
                    {
                        if (!file) continue;
                        foreach (var item in file.entries)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }
        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var item in entries)
                {
                    yield return item.Key;
                }
                if (isMasterFile)
                {
                    foreach (var file in ChildFiles)
                    {
                        if (!file) continue;
                        foreach (var item in file.entries)
                        {
                            yield return item.Key;
                        }
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
        public bool TryGet(string key, out string value) => TryGet(key, out value, new());

        bool TryGet(string key, out string value, HashSet<LanguageFile> files = null)
        {
            if (files.Contains(this)) throw new NotSupportedException("Child files cannot be self or its own master: " + name);
            files.Add(this);

            entries ??= new();
            value = entries.Find(e => e.Key == key)?.Value;

            if (value != null) return true;
            if (!isMasterFile) return false;

            foreach (var item in ChildFiles)
            {
                if (!item) continue;
                if (item.TryGet(key, out value, files)) return true;
            }

            value = null;
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
                haskey = ChildFiles.Any(f => f && f.HasKey(key, false));
            }
            return haskey;
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
            //if (IsReadOnly)
            //{
            //    return FileWrite(key, value, immediateSave);
            //}

            EditorUtility.SetDirty(this);
            //Debug.Log($"Write Entry " + key + " with value " + value);
            var entry = GetEntry(key);
            var oldVal = entry?.Value;
            if (entry != null)
            {
                // no change
                if (entry.Value == value) return value;
                entry.Value = value;
            }
            else
            {
                entry = new Entry(key, value);
                entries.Add(entry);
            }
            // L10n is loading current region
            if (L10n.IsLoaded && L10n.Region == region)
            {
                L10n.Write(key, value);
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
        public bool Add(string key, string value = "", bool immediateSave = false)
        {
            if (IsReadOnly)
            {
                return false;
            }

            EditorUtility.SetDirty(this);
            var entry = GetEntry(key);
            if (entry != null)
            {
                Debug.LogWarning($"Entry exist for key {key}");
                return false;
            }
            else entries.Add(new Entry(key, value));
            serializedObject.Update();
            // Debug.Log($"Add Entry " + key + " with value " + value);
            if (immediateSave) AssetDatabase.SaveAssets();
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
        public bool PutAt(string key, string fileTag, string value = "", bool updateAssets = false)
        {
            if (fileTag == tag)
            {
                return Add(key, value, updateAssets);
            }
            if (isMasterFile)
            {
                var file = ChildFiles.FirstOrDefault(f => f && f.tag == fileTag);
                if (!file) file = CreateChildFile(fileTag);
                if (!file) return false;

                return file.Add(key, value, updateAssets);
            }

            return false;
        }





        //#region .lang File Editing


        ///// <summary>
        ///// Write the value by key
        ///// <para>
        ///// If the key appears in the file or child file, it will override existing value
        ///// <br></br>
        ///// If the key has not yet appears in the file, a new entry will be added to this file
        ///// </para>
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        ///// <returns> string of the old entry, null if it is a new entry </returns>
        //public string FileWrite(string key, string value = "", bool immediateSave = false)
        //{
        //    var entry = GetEntry(key);
        //    var oldVal = entry?.Value;
        //    if (entry != null)
        //    {
        //        // no change
        //        if (entry.Value == value) return value;
        //        entry.Value = value;
        //    }
        //    else
        //    {
        //        entry = new Entry(key, value);
        //        entries.Add(entry);
        //    }
        //    Debug.Log($"Write Entry " + key + " with value " + value);
        //    // L10n is loading current region
        //    if (L10n.isInitialized && L10n.Region == region)
        //    {
        //        L10n.Override(key, value);
        //    }

        //    if (immediateSave)
        //    {
        //        FileSave();
        //    }
        //    isFileDirty = true;
        //    return oldVal;
        //}

        ///// <summary>
        ///// Add value by key
        ///// <para>
        ///// If the key appears in the file or child file, it will not override existing value, do nothing and return false.
        ///// </para>
        ///// <para>
        ///// If the key has not yet appears in the file, a new entry will be added to this file
        ///// </para>
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        ///// <returns> Whether given key is in the file already </returns>
        //public bool FileAdd(string key, string value = "", bool immediateSave = false)
        //{
        //    var entry = GetEntry(key);
        //    if (entry != null)
        //    {
        //        Debug.Log("Entry exist");
        //        return false;
        //    }
        //    else entries.Add(new Entry(key, value));
        //    isFileDirty = false;
        //    Debug.Log($"Write Entry " + key + " with value " + value);

        //    if (immediateSave) FileSave();
        //    return true;
        //}

        ///// <summary>
        ///// Save file (if is from .lang)
        ///// </summary>
        //public void FileSave()
        //{
        //    Debug.Log($"Saving {name}");
        //    ExportTo(path, noBackup: true);
        //    isFileDirty = false;
        //    AssetDatabase.Refresh();
        //}

        //public bool FileDirty()
        //{
        //    return isFileDirty;
        //}

        //#endregion






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
            foreach (var child in ChildFiles)
            {
                if (!child) continue;
                Entry entry = child.GetEntryOnSelf(key);
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

        public IEnumerable<(string, SerializedProperty)> GetProperties()
        {
            SerializedProperty entriesProperty = serializedObject.FindProperty(nameof(entries));
            for (int i = 0; i < entries.Count; i++)
            {
                Entry item = entries[i];
                var value = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("value");
                yield return (item.Key, value);
            }
            if (!isMasterFile) yield break;
            foreach (var item in childFiles)
            {
                foreach (var entry in item.GetProperties())
                {
                    yield return entry;
                }
            }
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
            if (GetProperty(index, out var tuple))
            {
                value = tuple.Item2;
                return true;
            }
            return false;
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
        public bool TryGetPropertyWithIndex(string key, out (int, SerializedProperty) value)
        {
            serializedObject.Update();
            value = default;
            var index = entries.FindIndex(e => e.Key == key);
            if (GetProperty(index, out var tuple))
            {
                value = (index, tuple.Item2);
                return true;
            }
            return false;
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
        public bool GetProperty(int index, out (string, SerializedProperty) value)
        {
            serializedObject.Update();
            value = default;

            SerializedProperty entriesProperty = serializedObject.FindProperty(nameof(entries));
            if (entriesProperty.arraySize != 0 && entriesProperty.arraySize > index)
            {
                value = (entries[index].Key, entriesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("value"));
                return true;
            }
            if (!isMasterFile) return false;

            foreach (var child in ChildFiles)
            {
                if (!child) continue;
                if (child.GetProperty(index, out value)) return true;
            }
            return false;
        }





        /// <summary>
        /// Sort entries
        /// </summary>
        /// <param name="searchInChild">also sort childs</param>
        public void Sort(bool searchInChild = false)
        {
            if (isReadOnly) return;
            entries.Sort();
            if (searchInChild) ChildFiles.ForEach(Sort);

            static void Sort(LanguageFile f)
            {
                if (f) f.Sort(false);
            }
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

            if (searchInChild)
            {
                ChildFiles.ForEach(f => RemoveKey(key, f));
            }

            if (updateAssets) AssetDatabase.SaveAssets();

            static void RemoveKey(string key, LanguageFile f)
            {
                if (f) f.RemoveKey(key, false);
            }
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
            foreach (var child in ChildFiles)
            {
                if (child) child.FindMatchedKeys_Internal(partialKey, result);
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
            foreach (var child in ChildFiles)
            {
                if (child) child.FindMatchedKeys_Internal(partialKey, result);
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
                foreach (var child in ChildFiles)
                {
                    if (child) child.SetMasterFile(this);
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

        /// <summary>
        /// Clear entries in the file
        /// </summary>
        public void Clear(bool recursive)
        {
            entries.Clear();
            if (recursive && isMasterFile)
            {
                foreach (var item in childFiles)
                {
                    if (item)
                        item.Clear(recursive);
                }
            }
        }




        private void ExportTo(string path, bool noBackup = false, bool fullKey = false)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!noBackup && File.Exists(path + "_old")) File.Copy(path, path + "_old", true);

            if (fullKey)
            {
                Yaml.ExportFullKey(entries, e => e.Key, e => e.Value, path);
            }
            else Yaml.Export(entries, e => e.Key, e => e.Value, path);

            AssetDatabase.Refresh();
        }






        [ContextMenu("Clear Duplicate Keys")]
        public void ClearDuplicateKeys()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry item = entries[i];
                for (int j = 0; j < ChildFiles.Count; j++)
                {
                    LanguageFile file = ChildFiles[j];
                    if (file && file.HasKey(item.Key))
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
            ExportTo(path);
        }

        [ContextMenu("Export to Yaml - Full key")]
        public void ExportToYamlFullKey()
        {
            string fileName = name;
            //string fileName = masterFile ? region : $"{name}-{region}";
            string path = EditorUtility.SaveFilePanel("Save yaml file", AssetDatabase.GetAssetPath(this), fileName, "yml");

            //Debug.Log(p);
            ExportTo(path, fullKey: true);
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
            var dictionary = Yaml.Import(string.Join("\n", lines));
            entries.AddRange(dictionary.Select(d => new Entry(d.Key, d.Value)));
            //for (int i = 0; i < lines.Length; i++)
            //{
            //    if (string.IsNullOrWhiteSpace(lines[i])) continue;
            //    string item = lines[i].Trim();
            //    if (string.IsNullOrEmpty(item) || item.StartsWith('#')) continue;

            //    int spliter = item.IndexOf(':');
            //    // some reason it is not right, skip line
            //    if (spliter == -1) continue;

            //    string key = item[..spliter].Trim();
            //    string value = ParseValue(item[(spliter + 1)..]);//.Trim()[1..^1];
            //    entries.Add(new Entry(key, value));
            //}
        }

        private string ParseValue(string raw)
        {
            var value = raw.Trim();
            if (QuotedBy(value, '"') || QuotedBy(value, '\''))
            {
                value = value[1..^1];
            }
            return value.Replace("\\n", "\n");

            static bool QuotedBy(string value, char c)
            {
                return value.StartsWith(c) && value.EndsWith(c);
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
            string path = Path.Join(Application.dataPath, AssetDatabase.GetAssetPath(this));
            path = $"{path[..path.LastIndexOf('/')]}/{fileName}";
            if (string.IsNullOrEmpty(path)) return null;
            return CreateChildFilePath(fileTag, path);
        }

        private LanguageFile CreateChildFilePath(string tag, string path)
        {
            Debug.Log(path);
            var subfile = CreateInstance<LanguageFile>();
            subfile.tag = tag;
            ChildFiles.Add(subfile);
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
        public static LanguageFile NewLangFile(string path)
        {
            var file = CreateInstance<LanguageFile>();
            file.isReadOnly = true;
            file.path = path;
            return file;
        }
#endif
    }
}