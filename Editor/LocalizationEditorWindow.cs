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
        public LocalizationDataManager fileManager;
        public LocalizationEditorSetting setting;

        public string key;
        private string referenceCountry;
        private string country;

        Window window;
        EntryDrawMode selectClass;

        DrawEntryModule drawEntry;
        CreateNewEntryModule newEntry;

        Vector2 missingEntryView;
        private Vector2 classicView;
        private PageList primaryPageList;
        private PageList pageList;



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
            //GUILayout.Space(10);
            GUILayout.Toolbar(-1, new string[] { "" });
            //GUILayout.Label("Language Manager", EditorStyles.boldLabel);
            fileManager = (LocalizationDataManager)EditorGUILayout.ObjectField("Language Manager", fileManager, typeof(LocalizationDataManager), false);
            //if (string.IsNullOrEmpty(key)) key = fileManager.topLevelDomain;
            if (!fileManager) { EndWindow(); return; }

            key = EditorGUILayout.TextField("Class Path", key);
            if (GUILayout.Button("Return")) ReturnClass();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            window = (Window)GUILayout.Toolbar((int)window, new string[] { "Reference Sheet", "Table", "Missing Entries", "Options", "Settings" }, GUILayout.Height(30));
            if (window == Window.Classic) selectClass = (EntryDrawMode)GUILayout.Toolbar((int)selectClass, new string[] { "Entry", "Class" }, GUILayout.Height(25));
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

            GUILayout.Space(50);


            EndWindow();
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
            string remove = string.Empty;
            string add = string.Empty;
            GUILayout.Label($"{fileManager.missingKeys.Count} missing entries:");
            foreach (var item in fileManager.missingKeys)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((++val).ToString(), maxWidthNum);
                bool enabled = GUI.enabled;
                bool keyExist = fileManager.HasKey(item);
                if (keyExist) GUI.enabled = false;

                if (GUILayout.Button("Add", maxWidth)) add = item;

                GUI.enabled = true;
                if (GUILayout.Button("X", maxWidthX)) remove = item;
                GUI.enabled = !keyExist;

                if (keyExist) GUILayout.Label(item + " \t(Key exist in the files already)");
                else GUILayout.Label(item);
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
            }
            if (add != string.Empty)
            {
                fileManager.AddKey(add);
                fileManager.missingKeys.Remove(add);
            }
            if (remove != string.Empty)
            {
                fileManager.missingKeys.Remove(remove);
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

        private void DrawTable()
        {
            var table = fileManager.LocalizationTable;
            var keyEntrykeyWidth = GUILayout.MaxWidth(200);

            GUILayout.BeginHorizontal();
            setting.tableUseArea = GUILayout.Toggle(setting.tableUseArea, "Use large field", keyEntrykeyWidth);
            setting.tableEntryWidth = EditorGUILayout.FloatField("W", setting.tableEntryWidth, keyEntrykeyWidth);
            setting.tableEntryHeight = EditorGUILayout.FloatField("H", setting.tableEntryHeight, keyEntrykeyWidth);
            setting.tableEntryWidth = Mathf.Max(50, setting.tableEntryWidth);
            setting.tableEntryHeight = Mathf.Max(20, setting.tableEntryHeight);
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

                GUILayout.Label(keyValPair.Key, keyEntrykeyWidth);
                foreach (var region in keyValPair.Value.Keys)
                {
                    string newText;
                    string oldText = keyValPair.Value[region];
                    if (setting.tableUseArea) newText = GUILayout.TextArea(oldText, keyEntryWidth, keyEntryHeight);
                    else newText = GUILayout.TextField(oldText, keyEntryWidth, keyEntryHeight);
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


        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings");
            setting.autoSwitchMode = EditorGUILayout.Toggle("Auto Switch Mode", setting.autoSwitchMode);
            setting.displayCount = EditorGUILayout.IntField("Display Count", setting.displayCount);
            setting.textEditorHeight = EditorGUILayout.IntField("Text Field Height", setting.textEditorHeight);
            setting.linePerPage = EditorGUILayout.IntField("Text Field Line Count", setting.linePerPage);
            setting.showSecondaryCountry = EditorGUILayout.Toggle("Show Secondary Country", setting.showSecondaryCountry);
            setting.sudo = EditorGUILayout.Toggle("No dialogue", setting.sudo);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            setting.textEditorHeight = Mathf.Max(setting.textEditorHeight, LocalizationEditorSetting.TEXT_EDITOR_DEFAULT_HEIGHT);
        }

        private void Initialize()
        {
            setting ??= LocalizationEditorSetting.GetOrCreateSettings();

            newEntry ??= new CreateNewEntryModule();
            newEntry.FileManager = fileManager;

            drawEntry ??= new DrawEntryModule();
            newEntry.FileManager = fileManager;
        }

        private void DrawPrimaryLanguage()
        {
            EditorGUILayout.BeginVertical();
            referenceCountry = GetCountry("Reference Country/Region", referenceCountry);
            primaryPageList = DrawCountry(referenceCountry, primaryPageList);
            EditorGUILayout.EndVertical();
        }

        private void DrawSecondaryLanguage()
        {
            EditorGUILayout.BeginVertical();
            country = GetCountry("Country/Region", country);
            pageList = DrawCountry(country, pageList);
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

        private static void EndWindow()
        {
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public PageList DrawCountry(string country, PageList pageList)
        {
            var file = fileManager.GetLanguageFile(country);
            if (file == null)
            {
                GUILayout.Label(country + " is not added to the File Manager");
                return pageList;
            }

            var possibleKeys = file.FindMatchedKeys(key, true);
            var first = possibleKeys.FirstOrDefault();
            if (string.IsNullOrEmpty(first)) return pageList;


            GUILayout.BeginVertical();
            var keyLength = key?.Length ?? 0;
            first = first[keyLength..^0];
            int index = first.IndexOf(".");
            if (setting.autoSwitchMode)
            {
                selectClass = possibleKeys.Count <= setting.displayCount ? EntryDrawMode.entry : EntryDrawMode.@class;
            }

            if (selectClass == EntryDrawMode.entry || index == -1)
            {
                GUILayout.Label(file.Region.ToString(), EditorStyles.boldLabel);
                pageList ??= DrawListPage(
                    possibleKeys,
                    (possibleKey) =>
                    {
                        drawEntry.Initialize(setting, key, fileManager);
                        drawEntry.File = file;
                        drawEntry.Draw(possibleKey);
                    }
                );
                ((GenericListPageList<string>)pageList).entryList = possibleKeys;
                pageList.LinesPerPage = setting.LinePerPage;
                pageList.Draw();
                //foreach (var possibleKey in possibleKeys)
                //{
                //    drawEntry.Initialize(setting, key, fileManager);
                //    drawEntry.File = file;
                //    drawEntry.Draw(possibleKey);
                //}
            }
            else
            {
                selectClass = EntryDrawMode.@class;
                ClassSelection();
            }
            GUILayout.EndVertical();
            return pageList;
        }

        void ClassSelection()
        {
            List<string> possible = fileManager.FindPossibleNextClass(key);
            foreach (var pkey in possible)
            {
                var s = GUILayout.Button(pkey, GUILayout.Height(setting.textEditorHeight));
                if (s)
                {
                    OpenSelectedClass(pkey);
                }
            }

        }

        void OpenSelectedClass(string nextClass)
        {
            if (key.EndsWith(".") || key.Length == 0)
            {
                key += nextClass;
            }
            else
            {
                key += "." + nextClass;
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
            key = string.Join('.', list);
        }



        private void DrawCreateNewEntry()
        {
            newEntry.Initialize(setting, key, fileManager);
            newEntry.Draw();
        }

        public class LanguagePackModule
        {
            protected string key;
            protected LocalizationDataManager fileManager;
            protected LocalizationEditorSetting setting;

            public string CurrentKey { get => key; set => key = value; }
            public LocalizationDataManager FileManager { get => fileManager; set => fileManager = value; }
            public LocalizationEditorSetting Seting { get => setting; set => setting = value; }

            public void Initialize(LocalizationEditorSetting setting, string key, LocalizationDataManager fileManager)
            {
                this.key = key;
                this.setting = setting;
                this.fileManager = fileManager;
            }
        }


        public class DrawEntryModule : LanguagePackModule
        {
            private LanguageFile file;
            private string changingKey;
            private string changeToKey;

            private int TextEditorHeight => setting.TextEditorHeight;
            public LanguageFile File { get => file; set => file = value; }

            public void Draw(string key)
            {
                GUILayout.BeginHorizontal();
                var keyLength = CurrentKey?.Length ?? 0;
                string partialKey = key[keyLength..^0];
                string labelName = partialKey.StartsWith('.') ? partialKey[1..partialKey.Length] : partialKey;

                string oldValue = File.Get(key);
                var value = EditorGUILayout.TextField(labelName, oldValue, GUILayout.Height(TextEditorHeight));
                if (value != oldValue)
                {
                    EditorUtility.SetDirty(File);
                    File.Write(key, value);
                }


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
                        if (!fileManager.HasKey(newKey)) fileManager.AddKeyToFiles(newKey, newKeyValue);
                        newKey = "";
                    }
                }
            }
        }
    }


}