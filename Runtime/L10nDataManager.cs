using Minerva.Module;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using PropertyTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, UnityEditor.SerializedProperty>>;
#endif

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

            public string this[string col] { get => GetValue(col); set => table[col] = value; }

            private string GetValue(string col) => table.TryGetValue(col, out var value) ? value : table[col] = string.Empty;

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

        public string defaultRegion;
        public string topLevelDomain;
        public bool disableEmptyEntry;
        public bool importReferenceWithLinkTag;
        public MissingKeySolution missingKeySolution;
        public ReferenceImportOption referenceImportOption;
        public ReferenceImportOption tooltipImportOption;
        [Header("Data")]
        public List<LanguageFile> files;
        public List<LanguageFileSource> sources;
        public List<string> regions;


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


        #region Editor
#if UNITY_EDITOR

        private bool keyBuild = false;

        private LocalizationKeyCollection localizationKeyCollection;

        //private HashSet<string> keys;
        //private List<string> keyList;
        ///// <summary> Keys' trie </summary>
        //private Trie trie;
        /// <summary> Source's trie </summary>
        private Trie sourceTrie;


        /// <summary> Serialized properties </summary>
        private PropertyTable propertyTable;
        private SerializedObject sobj;

        /// <summary> Self as Serializable Object </summary>
        public SerializedObject serializedObject { get => sobj ??= new(this); }
        public LocalizationKeyCollection LocalizationKeyCollection => localizationKeyCollection;
        //public Table LocalizationTable { get => localizationTable ??= GenerateTable(localizationKeyCollection == null || localizationKeyCollection.Count == 0); set => localizationTable = value; }
        /// <summary> Self as Serializable Object </summary>
        public PropertyTable PropertyTable { get => propertyTable ??= GeneratePropertyTable(localizationKeyCollection == null || localizationKeyCollection.Count == 0); set => propertyTable = value; }
        //public HashSet<string> Keys { get => keyBuild ? keys ??= RebuildKeyList() : keys = RebuildKeyList(); }
        //public List<string> KeyList { get => keyList ??= new(keys); }
        public string[] FileTags
        {
            get
            {
                var fileTags = new HashSet<string>();
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.Tag)) fileTags.Add(file.Tag);
                    foreach (var child in file.ChildFiles)
                    {
                        if (child && !string.IsNullOrEmpty(child.Tag)) fileTags.Add(child.Tag);
                    }
                }
                return fileTags.ToArray();
            }
        }


        /// <summary>
        /// Refresh underlying table
        /// </summary>
        public void RefreshTable()
        {
            serializedObject.Update();
            propertyTable = GeneratePropertyTable(false);
            L10n.ReloadIfInitialized();
        }

        public Table GenerateTable(bool rebuildKeys = true)
        {
            var localizationTable = new Table(regions.ToArray());
            if (rebuildKeys) RebuildKeyList();
            foreach (var file in files)
            {
                foreach (var (key, value) in file.Entries)
                {
                    if (!localizationTable.TryGetValue(key, out var entry))
                    {
                        entry = new KeyEntry(key);
                        localizationTable[key] = entry;
                    }
                    entry[file.Region] = value;
                }
            }
            return localizationTable;
        }

        public async Task<Table> GenerateTableAsync(bool rebuildKeys = true)
        {
            if (rebuildKeys) await RebuildKeysAsync();
            var table = await Task.Run(() => GenerateTable(false));
            return table;
        }

        private PropertyTable GeneratePropertyTable(bool rebuildKeys = true)
        {
            var localizationTable = new PropertyTable();
            if (rebuildKeys) RebuildKeyList();
            foreach (var file in files)
            {
                var regionProperties = file.GetProperties();
                foreach (var (key, p) in regionProperties)
                {
                    if (!localizationTable.TryGetValue(key, out var dictionary))
                    {
                        dictionary = new Dictionary<string, SerializedProperty>();
                        localizationTable.Add(key, dictionary);
                    }
                    dictionary[file.Region] = p;
                }
            }
            Debug.Log(localizationTable.Count);
            return localizationTable;
        }




        /// <summary>
        /// Given key in source
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsInSource(string key)
        {
            if (string.IsNullOrEmpty(key)) { return true; }
            if (string.IsNullOrWhiteSpace(key)) { return true; }
            if (key.EndsWith(".")) key = key.Remove(key.Length - 1);
            return sources.Any(s => s && s.Keys.Contains(key));
        }

        /// <summary>
        /// Given key in the key list
        /// </summary>
        /// <param name="key"></param>
        /// <remarks>
        /// This method is extremely slow, because it traverse through all the entries in all the files,
        /// it is highly recommended to use <see cref="LanguageFile.GetDictionary()"/> to check
        /// and use <see cref="IsInSource(string)"/> to check whether string is already in source
        /// </remarks>
        /// <returns></returns>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) { return true; }
            if (string.IsNullOrWhiteSpace(key)) { return true; }
            if (key.EndsWith(".")) key = key.Remove(key.Length - 1);

            if (localizationKeyCollection == null || localizationKeyCollection.Count == 0)
            {
                RebuildKeyList();
            }
            return localizationKeyCollection.Contains(key);
        }

        public async Task<bool> HasKeyAysc(string key)
        {
            if (localizationKeyCollection == null || localizationKeyCollection.Count == 0)
            {
                await RebuildKeysAsync();
            }
            var result = await Task.Run(() => HasKey(key));
            return result;
        }

        /// <summary>
        /// Check given key is in the key list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="allowInSource">Whether also check string is in source</param>
        /// <remarks>
        /// This method is extremely slow, because it traverse through all the entries in all the files,
        /// it is highly recommended to use <see cref="LanguageFile.GetDictionary()"/> to check
        /// and use <see cref="IsInSource(string)"/> to check whether string is already in source
        /// </remarks>
        /// <returns></returns> 
        public bool HasKey(string key, bool allowInSource)
        {
            // assuming key should more likely in source
            return (allowInSource && IsInSource(key)) || HasKey(key);
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
            AddKey_Internal(key, tag, defaultValue);
            AddKeyToTable(key, defaultValue); // no need to refresh table?
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
            L10n.ReloadIfInitialized();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        private void AddKey_Internal(string key, string tag, string defaultValue)
        {
            foreach (var file in files)
            {
                if (!file.PutAt(key, tag, defaultValue)) Debug.LogWarning($"File {file.name} has key '{key}' already.");
            }
            serializedObject.Update();
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
                if (!propertyTable.TryGetValue(key, out var properties))
                {
                    properties = propertyTable[key] = new Dictionary<string, SerializedProperty>();
                }
                foreach (var file in files)
                {
                    properties[file.Region] = file.GetProperty(key);
                }
            }

            localizationKeyCollection.Add(key);
        }

        public void AddKeyToFile(string key, string fileTag, string defaultValue = "")
        {
            foreach (var file in files)
            {
                if (file.PutAt(key, fileTag, defaultValue, true))
                {
                    continue;
                }
                Debug.LogWarning($"File {file.name} has key '{key}' already.");
            }

            AddKeyToTable(key, defaultValue);
            L10n.ReloadIfInitialized();
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
            PropertyTable.Remove(key);
            localizationKeyCollection.Remove(key);
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
            localizationKeyCollection.Remove(oldKey);
            localizationKeyCollection.Add(newKey);
            EditorUtility.SetDirty(this);

            //table move key  
            var property = PropertyTable[oldKey] ?? throw new KeyNotFoundException();
            PropertyTable.Remove(oldKey);
            PropertyTable.Add(newKey, property);

            L10n.ReloadIfInitialized();
        }





        [ContextMenu("Rebuild key list")]
        public void RebuildKeyList()
        {
            keyBuild = false;
            localizationKeyCollection ??= new();
            localizationKeyCollection.Clear();
            foreach (var item in files)
            {
                localizationKeyCollection.UnionWith(item.Keys);
            }
            sourceTrie = new Trie(sources.Where(s => s).SelectMany(s => s.Keys));
            keyBuild = true;
        }

        public async Task RebuildKeysAsync()
        {
            keyBuild = false;
            localizationKeyCollection ??= new();
            IEnumerable<LanguageFileSource> enumerable = sources.Where(s => s);
            await Task.Run(() => Rebuild());
            keyBuild = true;
            async Task Rebuild()
            {
                localizationKeyCollection.Clear();
                foreach (var item in files)
                {
                    await localizationKeyCollection.UnionWithAsync(item.Keys);
                }
                sourceTrie = new Trie(enumerable.SelectMany(s => s.Keys));
            }
        }

        public void UpdateSources()
        {
            sourceTrie = new Trie(sources.Where(s => s).SelectMany(s => s.Keys));
        }

        public void UpdateSources(List<string> keys)
        {
            sourceTrie = new Trie(sources.Where(s => s).SelectMany(s => s.Keys));
            sourceTrie.AddRange(keys);
        }





        public void SortEntries()
        {
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
        public List<string> FindPossibleNextClass(string pKey, bool allowSource = false)
        {
            if (string.IsNullOrEmpty(pKey))
            {
                return localizationKeyCollection.FirstLevelKeys.ToList();
            }
            bool hasKey = localizationKeyCollection.TryGetSegment(pKey, out TrieSegment subTrie);
            if (!allowSource)
            {
                return hasKey ? subTrie.FirstLayerKeys.ToList() : new List<string>();
            }
            sourceTrie ??= new Trie(sources.Where(s => s).SelectMany(s => s.Keys));
            if (!sourceTrie.TryGetSegment(pKey, out var sourceSubTrie))
                return hasKey ? subTrie.FirstLayerKeys.ToList() : new List<string>();

            if (hasKey) subTrie.AddRange(sourceSubTrie);
            else subTrie = sourceSubTrie;

            return subTrie.FirstLayerKeys.ToList();
        }





        #region Data IO

        [ContextMenu("Export to CSV")]
        public void Export()
        {
            string output = CSV.ConvertToCSV("Language pack", GenerateTable());
            string p = EditorUtility.SaveFilePanel("Save Localization csv file", AssetDatabase.GetAssetPath(this), "Keys", "csv");
            //Debug.Log(p);
            if (string.IsNullOrEmpty(p)) return;
            if (!File.Exists(p)) File.Create(p).Dispose();
            if (File.Exists(p + "_old")) File.Copy(p, p + "_old", true);
            File.WriteAllText(p, output, System.Text.Encoding.UTF8);
        }

        [ContextMenu("Import from CSV")]
        public void Import()
        {
            EditorUtility.SetDirty(this);
            var path = EditorUtility.OpenFilePanel("Select Localization csv file", AssetDatabase.GetAssetPath(this), "csv");
            if (string.IsNullOrEmpty(path)) return;

            var csvFile = CSV.Import(path);
            regions = new List<string>(csvFile.cols);
            localizationKeyCollection = new LocalizationKeyCollection(csvFile.rows);

            foreach (var item in files)
            {
                EditorUtility.SetDirty(item);
            }

            foreach (var textFile in files)
            {
                textFile.Clear(true);
            }

            foreach (var table in csvFile.table)
            {
                foreach (var file in files)
                {
                    if (table.Value.TryGetValue(file.Region, out string value))
                        file.Write(table.Key, value, false);
                    else Debug.LogError($"Key {table.Key} in language {file.Region} not found");
                }
            }

            // update representation
            serializedObject.Update();
            AssetDatabase.SaveAssets();
        }
        #endregion




        public void EditorSaveSelf()
        {
            AssetDatabase.SaveAssetIfDirty(this);
            foreach (var file in files)
            {
                AssetDatabase.SaveAssetIfDirty(file);
                foreach (var child in file.ChildFiles)
                {
                    if (child) AssetDatabase.SaveAssetIfDirty(child);
                }
            }
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
                    if (child && Matches(child, fileTag))
                        return true;
            }

            return false;

            static bool Matches(LanguageFile file, string fileTag)
            {
                return file.Tag == fileTag && file.IsReadOnly;
            }
        }
#endif
        #endregion

    }
}
