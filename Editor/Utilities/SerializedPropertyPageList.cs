using UnityEditor;

namespace Minerva.Localizations.Editor.Utilities
{
    public class SerializedPropertyPageList : PageList
    {
        public SerializedProperty entryList;

        public override int Size => entryList.arraySize;

        public SerializedPropertyPageList(SerializedProperty entryList, int linesPerPage = 10)
        {
            windowMinWidth = 100;
            this.entryList = entryList;
            LinesPerPage = linesPerPage;
        }

        protected override void DrawElement(int index)
        {
            SerializedProperty element = entryList.GetArrayElementAtIndex(index);
            element.isExpanded = true;
            EditorGUILayout.PropertyField(element, true);
        }

        public override void AddElement()
        {
            entryList.InsertArrayElementAtIndex(Size);
        }
    }
}
