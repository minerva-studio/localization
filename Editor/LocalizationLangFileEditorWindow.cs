using UnityEditor;

namespace Minerva.Localizations.Editor
{
    public class LocalizationLangFileEditorWindow : EditorWindow
    {
        public LanguageFile file;

        public void OnGUI()
        {
            file = (LanguageFile)EditorGUILayout.ObjectField(file, typeof(LanguageFile), false);
            if (!file) return;
            if (!file.IsReadOnly) return;
        }
    }
}