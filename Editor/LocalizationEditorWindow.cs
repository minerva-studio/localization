using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Minerva.Module.Editor.EditorFieldDrawers;

namespace Minerva.Localizations.Editor
{

    public class LocalizationEditorWindow : EditorWindow
    {
        public enum EntryDrawMode
        {
            entry,
            @class,
        }

        public enum Window
        {
            Classic,
            Table,
            MissingEntries,
            Options,
            Setting,
        }

        Vector2 tableScrollView;
        Vector2 scrollPos;
        public L10nDataManager fileManager;
        public LocalizationEditorSetting setting;

        private SerializedObject serializedObject => fileManager.serializedObject;

        public string key;
        private string referenceRegion;
        private string region;

        Window window;
        EntryDrawMode selectClass;

        DrawEntryModule drawEntry;
        CreateNewEntryModule newEntry;

        Vector2 missingEntryView;
        private Vector2 classicView;
        private PageList primaryPageList;
        private PageList pageList;
        private int tableBaseIndex;



        // Add menu item named "My Window" to the Window menu
        [MenuItem("Window/Localization/Language Editor")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var window = GetWindow(typeof(LocalizationEditorWindow), false, "Language Editor");
            window.name = "Language Editor";
        }

        void OnGUI()
        {
            Initialize();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            GUILayout.BeginVertical();
            //GUILayout.Space(10);
            GUILayout.Toolbar(-1, new string[] { "" });
            //GUILayout.Label("Language Manager", EditorStyles.boldLabel);
            fileManager = (L10nDataManager)EditorGUILayout.ObjectField("Language Manager", fileManager, typeof(L10nDataManager), false);
            //if (string.IsNullOrEmpty(key)) key = fileManager.topLevelDomain;
            if (!fileManager)
            {
                GUILayout.EndVertical();
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                return;
            }

            key = EditorGUILayout.TextField("Class Path", key);
            if (GUILayout.Button("Return")) ReturnClass();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            window = (Window)GUILayout.Toolbar((int)window, new string[] { "Reference Sheet", "Table", "Missing Entries", "Options", "Settings" }, GUILayout.Height(30));
            if (window == Window.Classic && !setting.autoSwitch) selectClass = (EntryDrawMode)GUILayout.Toolbar((int)selectClass, new string[] { "Entry", "Class" }, GUILayout.Height(25));
            GUILayout.EndVertical();


            switch (window)
            {
                case Window.Classic:
                    DrawClassic();
                    break;
                case Window.Table:
                    DrawTable();
                    break;
                case Window.MissingEntries:
                    DrawMissingPage();
                    break;
                case Window.Options:
                    DrawOptions();
                    break;
                case Window.Setting:
                    DrawSettings();
                    break;
                default:
                    break;
            }
            if (window == Window.Classic || window == Window.Table)
            {
                GUILayout.FlexibleSpace();
                DrawCreateNewEntry();
            }

            GUILayout.Space(30);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void OnLostFocus()
        {
            L10n.ReloadIfInitialized();
            SaveChanges();
        }

        public override void SaveChanges()
        {
            if (fileManager)
            {
                fileManager.EditorSaveSelf();
                fileManager.RefreshTable();
            }
            base.SaveChanges();
        }



        private void DrawOptions()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (GUILayout.Button("Sync keys", GUILayout.Width(240), GUILayout.Height(30)))
            {
                fileManager.SyncKeys();
            }

            if (GUILayout.Button("Refresh Localization Table", GUILayout.Width(240), GUILayout.Height(30)))
            {
                fileManager.RefreshTable();
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            GUILayout.Label("Data", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export .csv", GUILayout.Width(120), GUILayout.Height(30)))
            {
                fileManager.Export();
            }
            if (GUILayout.Button("Import .csv", GUILayout.Width(120), GUILayout.Height(30)))
            {
                fileManager.Import();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawMissingPage()
        {
            GUILayout.BeginVertical();
            if (fileManager.missingKeys.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No entry to display");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawMissingEntries();
            }
            GUILayout.EndVertical();
        }

        private void DrawMissingEntries()
        {
            missingEntryView = GUILayout.BeginScrollView(missingEntryView);
            var maxWidth = GUILayout.MaxWidth(50);
            var maxWidthX = GUILayout.MaxWidth(20);
            var maxWidthNum = GUILayout.MaxWidth(30);
            int val = 0;

            int index = -1;
            string remove = null;
            string add = null;
            GUILayout.Label($"{fileManager.missingKeys.Count} missing entries:");

            var property = serializedObject.FindProperty(nameof(fileManager.missingKeys));
            for (int i = 0; i < property.arraySize; i++)
            {
                var elementProperty = property.GetArrayElementAtIndex(i);
                string item = elementProperty.stringValue;

                GUILayout.BeginHorizontal();
                GUILayout.Label((++val).ToString(), maxWidthNum);
                bool enabled = GUI.enabled;
                bool keyExist = fileManager.HasKey(item);
                if (keyExist) GUI.enabled = false;

                if (GUILayout.Button("Add", maxWidth))
                {
                    index = i;
                    add = item;
                }

                GUI.enabled = true;
                if (GUILayout.Button("X", maxWidthX))
                {
                    index = i;
                    remove = item;
                }
                GUI.enabled = !keyExist;

                if (keyExist) GUILayout.Label(item + " \t(Key exist in the files already)");
                else GUILayout.Label(item);
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
            }
            if (add != null)
            {
                fileManager.AddKey(add);
                property.DeleteArrayElementAtIndex(index);
            }
            if (remove != null)
            {
                property.DeleteArrayElementAtIndex(index);
            }
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                EditorUtility.SetDirty(fileManager);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Resolve all missing keys"))
            {
                fileManager.ResolveAllMissingKey();
            }
            GUILayout.EndScrollView();
        }

        private void DrawClassic()
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            classicView = EditorGUILayout.BeginScrollView(classicView);
            EditorGUILayout.BeginHorizontal();
            DrawPrimaryLanguage();
            if (selectClass == EntryDrawMode.entry && setting.showSecondaryCountry) DrawSecondaryLanguage();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTable_Old()
        {
            var table = fileManager.LocalizationTable;
            var keyEntrykeyWidth = GUILayout.Width(300);

            GUILayout.BeginHorizontal();
            SerializedObject sobj = setting.serializedObject;
            EditorGUILayout.PropertyField(sobj.FindProperty(nameof(setting.tableEntryWidth)));
            EditorGUILayout.PropertyField(sobj.FindProperty(nameof(setting.tableEntryHeight)));
            if (sobj.hasModifiedProperties)
            {
                sobj.ApplyModifiedProperties();
                sobj.Update();
                EditorUtility.SetDirty(setting);
            }
            GUILayout.EndHorizontal();

            var keyEntryWidth = GUILayout.MaxWidth(setting.tableEntryWidth);
            //var keyEntryWidthDouble = GUILayout.MaxWidth(setting.tableEntryWidth * 2);
            var keyEntryHeight = GUILayout.MaxHeight(setting.tableEntryHeight);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Button("...", GUILayout.Width(EditorGUIUtility.singleLineHeight));
            GUILayout.Label("Files", keyEntrykeyWidth);
            foreach (var file in fileManager.files)
            {
                GUILayout.Label(file.Region, keyEntryWidth);
            }
            GUILayout.EndHorizontal();


            var changedVal = string.Empty;
            var changedRegion = string.Empty;
            var deleteKey = string.Empty;
            tableScrollView = GUILayout.BeginScrollView(tableScrollView);
            foreach (var keyValPair in table)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    deleteKey = keyValPair.Key;
                }

                EditorGUILayout.SelectableLabel(keyValPair.Key, keyEntrykeyWidth, keyEntryHeight);
                foreach (var region in keyValPair.Value.Keys)
                {
                    string newText;
                    string oldText = keyValPair.Value[region];
                    newText = GUILayout.TextField(oldText, keyEntryWidth, keyEntryHeight);
                    if (newText != oldText)
                    {
                        changedVal = newText;
                        changedRegion = region;
                    }
                }
                if (changedRegion != string.Empty)
                {
                    keyValPair.Value[changedRegion] = changedVal;
                    LanguageFile languageFile = fileManager.GetLanguageFile(changedRegion);
                    var oldVal = languageFile.Write(keyValPair.Key, changedVal);
                    changedVal = string.Empty;
                    changedRegion = string.Empty;
                }
                GUILayout.EndHorizontal();
            }

            if (deleteKey.Length != 0)
            {
                if (setting.sudo || EditorUtility.DisplayDialog("Delete key " + deleteKey, $"Delete key {deleteKey} from all files?", "OK", "Cancel"))
                {
                    fileManager.RemoveKey(deleteKey);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawTable()
        {
            const int HEIGHT_OFFSET = 290;
            if (setting.useOldTableStyle)
            {
                DrawTable_Old();
                return;
            }
            if (Event.current.type == EventType.ScrollWheel)
            {
                tableBaseIndex += Event.current.delta.y > 0 ? 1 : -1;
                tableBaseIndex = Mathf.Min(fileManager.PropertyTable.Count, Mathf.Max(0, tableBaseIndex));
                Repaint();
            }

            var table = fileManager.PropertyTable;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            SerializedObject sobj = setting.serializedObject;
            EditorGUILayout.PropertyField(sobj.FindProperty(nameof(setting.tableEntryWidth)));
            EditorGUILayout.PropertyField(sobj.FindProperty(nameof(setting.tableEntryHeight)));
            if (sobj.hasModifiedProperties)
            {
                sobj.ApplyModifiedProperties();
                sobj.Update();
                EditorUtility.SetDirty(setting);
            }
            GUILayout.EndHorizontal();


            // shoud display at least 1 element
            int entryCount = (int)((position.height - HEIGHT_OFFSET) / setting.tableEntryHeight);
            entryCount = Mathf.Max(1, entryCount);
            int upperTableIndex = Mathf.Min(table.Count, tableBaseIndex + entryCount);
            EditorGUILayout.LabelField($"{tableBaseIndex + 1}~{upperTableIndex} of {table.Count}");
            var keyEntrykeyWidth = GUILayout.Width(300);
            var keyEntryWidth = GUILayout.Width(setting.tableEntryWidth);
            var keyEntryHeight = GUILayout.Height(setting.tableEntryHeight);
            GUILayout.BeginHorizontal();
            tableScrollView = GUILayout.BeginScrollView(tableScrollView, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.MaxWidth(position.width));
            {
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                GUILayout.Button("...", GUILayout.Width(EditorGUIUtility.singleLineHeight));
                GUILayout.Label("Files", keyEntrykeyWidth);
                foreach (var file in fileManager.files)
                {
                    GUILayout.Label(file.Region, keyEntryWidth);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginVertical(GUILayout.Height(position.height - HEIGHT_OFFSET));
            for (int i = tableBaseIndex; i < upperTableIndex; i++)
            {
                string key = fileManager.keyList[i];
                GUILayout.BeginHorizontal(keyEntryHeight);
                // try remove
                if (GUILayout.Button("x", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    if (setting.sudo || EditorUtility.DisplayDialog("Delete key " + key, $"Delete key {key} from all files?", "OK", "Cancel"))
                    {
                        fileManager.RemoveKey(key);
                        GUILayout.EndHorizontal();
                        break;
                    }
                }

                // draw all entries
                EditorGUILayout.SelectableLabel(key, keyEntrykeyWidth, keyEntryHeight);
                var regionsValue = table[key];
                foreach (var region in fileManager.files)
                {
                    var so = regionsValue[region.Region];
                    DrawTableEntry(GUIContent.none, so, keyEntryWidth, keyEntryHeight);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            // try remove key

            GUILayout.EndScrollView();
            tableBaseIndex = (int)GUILayout.VerticalScrollbar(tableBaseIndex, entryCount, 0, table.Count, GUILayout.ExpandHeight(true));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawTableEntry(GUIContent label, SerializedProperty so, params GUILayoutOption[] options)
        {
            if (so == null)
            {
                EditorGUILayout.LabelField("===", options);
                return;
            }
            EditorGUILayout.PropertyField(so, label, options);
            if (so.serializedObject.hasModifiedProperties)
            {
                so.serializedObject.ApplyModifiedProperties();
                so.serializedObject.Update();
                EditorUtility.SetDirty(so.serializedObject.targetObject);
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Setting file", setting, typeof(LocalizationEditorSetting), false);
            SerializedObject obj = setting.serializedObject;
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            EditorGUILayout.LabelField("Reference Sheet", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.autoSwitch)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.displayCount)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.textEditorHeight)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.linePerPage)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.showSecondaryCountry)));
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            EditorGUILayout.LabelField("Table", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.tableEntryWidth)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.tableEntryHeight)));
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.useOldTableStyle)));
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(obj.FindProperty(nameof(setting.sudo)), new GUIContent("No Dialogue"));

            if (obj.hasModifiedProperties)
            {
                obj.ApplyModifiedProperties();
                obj.Update();
                EditorUtility.SetDirty(setting);
            }

            setting.textEditorHeight = Mathf.Max(setting.textEditorHeight, LocalizationEditorSetting.TEXT_EDITOR_DEFAULT_HEIGHT);
        }

