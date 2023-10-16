using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations
{
    public class LocalizationSettings : ScriptableObject
    {
        public const string SETTING_PATH = "Assets/Resources/" + RUNTIME_SETTING_PATH + ".asset";
        public const string RUNTIME_SETTING_PATH = "Localization/LocalizationSetting";


        public L10nDataManager manager;


#if UNITY_EDITOR

        internal static LocalizationSettings GetOrCreateSettings()
        {
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
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#else
        internal static LocalizationSettings GetOrCreateSettings()
        {
            var settings = Resources.Load(RUNTIME_SETTING_PATH) as LocalizationSettings;
            return settings;
        }
#endif



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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