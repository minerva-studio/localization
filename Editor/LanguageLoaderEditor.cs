using Minerva.Localizations.Components;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(TextLocalizer))]
    public class TextLocalizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayoutOption height = GUILayout.Height(36);

            GUILayout.Space(10);
            TextLocalizer languageLoader = target as TextLocalizer;
            LocalizationDataManager languageFileManager = languageLoader.languageFileManager;
            string key = languageLoader.Key;
            Color currentContentColor = GUI.contentColor;
            SerializedObject obj = new SerializedObject(languageLoader);
            var property = obj.FindProperty("key");

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
                    if (!GUILayout.Button(item)) continue;

                    if (string.IsNullOrWhiteSpace(key)) property.stringValue = item;
                    else property.stringValue = key + "." + item;
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
                        languageFileManager.AddKeyToFiles(property.stringValue); 
                    }
                }
                if (GUILayout.Button("Clear", height))
                {
                    ClearKey(languageLoader);
                }
                GUILayout.EndHorizontal();
            }
            if (obj.hasModifiedProperties)
            {
                EditorUtility.SetDirty(this);
                obj.ApplyModifiedProperties();
            }
            ////GUILayout.Box("sele");
            ////GUILayout.Box("");
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