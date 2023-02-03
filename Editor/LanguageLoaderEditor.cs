using Amlos.Localizations.Components;
using UnityEditor;
using UnityEngine;

namespace Amlos.Localizations.Editor
{
    [CustomEditor(typeof(TextLocalizer))]
    public class TextLocalizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorUtility.SetDirty(this);
            GUILayoutOption height = GUILayout.Height(36);

            GUILayout.Space(10);
            TextLocalizer languageLoader = target as TextLocalizer;
            LocalizationDataManager languageFileManager = languageLoader.languageFileManager;
            string key = languageLoader.Key;
            Color currentContentColor = GUI.contentColor;

            if (languageFileManager == null)
            {
                GUI.contentColor = Color.red;
                GUILayout.Label(new GUIContent("Language File Manager not found"));
            }
            else if (languageLoader.HasValidkey)
            {
                GUI.contentColor = Color.green;
                GUILayout.Label(new GUIContent("The key is valid"));
                GUI.contentColor = currentContentColor;
                foreach (var file in languageFileManager.files)
                {
                    EditorGUILayout.LabelField(file.Region.ToString(), file.Get(key));
                }
            }
            else
            {
                GUI.contentColor = Color.red;
                GUILayout.Label(new GUIContent("Current input key not found"));
            }

            GUI.contentColor = currentContentColor;

            if (languageFileManager != null)
            {
                GUILayout.Label("Possible Next Class");
                //string currentFullKey = currentKey.Contains('.') ? currentKey[..currentKey.LastIndexOf('.')] : currentKey;
                var possibleNextClass = languageFileManager.FindPossibleNextClass(key).ToArray();
                foreach (var item in possibleNextClass)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    if (GUILayout.Button(item))
                    {
                        languageLoader.Key = key + "." + item;
                    }

                }
                GUILayout.Space(10);
                if (GUILayout.Button("Back"))
                {
                    languageLoader.KeyReturnClass();
                }

                GUILayout.Label("Tools");
                GUILayout.BeginHorizontal(height);
                if (!languageLoader.HasValidkey)
                {
                    if (GUILayout.Button("Add New Key", height))
                    {
                        languageLoader.AddKeyToLanguageFiles();
                    }
                }
                if (GUILayout.Button("Clear", height))
                {
                    ClearKey(languageLoader);
                }
                GUILayout.EndHorizontal();
            }
            ////GUILayout.Box("sele");
            ////GUILayout.Box("");
            EditorUtility.ClearDirty(this);
        }

        private static void ClearKey(params TextLocalizer[] languageLoaders)
        {
            foreach (TextLocalizer languageLoader in languageLoaders)
            {
                EditorUtility.SetDirty(languageLoader);
                languageLoader.Key = "";
                languageLoader.OnValidate();
            }
        }
    }
}