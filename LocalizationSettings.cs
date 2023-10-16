using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations
{
    internal class LocalizationSettings : ScriptableObject
    {
        public const string SETTING_PATH = "Assets/Resources/Localization/LocalizationSetting.asset";
        public const string RUNTIME_SETTING_PATH = "Localization/LocalizationSettings";


        public L10nDataManager manager;


        internal static LocalizationSettings GetOrCreateSettings()
        {
#if UNITY_EDITOR

            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SETTING_PATH);
            if (settings == null)
            {
                Debug.Log("Recreate");
                settings = CreateInstance<LocalizationSettings>();
                Directory.CreateDirectory(Path.GetDirectoryName(SETTING_PATH));
                AssetDatabase.CreateAsset(settings, SETTING_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;

#else
            var settings = Resources.Load(RUNTIME_SETTING_PATH) as LocalizationSettings;
            return settings;
#endif
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnInit()
        {
            var setting = GetOrCreateSettings();
            if (setting && setting.manager)
            {
                L10n.Init(setting.manager);
            }
            else
            {
                Debug.LogWarning("Localization did not auto init due to localization setting of the project");
            }
        }
    }
}