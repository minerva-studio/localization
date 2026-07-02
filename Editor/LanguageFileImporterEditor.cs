using Minerva.Localizations;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace Minerva.Localizations.Editor
{
    [CustomEditor(typeof(LanguageFileImporter))]
    public class LanguageFileImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty masterFile = serializedObject.FindProperty(nameof(LanguageFileImporter.masterFile));
            EditorGUILayout.PropertyField(masterFile);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageFileImporter.tag)));

            // A child .lang file gets its region from the selected master file.
            if (!masterFile.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageFileImporter.region)));
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
