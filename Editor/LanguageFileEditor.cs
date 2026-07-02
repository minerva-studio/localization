using Minerva.Localizations.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(LanguageFile))]
    public class LanguageFileEditor : UnityEditor.Editor
    {
        bool debugFold;
        SerializedPropertyPageList pageList;


        public override void OnInspectorGUI()
        {
            LanguageFile file = (LanguageFile)target;
            var height = GUILayout.Height(27);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(file), typeof(MonoScript), false);
            using (new EditorGUI.DisabledScope(!file.IsMasterFile))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("region"));

            if (file.IsMasterFile)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageFile.listDelimiter)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageFile.wordSpace)));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tag"));

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.IS_MASTER_FILE_NAME));
            if (file.IsMasterFile) if (GUILayout.Button("Create Child file")) file.CreateChildFile();
            GUILayout.EndHorizontal();


            if (!file.IsMasterFile) EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.MASTER_FILE_NAME));
            else EditorGUILayout.PropertyField(serializedObject.FindProperty(LanguageFile.CHILD_FILE_NAME));


            var entryList = serializedObject.FindProperty(LanguageFile.ENTRIES_NAME);
            pageList ??= new SerializedPropertyPageList(entryList);
            pageList.OnDrawHeader = () =>
            {
                if (GUILayout.Button("Import from Yaml", height)) file.ImportFromYaml();
                if (GUILayout.Button("Export as Yaml", height)) file.ExportToYaml();
                //if (GUILayout.Button("Export as Source Yaml", height)) file.ExportToYamlAsSource();
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
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Editor", MonoScript.FromScriptableObject(this), typeof(MonoScript), false);
                EditorGUILayout.PropertyField(entryList, true);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();


            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}