        private void Initialize()
        {
            setting = LocalizationEditorSetting.GetOrCreateSettings();

            newEntry ??= new CreateNewEntryModule();
            newEntry.FileManager = fileManager;

            drawEntry ??= new DrawEntryModule();
            newEntry.FileManager = fileManager;
        }

        private void DrawPrimaryLanguage()
        {
            EditorGUILayout.BeginVertical();
            referenceRegion = GetCountry("Reference Country/Region", referenceRegion);
            primaryPageList = DrawCountry(referenceRegion, primaryPageList);
            EditorGUILayout.EndVertical();
        }

        private void DrawSecondaryLanguage()
        {
            EditorGUILayout.BeginVertical();
            region = GetCountry("Country/Region", region);
            pageList = DrawCountry(region, pageList);
            EditorGUILayout.EndVertical();
        }

        private string GetCountry(string label, string input)
        {
            var languages = fileManager.regions.ToArray();
            int index = Array.IndexOf(languages, input);
            if (index < 0) index = 0;
            index = EditorGUILayout.Popup(label, index, languages);
            return languages[index];
        }

        public PageList DrawCountry(string region, PageList pageList)
        {
            var file = fileManager.GetLanguageFile(region);
            if (file == null)
            {
                GUILayout.Label(region + " is not added to the File Manager");
                return pageList;
            }

            var possibleKeys = file.FindMatchedKeys(key, true);
            var first = possibleKeys.FirstOrDefault();
            // no key matches
            if (possibleKeys.Count == 0)
            {
                if (key.EndsWith('.'))
                {
                    key = key[..^1];
                }
                // has current key valid
                if (file.HasKey(key, true))
                {
                    possibleKeys.Add(key);
                    first = key;
                }
                //current key is not valid,return
                else return pageList;
            }


            GUILayout.BeginVertical();
            var keyLength = key?.Length ?? 0;
            bool isAtButtomLevel = first[keyLength..^0].IndexOf(".") == -1;
            if (setting.autoSwitch)
            {
                selectClass = possibleKeys.Count <= setting.displayCount ? EntryDrawMode.entry : EntryDrawMode.@class;
            }
            if (selectClass == EntryDrawMode.entry || isAtButtomLevel)
            {
                GUILayout.Label(file.Region.ToString(), EditorStyles.boldLabel);
                pageList ??= DrawListPage(possibleKeys, (possibleKey) => drawEntry.Draw(possibleKey));
                drawEntry.Initialize(setting, key, fileManager, region);
                ((GenericListPageList<string>)pageList).entryList = possibleKeys;
                pageList.LinesPerPage = setting.LinePerPage;
                pageList.Draw();
            }
            else
            {
                var possible = fileManager.FindPossibleNextClass(key);
                foreach (var pkey in possible)
                {
                    var s = GUILayout.Button(pkey, GUILayout.Height(setting.textEditorHeight));
                    if (!s) continue;
                    OpenSelectedClass(pkey);
                }
            }
            GUILayout.EndVertical();
            return pageList;
        }

