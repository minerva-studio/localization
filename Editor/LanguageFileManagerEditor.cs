using Minerva.Module.Editor;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(LocalizationDataManager))]
    public class LocalizationManagerEditor : UnityEditor.Editor
    {
        EditorFieldDrawers.SerializedPropertyPageList pageList;
        EditorFieldDrawers.SerializedPropertyPageList missingPageList;

        public override void OnInspectorGUI()
        {
            var height = GUILayout.Height(27);
            LocalizationDataManager file = (LocalizationDataManager)target;

            var state = GUI.enabled; GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(file), typeof(MonoScript), false);
            GUI.enabled = state;

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.topLevelDomain)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.missingKeySolution)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.files)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.regions)));
            pageList ??= new EditorFieldDrawers.SerializedPropertyPageList(serializedObject.FindProperty(nameof(file.keyList)));
            pageList.Draw("Keys");

            missingPageList ??= new EditorFieldDrawers.SerializedPropertyPageList(serializedObject.FindProperty(nameof(file.missingKeys)));
            missingPageList.Draw("Missing Keys");

            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(height);

            if (GUILayout.Button("Import from .csv", height))
            {
                file.Import();
            }
            if (GUILayout.Button("Save to all Text File", height))
            {
                file.SaveToFiles();
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Import and save", height))
            {
                file.Import();
                file.SaveToFiles();
            }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal(height);
            if (GUILayout.Button("Export to .csv", height))
            {
                file.Export();
            }
            if (GUILayout.Button("Load from all Text File", height))
            {
                file.RefreshTable();
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Import and save", height))
            {
                file.RefreshTable();
                file.Export();
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            //GUILayout.BeginHorizontal(height);
            //if (GUILayout.Button("Sort missing keys", height))
            //{
            //    textFileManager.SortMissing();
            //}
            //GUILayout.EndHorizontal();
            //GUILayout.Space(10);



            //if (GUILayout.Button("Clear obsolete missing keys", height))
            //{
            //    textFileManager.ClearObsoleteMissingKeys();
            //}
            //GUILayout.Space(30); 
        }

    }
}