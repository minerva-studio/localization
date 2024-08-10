﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    public class LocalizationEditorSetting : ScriptableObject
    {
        public const string SETTING_PATH = "Assets/Editor/User/Localization.asset";
        public const int TEXT_EDITOR_DEFAULT_HEIGHT = 20;
        public const int TEXT_EDITOR_MIN_LINE_PER_PAGE = 1;
        public const int TEXT_EDITOR_MAX_LINE_PER_PAGE = 40;

        public const int TABLE_ENTRY_MIN_HEIGHT = 20;
        public const int TABLE_ENTRY_MAX_HEIGHT = 250;


        static LocalizationEditorSetting()
        {
            L10n.OnKeyMissing += (key) => GetOrCreateSettings().AddMissingKey(key);
        }



        private SerializedObject obj;
        public SerializedObject serializedObject => obj ?? new SerializedObject(this);





        [Tooltip("Reference sheet will automatically show entries if there are entries fewer than display count met current partial key")]
        public bool autoSwitch;
        public int displayCount = 10;
        public int textEditorHeight = TEXT_EDITOR_DEFAULT_HEIGHT;
        [Range(TEXT_EDITOR_MIN_LINE_PER_PAGE, TEXT_EDITOR_MAX_LINE_PER_PAGE)]
        public int linePerPage = TEXT_EDITOR_MIN_LINE_PER_PAGE;
        public bool showSecondaryCountry = true;

        [Range(200, 1000)]
        public float tableEntryWidth = 200;
        [Range(TABLE_ENTRY_MIN_HEIGHT, TABLE_ENTRY_MAX_HEIGHT)]
        public float tableEntryHeight = 20;

        public bool sudo;

        [ContextMenuItem("Sort", nameof(SortMissing))]
        //[ContextMenuItem("Clear Obsolete Missing Keys", nameof(ClearObsoleteMissingKeys))]
        public List<string> missingKeys;




        public int TextEditorHeight => textEditorHeight = Mathf.Max(TEXT_EDITOR_DEFAULT_HEIGHT, textEditorHeight);
        public int LinePerPage => linePerPage = Mathf.Max(TEXT_EDITOR_MIN_LINE_PER_PAGE, linePerPage);

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        internal static LocalizationEditorSetting GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationEditorSetting>(SETTING_PATH);
            if (settings == null)
            {
                settings = CreateInstance<LocalizationEditorSetting>();
                //settings.m_Number = 42;
                //settings.m_SomeString = "The answer to the universe";
                AssetDatabase.CreateAsset(settings, SETTING_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        /// <summary>
        /// Sort all missing key
        /// </summary>
        public void SortMissing()
        {
            missingKeys.Sort();
        }


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
    }
}