        void OpenSelectedClass(string nextClass)
        {
            if (key.EndsWith(".") || key.Length == 0)
            {
                key += nextClass + ".";
            }
            else
            {
                key += "." + nextClass + ".";
            }
        }

        void ReturnClass()
        {
            if (key.EndsWith("."))
            {
                key = key.Remove(key.Length - 1);
            }
            List<string> list = key.Split('.').ToList();
            if (list.Count == 1)
            {
                key = "";
                return;
            }
            list.RemoveAt(list.Count - 1);
            key = string.Join('.', list) + '.';
        }



        private void DrawCreateNewEntry()
        {
            newEntry.Initialize(setting, key, fileManager);
            newEntry.Draw();
        }

        public class LanguagePackModule
        {
            public string region;
            public string key;
            public L10nDataManager fileManager;
            public LocalizationEditorSetting setting;

            public string CurrentKey { get => key; set => key = value; }
            public L10nDataManager FileManager { get => fileManager; set => fileManager = value; }
            public LocalizationEditorSetting Seting { get => setting; set => setting = value; }

            public void Initialize(LocalizationEditorSetting setting, string key, L10nDataManager fileManager, string region = L10n.DEFAULT_REGION)
            {
                this.key = key;
                this.setting = setting;
                this.fileManager = fileManager;
                this.region = region;
            }
        }


