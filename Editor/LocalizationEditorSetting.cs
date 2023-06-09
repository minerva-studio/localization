using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    public class LocalizationEditorSetting : ScriptableObject
    {
        public const string SETTING_PATH = "Assets/Editor/User/Localization.asset";

        public bool autoSwitchMode;
        public int displayCount = 10;
        public int textEditorHeight = 20;
        public bool showSecondaryCountry = true;

        public float tableEntryWidth = 200;
        public float tableEntryHeight = 20;
        public bool tableUseArea;
        public bool sudo;

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        internal static LocalizationEditorSetting GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationEditorSetting>(SETTING_PATH);
            if (settings == null)
            {
                Debug.Log("Recreate");
                settings = CreateInstance<LocalizationEditorSetting>();
                //settings.m_Number = 42;
                //settings.m_SomeString = "The answer to the universe";
                AssetDatabase.CreateAsset(settings, SETTING_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }


}