using Minerva.Module;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations
{


    /// <summary>
    /// Data manage class of localization system
    /// </summary>
    [CreateAssetMenu(fileName = "Localization Manager", menuName = "Localization/Localization Manager")]
    public class L10nDataManager : ScriptableObject
    {
        public class Table : IEnumerable<KeyValuePair<string, KeyEntry>>, ITable
        {
            Dictionary<string, KeyEntry> entries;
            string[] region;

            public int Count => entries.Count;
            public string[] ColumnNames => region;
            public string[] RowNames => entries.Keys.ToArray();
            IRow ITable.this[string row] { get => entries[row]; }

            public Table(string[] region)
            {
                this.entries = new Dictionary<string, KeyEntry>();
                this.region = region;
            }


            public KeyEntry this[string key]
            {
                get => entries[key];
                set => entries[key] = value;
            }

            public string this[string key, string region]
            {
                get => entries[key].table[region];
                set => entries[key].table[region] = value;
            }

            public IEnumerator<KeyValuePair<string, KeyEntry>> GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<string, KeyEntry>>)entries).GetEnumerator();
            }

            IEnumerator<IRow> IEnumerable<IRow>.GetEnumerator()
            {
                foreach (var item in entries.Values)
                {
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)entries).GetEnumerator();
            }

            public bool Remove(string key)
            {
                return entries.Remove(key);
            }

            public void Add(string key, KeyEntry entry)
            {
                entries.Add(key, entry);
            }

            public bool ContainsKey(string key)
            {
                return entries.ContainsKey(key);
            }

            public bool TryGetValue(string key, out KeyEntry strTable)
            {
                return entries.TryGetValue(key, out strTable);
            }

            public KeyEntry GetOrCreateRow(string rowName)
            {
                if (TryGetValue(rowName, out var value)) return value;
                value = new KeyEntry(rowName);
                this[rowName] = value;
                return value;
            }

            IRow ITable.GetOrCreateRow(string rowName)
            {
                return GetOrCreateRow(rowName);
            }
        }

        public class KeyEntry : IRow
        {
            public string key;
            public string fromFile;
            public Dictionary<string, string> table = new();

            public KeyEntry(string key)
            {
                this.key = key;
            }

            public string this[string col] { get => table[col]; set => table[col] = value; }
            public string Name => key;
            public int Count => table.Count;

            public IEnumerator<string> GetEnumerator()
            {
                return table.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return table.Values.GetEnumerator();
            }
        }

        public string topLevelDomain;
        public bool disableEmptyEntry;
        public MissingKeySolution missingKeySolution;
        [Header("Data")]
        public List<LanguageFile> files;
        public List<string> regions;

        private const char CSV_SEPARATOR = ',';

        /// <summary>
        /// Get main language file by given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public LanguageFile GetLanguageFile(string region)
        {
            return files
                .Where(item => item.Region.ToString() == region)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get language file by given region
        /// </summary>
        /// <param name="region"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public LanguageFile GetLanguageFile(string region, string tag)
        {
            var main = GetLanguageFile(region);
            if (main.Tag == tag)
            {
                return main;
            }
            return main.ChildFiles
                   .Where(item => item.Tag == tag)
                   .FirstOrDefault();
        }



#if UNITY_EDITOR   

        [ContextMenuItem("Sort", nameof(SortKeyList))]
        public List<string> keyList;
        [ContextMenuItem("Sort", nameof(SortMissing))]
        [ContextMenuItem("Clear Obsolete Missing Keys", nameof(ClearObsoleteMissingKeys))]
        public List<string> missingKeys;

        /// <summary> Table of [key][region] </summary>
        private Table localizationTable;
        /// <summary> Serialized properties </summary>
        private Dictionary<string, Dictionary<string, SerializedProperty>> propertyTable;
        /// <summary> Keys' trie </summary>
        private Trie trie;
        private SerializedObject sobj;

        public SerializedObject serializedObject { get => sobj ??= new(this); }
        public Table LocalizationTable { get => localizationTable ??= GenerateTable(); set => localizationTable = value; }
        public Dictionary<string, Dictionary<string, SerializedProperty>> PropertyTable { get => propertyTable ??= GeneratePropertyTable(); set => propertyTable = value; }


        /// <summary>
        /// Refresh underlying table
        /// </summary>
        public void RefreshTable()
        {
            serializedObject.Update();
            localizationTable = GenerateTable();
            propertyTable = GeneratePropertyTable();
            trie = new Trie(keyList);
            L10n.ReloadIfInitialized();
        }

        private Table GenerateTable()
        {
            var localizationTable = new Table(regions.ToArray());
            RebuildKeyList();
            foreach (var key in keyList)
            {
                KeyEntry entry = new KeyEntry(key);
                localizationTable.Add(key, entry);
                foreach (var file in files)
                {
                    entry.table.Add(file.Region, file.Get(key));
                }
            }
            return localizationTable;
        }

        private Dictionary<string, Dictionary<string, SerializedProperty>> GeneratePropertyTable()
        {
            var localizationTable = new Dictionary<string, Dictionary<string, SerializedProperty>>();
            RebuildKeyList();
            foreach (var key in keyList)
            {
                Dictionary<string, SerializedProperty> table = new();
                localizationTable.Add(key, table);
                foreach (var file in files)
                {
                    SerializedProperty value = file.GetProperty(key);
                    if (value != null) table.Add(file.Region, value);
                }
            }
            return localizationTable;
        }




        /// <summary>
        /// Given key in the key list
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) { return true; }
            if (string.IsNullOrWhiteSpace(key)) { return true; }
            if (key.EndsWith(".")) key = key.Remove(key.Length - 1);
            return keyList.Contains(key);
        }

        /// <summary>
        /// Add key to all files
        /// </summary>
        /// <remarks>This will update but NOT refresh the entire localization table</remarks>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public void AddKey(string key, string tag = "Main", string defaultValue = "")
        {
            Debug.Log(keyList.Count);
            AddKey_Internal(key, tag, defaultValue);
            AddKeyToTable(key, defaultValue);
            L10n.ReloadIfInitialized();
        }

        /// <summary>
        /// Add key to all files
        /// </summary>
        /// <remarks>This will refresh the localization table on the localization manager</remarks>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        public void AddKeyToFiles(string key, string tag = "Main", string defaultValue = "")
        {
            AddKey_Internal(key, tag, defaultValue);
            RefreshTable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        private void AddKey_Internal(string key, string tag, string defaultValue)
        {
            bool hasKey = !HasKey(key);
            if (hasKey)
            {
                EditorUtility.SetDirty(this);
                trie?.Add(key);
            }

            foreach (var file in files)
            {
                if (!file.PutAt(key, tag, defaultValue)) Debug.LogWarning($"File {file.name} has key '{key}' already.");
            }

            if (hasKey)
            {
                keyList.Add(key);
                serializedObject.Update();
            }
        }

        /// <summary>
        /// Add key to localization table
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private void AddKeyToTable(string key, string defaultValue = "")
        {
            if (propertyTable != null)
            {
                if (!LocalizationTable.TryGetValue(key, out var strTable))
                {
                    LocalizationTable[key] = strTable = new KeyEntry(key);
                }
                foreach (var region in regions)
                {
                    strTable.table[region] = defaultValue;
                }
            }

            if (propertyTable != null)
            {
                if (!propertyTable.TryGetValue(key, out var properties))
                {
                    properties = propertyTable[key] = new Dictionary<string, SerializedProperty>();
                }
                foreach (var file in files)
                {
                    properties[file.Region] = file.GetProperty(key);
                }
            }
        }

        public void AddKeyToFile(string key, string fileTag, string defaultValue = "")
        {
            Debug.Log("Add");
            bool hasKey = !HasKey(key);
            if (hasKey)
            {
                EditorUtility.SetDirty(this);
                trie?.Add(key);
            }

            foreach (var file in files)
            {
                if (file.PutAt(key, fileTag, defaultValue))
                {
                    continue;
                }
                Debug.LogWarning($"File {file.name} has key '{key}' already.");
            }

            AddKeyToTable(key, defaultValue);
            L10n.ReloadIfInitialized();
            if (hasKey)
            {
                keyList.Add(key);
                serializedObject.Update();
            }
        }



        /// <summary>
        /// remove key from all files
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveKey(string key)
        {
            //remove key from all file
            foreach (var file in files)
            {
                file.RemoveKey(key);
            }

            //remove key from localization table
            LocalizationTable.Remove(key);
            PropertyTable.Remove(key);
            keyList.Remove(key);
            trie.Remove(key);
            EditorUtility.SetDirty(this);

            L10n.ReloadIfInitialized();
            return true;
        }

        /// <summary>
        /// move a entry to a new key
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        /// <returns></returns>
        public void MoveKey(string oldKey, string newKey)
        {
            //file move key
            foreach (var file in files)
            {
                EditorUtility.SetDirty(file);
                file.MoveKey(oldKey, newKey, true, true);
            }

            //keylist move
            keyList.Remove(oldKey);
            keyList.Add(newKey);
            trie.Remove(oldKey);
            trie.Add(newKey);
            EditorUtility.SetDirty(this);

            //table move key
            var singleWordPair = LocalizationTable[oldKey] ?? throw new KeyNotFoundException();
            LocalizationTable.Remove(oldKey);
            LocalizationTable.Add(newKey, singleWordPair);

            var property = PropertyTable[oldKey] ?? throw new KeyNotFoundException();
            PropertyTable.Remove(oldKey);
            PropertyTable.Add(newKey, property);

            L10n.ReloadIfInitialized();
        }





        [ContextMenu("Rebuild key list")]
        public void RebuildKeyList()
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var item in files)
            {
                keys.UnionWith(item.Keys);
            }
            keyList = keys.ToList();
            trie = new Trie(keyList);
        }

        [ContextMenu("Sync key list")]
        public void SyncKeys()
        {
            EditorUtility.SetDirty(this);
            RebuildKeyList();
            foreach (var file in files)
            {
                foreach (var keys in keyList)
                {
                    file.Add(keys);
                }
            }
        }





        /// <summary>
        /// Sort key list
        /// </summary>
        public void SortKeyList()
        {
            keyList.Sort();
            EditorUtility.SetDirty(this);
        }

        public void SortEntries()
        {
            SortKeyList();
            foreach (var item in files)
            {
                item.Sort(true);
                EditorUtility.SetDirty(item);
            }
        }

        /// <summary>
        /// Find all keys that matches with this partial keys 
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public List<string> FindPossibleNextClass(string pKey)
        {
            trie ??= new(keyList);
            if (string.IsNullOrEmpty(pKey))
            {
                return trie.FirstLevelKeys.ToList();
            }
            bool hasKey = trie.TryGetSubTrie(pKey, out Trie subTrie);
            return hasKey ? subTrie.GetChildrenKeys() : new List<string>();
        }





        #region Data IO

        [ContextMenu("Export to CSV")]
        public void Export()
        {
            RefreshTable();
            string output = CSV.ConvertToCSV("Language pack", localizationTable);
            string p = EditorUtility.SaveFilePanel("Save Localization csv file", AssetDatabase.GetAssetPath(this), "Keys", "csv");
            //Debug.Log(p);
            if (string.IsNullOrEmpty(p)) return;
            if (!File.Exists(p)) File.Create(p);
            if (File.Exists(p + "_old")) File.Copy(p, p + "_old", true);
            File.WriteAllText(p, output, System.Text.Encoding.UTF8);
        }

        [ContextMenu("Import from CSV")]
        public void Import()
        {
            EditorUtility.SetDirty(this);
            var path = EditorUtility.OpenFilePanel("Select Localization csv file", AssetDatabase.GetAssetPath(this), "csv");
            if (string.IsNullOrEmpty(path)) return;

            var file = CSV.Import(path);
            regions = new List<string>(file.cols);
            keyList = new List<string>(file.rows);
            localizationTable = new(file.cols);
            ITable.Convert(file, localizationTable);
        }

        [ContextMenu("Save to all small file")]
        public void SaveToFiles()
        {
            foreach (var item in files)
            {
                EditorUtility.SetDirty(item);
            }

            foreach (var textFile in files)
            {
                textFile.Clear();
            }

            foreach (var table in LocalizationTable)
            {
                foreach (var file in files)
                {
                    if (table.Value.table.TryGetValue(file.Region, out string value))
                        file.Write(table.Key, value, false);
                    else Debug.LogError($"Key {table.Key} in language {file.Region} not found");
                }
            }
            AssetDatabase.SaveAssets();
        }
        #endregion





        /// <summary>
        /// Add a missing key
        /// </summary>
        /// <param name="key"></param>
        public void AddMissingKey(string key)
        {
            if (missingKeys.Contains(key)) return;
            if (string.IsNullOrEmpty(key)) return;

            missingKeys.Add(key);
        }

        /// <summary>
        /// Sort all missing key
        /// </summary>
        public void SortMissing()
        {
            missingKeys.Sort();
        }

        /// <summary>
        /// Clear the obsolete missing keys from the missing keys entry
        /// </summary>
        public void ClearObsoleteMissingKeys()
        {
            missingKeys.RemoveAll(key => string.IsNullOrEmpty(key) || LocalizationTable.ContainsKey(key));
        }

        /// <summary>
        /// Add all missing keys to the table
        /// </summary>
        [ContextMenu("Add all missing keys to files")]
        public void ResolveAllMissingKey()
        {
            EditorUtility.SetDirty(this);
            foreach (var key in missingKeys)
            {
                foreach (var file in files)
                {
                    if (!file.Add(key))
                    {
                        Debug.LogWarning($"Key {key} appears to be missing but is in file {file.name}");
                    }
                }
            }
            missingKeys.Clear();
            RefreshTable();
        }

        public void EditorSaveSelf()
        {
            AssetDatabase.SaveAssetIfDirty(this);
            foreach (var file in files)
            {
                AssetDatabase.SaveAssetIfDirty(file);
                foreach (var child in file.ChildFiles)
                {
                    AssetDatabase.SaveAssetIfDirty(child);
                }
            }
        }

        public string[] GetFileTags()
        {
            var fileTags = new HashSet<string>();
            foreach (var file in files)
            {
                fileTags.Add(file.Tag);
                foreach (var child in file.ChildFiles)
                {
                    fileTags.Add(child.Tag);
                }
            }
            return fileTags.ToArray();
        }

        public bool IsFileReadOnly(string fileTag)
        {
            foreach (var file in files)
            {
                if (Matches(file, fileTag))
                    return true;
            }
            foreach (var file in files)
            {
                foreach (var child in file.ChildFiles)
                    if (Matches(child, fileTag))
                        return true;
            }

            return false;

            static bool Matches(LanguageFile file, string fileTag)
            {
                return file.Tag == fileTag && file.IsReadOnly;
            }
        }
#endif

    }
}
