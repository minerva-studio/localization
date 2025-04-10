using Minerva.Module.Editor;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(L10nDataManager))]
    public class LocalizationManagerEditor : UnityEditor.Editor
    {
        EditorFieldDrawers.SerializedPropertyPageList pageList;
        EditorFieldDrawers.SerializedPropertyPageList missingPageList;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var height = GUILayout.Height(27);
            L10nDataManager file = (L10nDataManager)target;

            var state = GUI.enabled; GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(file), typeof(MonoScript), false);
            GUI.enabled = state;

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.topLevelDomain)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.defaultRegion)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.disableEmptyEntry)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.missingKeySolution)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.referenceImportOption)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.tooltipImportOption)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.files)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.sources)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(file.regions)));

            //SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(file.keyList));
            //pageList ??= EditorFieldDrawers.DrawListPage(serializedProperty);
            //pageList.entryList = serializedProperty;
            //pageList.OnSortList = () =>
            //{
            //    file.keyList.Sort();
            //    serializedObject.Update();
            //    EditorUtility.SetDirty(file);
            //};
            //pageList.Draw("Keys");

            //SerializedProperty serializedProperty1 = serializedObject.FindProperty(nameof(file.missingKeys));
            //missingPageList ??= EditorFieldDrawers.DrawListPage(serializedProperty1);
            //missingPageList.entryList = serializedProperty1;
            //missingPageList.Draw("Missing Keys");

            if (serializedObject.hasModifiedProperties)
            {
                EditorUtility.SetDirty(this);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }



            GUILayout.Space(10);
            if (GUILayout.Button("Sort All", height))
            {
                file.SortEntries();
            }
            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(height);

            if (GUILayout.Button("Import from .csv", height))
            {
                file.Import();
            }
            GUILayout.EndHorizontal();
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