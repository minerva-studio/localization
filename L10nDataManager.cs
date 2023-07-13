﻿using Minerva.Module;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KeyData = System.Collections.Generic.Dictionary<string, string>;
using Table = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace Minerva.Localizations
{
    /// <summary>
    /// Data manage class of localization system
    /// </summary>
    [CreateAssetMenu(fileName = "Localization Manager", menuName = "Localization/Localization Manager")]
    public class L10nDataManager : ScriptableObject
    {
        public string topLevelDomain;
        public bool disableEmptyEntry;
        public MissingKeySolution missingKeySolution;
        [Header("Data")]
        public List<LanguageFile> files;
        public List<string> regions;

        private const char CSV_SEPARATOR = ',';


        private string Directory
        {
            get
            {
                string v = Application.dataPath;
#if UNITY_EDITOR
                var currentPath = AssetDatabase.GetAssetPath(this).Split('/').ToList();
                currentPath.RemoveAt(0);
                if (currentPath.Count > 0) currentPath.RemoveAt(currentPath.Count - 1);
                foreach (var item in currentPath)
                {
                    v = v + "/" + item;
                }
#endif
                return v;
            }
        }

        public LanguageFile GetLanguageFile(string name)
        {
            return files.Where(item => item.Region.ToString() == name).FirstOrDefault();
        }



#if UNITY_EDITOR   

        [ContextMenuItem("Sort", nameof(SortKeyList))]
        public List<string> keyList;
        [ContextMenuItem("Sort", nameof(SortMissing))]
        [ContextMenuItem("Clear Obsolete Missing Keys", nameof(ClearObsoleteMissingKeys))]
        public List<string> missingKeys;

        private Table localizationTable;
        private Dictionary<string, Dictionary<string, SerializedProperty>> propertyTable;
        private Trie trie;
        private SerializedObject sobj;

        public SerializedObject serializedObject { get => sobj ??= new(this); }
        public Table LocalizationTable { get => localizationTable ??= GenerateTable(); set => localizationTable = value; }
        public Dictionary<string, Dictionary<string, SerializedProperty>> PropertyTable { get => propertyTable ??= GeneratePropertyTable(); set => propertyTable = value; }


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
            var localizationTable = new Table();
            UpdateKeyList();
            foreach (var key in keyList)
            {
                KeyData table = new();
                localizationTable.Add(key, table);
                foreach (var file in files)
                {
                    table.Add(file.Region, file.Get(key));
                }
            }
            return localizationTable;
        }

        private Dictionary<string, Dictionary<string, SerializedProperty>> GeneratePropertyTable()
        {
            var localizationTable = new Dictionary<string, Dictionary<string, SerializedProperty>>();
            SyncKeys();
            foreach (var key in keyList)
            {
                Dictionary<string, SerializedProperty> table = new();
                localizationTable.Add(key, table);
                foreach (var file in files)
                {
                    table.Add(file.Region, file.GetProperty(key));
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
        public void AddKey(string key, string defaultValue = "")
        {
            Debug.Log(keyList.Count);
            AddKey_Internal(key, defaultValue);
            AddKeyToTable(key, defaultValue);
            L10n.ReloadIfInitialized();
        }

        /// <summary>
        /// Add key to all files
        /// </summary>
        /// <remarks>This will refresh the localization table on the localization manager</remarks>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        public void AddKeyToFiles(string key, string defaultValue = "")
        {
            AddKey_Internal(key, defaultValue);
            RefreshTable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        private void AddKey_Internal(string key, string defaultValue)
        {
            if (!HasKey(key))
            {
                EditorUtility.SetDirty(this);
                keyList.Add(key);
                trie.Add(key);
                serializedObject.Update();
            }

            foreach (var file in files)
            {
                EditorUtility.SetDirty(file);
                if (!file.Add(key, defaultValue)) Debug.LogWarning($"File {file.name} has key '{key}' already.");
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
            if (!LocalizationTable.TryGetValue(key, out var strTable))
            {
                LocalizationTable[key] = strTable = new KeyData();
            }
            foreach (var region in regions)
            {
                strTable[region] = defaultValue;
            }

            if (!propertyTable.TryGetValue(key, out var properties))
            {
                properties = propertyTable[key] = new Dictionary<string, SerializedProperty>();
            }
            foreach (var file in files)
            {
                properties[file.Region] = file.GetProperty(key);
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
                file.MoveKey(oldKey, newKey);
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





        [ContextMenu("Update key list")]
        public void UpdateKeyList()
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
            UpdateKeyList();
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
            regions = file.cols;
            keyList = file.rows;
            LocalizationTable = file.table;
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
                    if (table.Value.TryGetValue(file.Region, out string value))
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
            foreach (var item in missingKeys.ShallowClone())
            {
                if (LocalizationTable.ContainsKey(item)) missingKeys.Remove(item);
            }
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
#endif

    }
}
