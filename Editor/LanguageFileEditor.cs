using Minerva.Module.Editor;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(LanguageFile))]
    public class LanguageFileEditor : UnityEditor.Editor
    {
        bool debugFold;
        EditorFieldDrawers.PageList pageList;


        public override void OnInspectorGUI()
        {
            LanguageFile file = (LanguageFile)target;
            var height = GUILayout.Height(27);

            GUILayout.FlexibleSpace();
            var state = GUI.enabled; GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(file), typeof(MonoScript), false);
            GUI.enabled = state;
            GUI.enabled = file.IsMasterFile;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("region")); GUI.enabled = state;

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.IS_MASTER_FILE_NAME));
            if (file.IsMasterFile) if (GUILayout.Button("Create Child file")) file.CreateChildFile();
            GUILayout.EndHorizontal();


            if (!file.IsMasterFile) EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.MASTER_FILE_NAME));
            else EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.CHILD_FILE_NAME));


            var entryList = serializedObject.FindProperty(LanguageFile.ENTRIES_NAME);
            pageList ??= EditorFieldDrawers.DrawListPage(entryList);
            pageList.OnDrawHeader = () =>
            {
                if (GUILayout.Button("Import from Yaml", height)) file.ImportFromYaml();
                if (GUILayout.Button("Export as Yaml", height)) file.ExportToYaml();
            };
            pageList.OnSortList = () =>
            {
                file.Sort();
            };
            pageList.Draw();


            debugFold = EditorGUILayout.Foldout(debugFold, "Debug");
            EditorGUI.indentLevel++;
            if (debugFold)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Editor", MonoScript.FromScriptableObject(this), typeof(MonoScript), false);
                GUI.enabled = state;
                EditorGUILayout.PropertyField(entryList, true);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();


            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}