        public class DrawEntryModule : LanguagePackModule
        {
            private string changingKey;
            private string changeToKey;

            private int TextEditorHeight => setting.TextEditorHeight;

            public void Draw(string key)
            {
                GUILayout.BeginHorizontal();
                var keyLength = CurrentKey?.Length ?? 0;
                string partialKey = key[keyLength..^0];
                string labelName = partialKey.StartsWith('.') ? partialKey[1..partialKey.Length] : partialKey;

                SerializedProperty property = FileManager.PropertyTable[key][region];
                DrawTableEntry(new GUIContent(labelName), property);


                if (changingKey != key)
                {
                    bool tryChange = GUILayout.Button("Change", GUILayout.Height(TextEditorHeight), GUILayout.Width(60));
                    if (tryChange)
                    {
                        changingKey = key;
                        changeToKey = changingKey;
                    }
                }
                if (GUILayout.Button("Delete", GUILayout.Height(TextEditorHeight), GUILayout.Width(60)))
                {
                    FileManager.RemoveKey(key);
                }
                else if (changingKey == key)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    changeToKey = EditorGUILayout.TextField("New Key", changeToKey, GUILayout.Height(TextEditorHeight));
                    var change = GUILayout.Button("Change", GUILayout.Height(TextEditorHeight), GUILayout.Width(60));
                    var close = GUILayout.Button("Close", GUILayout.Height(TextEditorHeight), GUILayout.Width(60));
                    if (close)
                    {
                        changingKey = "";
                    }
                    if (change)
                    {
                        FileManager.MoveKey(key, changeToKey);
                    }
                }
                GUILayout.EndHorizontal();
            }


        }

        public class CreateNewEntryModule : LanguagePackModule
        {
            private bool addKey;
            private string newKey;
            private string newKeyValue;

            public bool AddKey { get => addKey; set => addKey = value; }

            public void Draw()
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                if (!addKey)
                {
                    addKey = GUILayout.Button("Add New Entry");
                    newKey = "";
                    newKeyValue = "";
                }
                else
                {
                    newKey = EditorGUILayout.TextField("Key: " + key, newKey);
                    newKeyValue = EditorGUILayout.TextField("Value:", newKeyValue);
                    var canAddNewKey = !fileManager.HasKey(newKey);
                    GUILayout.BeginHorizontal();
                    addKey = !GUILayout.Button("Close");
                    var previousGUIState = GUI.enabled;
                    GUI.enabled = canAddNewKey;
                    bool add = GUILayout.Button("Add");
                    GUI.enabled = previousGUIState;
                    GUILayout.EndHorizontal();
                    if (add)
                    {
                        newKey = key + newKey;
                        if (!fileManager.HasKey(newKey))
                        {
                            fileManager.AddKeyToFiles(newKey, newKeyValue);
                        }

                        newKey = "";
                    }
                }
            }
        }
    }


}