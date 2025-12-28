using Minerva.Localizations.Components;
using Minerva.Module.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(TextLocalizerBase), true)]
    public class TextLocalizerEditor : UnityEditor.Editor
    {
        private bool entryFoldout = true;
        public static L10nDataManager Manager => LocalizationSettings.GetOrCreateSettings().manager;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayoutOption height = GUILayout.Height(36);

            GUILayout.Space(10);
            TextLocalizerBase localizer = target as TextLocalizerBase;
            string key = localizer.key;
            Color currentContentColor = GUI.contentColor;
            SerializedObject obj = new SerializedObject(localizer);
            var property = obj.FindProperty("key");

            if (Manager == null)
            {
                using (GUIContentColor.By(Color.red))
                    GUILayout.Label(new GUIContent("Language File Manager not found"));
            }
            else if (!string.IsNullOrEmpty(key))
            {
                if (HasValidkey(localizer))
                {
                    using (GUIContentColor.By(Color.green))
                        GUILayout.Label(new GUIContent("The key is valid"));
                    if (entryFoldout = EditorGUILayout.Foldout(entryFoldout, "Entries"))
                        foreach (var file in Manager.files)
                        {
                            EditorGUILayout.LabelField(file.Region.ToString(), file.Get(key));
                        }
                }
                else if (HasValidkeyInSource(localizer))
                {
                    using (GUIContentColor.By(Color.yellow))
                        GUILayout.Label(new GUIContent("Current input key is in source, but the translation is not given yet"));
                    // none
                    if (Manager.files.All(f => !f.HasKey(key, true)))
                    {
                    }
                    else if (entryFoldout = EditorGUILayout.Foldout(entryFoldout, "Entries"))
                    {
                        foreach (var file in Manager.files)
                        {
                            if (file.TryGet(key, out var content))
                            {
                                EditorGUILayout.LabelField(file.Region.ToString(), content);
                            }
                            else
                            {
                                using (GUIContentColor.By(Color.red))
                                    EditorGUILayout.LabelField(file.Region.ToString(), "!!!MISSING!!!");
                            }
                        }
                    }
                }
                else
                {
                    using (GUIContentColor.By(Color.red))
                        GUILayout.Label(new GUIContent("Current input key not found"));
                }
            }


            if (Manager != null)
            {
                GUILayout.Label("Possible Next Class");
                //string currentFullKey = currentKey.Contains('.') ? currentKey[..currentKey.LastIndexOf('.')] : currentKey;
                var possibleNextClass = Manager.FindPossibleNextClass(key, true).ToArray();
                foreach (var item in possibleNextClass)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    if (!GUILayout.Button(item)) continue;

                    property.stringValue = string.IsNullOrWhiteSpace(key) ? item : key + "." + item;
                    GUI.FocusControl(null);
                    EditorUtility.SetDirty(localizer);
                }
                GUILayout.Space(10);
                if (GUILayout.Button("Back"))
                {
                    localizer.key = KeyReturnClass(localizer.key);
                }

                GUILayout.Label("Tools");
                using (new GUILayout.HorizontalScope(height))
                {
                    if (!HasValidkey(localizer) && GUILayout.Button("Add New Key", height))
                    {
                        var menu = new GenericMenu();
                        foreach (var tag in Manager.FileTags)
                        {
                            GUIContent content = new GUIContent(tag);
                            if (Manager.GetLanguageFile(L10n.DEFAULT_REGION, tag) is LanguageFile file && !file.IsReadOnly)
                                menu.AddItem(content, false, () => Manager.AddKeyToFile(property.stringValue, tag));
                            else menu.AddDisabledItem(content, false);

                        }
                        menu.ShowAsContext();
                    }
                    if (GUILayout.Button("Clear", height))
                    {
                        ClearKey(localizer);
                    }
                }
            }
            if (obj.hasModifiedProperties)
            {
                EditorUtility.SetDirty(this);
                obj.ApplyModifiedProperties();
            }
        }

        private static string KeyReturnClass(string key)
        {
            if (key.EndsWith("."))
            {
                key = key.Remove(key.Length - 1);
            }
            if (key.StartsWith("."))
            {
                key = key[1..];
            }
            var list = key.Split('.').ToList();
            list.RemoveAt(list.Count - 1);
            key = string.Join('.', list);
            return key;
        }

        private static bool HasValidkey(TextLocalizerBase textLocalizer)
        {
            return Manager && Manager.HasKey(textLocalizer.key) && !string.IsNullOrEmpty(textLocalizer.key) && Manager.files.All(f => f.HasKey(textLocalizer.key, true));
        }

        private static bool HasValidkeyInSource(TextLocalizerBase textLocalizer)
        {
            return Manager && Manager.IsInSource(textLocalizer.key) && !string.IsNullOrEmpty(textLocalizer.key);
        }

        private static void ClearKey(params TextLocalizerBase[] languageLoaders)
        {
            foreach (TextLocalizerBase languageLoader in languageLoaders)
            {
                EditorUtility.SetDirty(languageLoader);
                languageLoader.key = "";
            }
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Add Localization Component")]
        [MenuItem("CONTEXT/TextMeshPro/Add Localization Component")]
        [MenuItem("CONTEXT/Text/Add Localization Component")]
        public static void AddComponent(MenuCommand command)
        {
            Component body = (Component)command.context;
            if (body.GetComponent<Text>())
            {
                body.gameObject.AddComponent<TextLocalizerLegacyText>();
            }
            else body.gameObject.AddComponent<TextLocalizer>();
        }
    }
}