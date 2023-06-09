using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Components
{
    public class TextLocalizer : TextLocalizerBase
    {
#if UNITY_EDITOR
        public bool HasValidkey => languageFileManager.HasKey(Key) && !string.IsNullOrEmpty(Key);

        private void OnEnable()
        {
            if (!HasValidkey)
            {
                Debug.LogError("The Language Loader does not have a valid key");
            }
        }

        public void OnValidate()
        {
            textField = GetComponent<TMP_Text>();
        }


        public void KeyReturnClass()
        {
            if (Key.EndsWith("."))
            {
                Key = Key.Remove(Key.Length - 1);
            }
            if (Key.StartsWith("."))
            {
                Key = Key[1..];
            }
            var list = Key.Split('.').ToList();
            list.RemoveAt(list.Count - 1);
            Key = string.Join('.', list);
        }

        public void AddKeyToLanguageFiles()
        {
            languageFileManager.AddKeyToFiles(Key);
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Add Localization Component")]
        [MenuItem("CONTEXT/TextMeshPro/Add Localization Component")]
        public static void AddComponent(MenuCommand command)
        {
            Component body = (Component)command.context;
            body.gameObject.AddComponent<TextLocalizer>();
        }
#endif
    }
}
