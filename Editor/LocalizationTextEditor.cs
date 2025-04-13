using UnityEditor;
using UnityEngine;
using static Minerva.Localizations.EscapePatterns.Regexes;


namespace Minerva.Localizations.Editor
{
    [SerializeField]
    public class LocalizationTextEditor
    {
        public bool showTools;
        public Color color;

        string result;
        TextEditor textEditor;
        bool focus;

        [SerializeField]
        private string controlName;

        public LocalizationTextEditor(string text = "editor")
        {
            this.controlName = text;
            this.color = Color.red;
        }

        public string DrawTextEditor(string desc)
        {
            GUI.SetNextControlName(controlName);
            result = EditorGUILayout.TextArea(desc, GUILayout.MinHeight(80));
            textEditor = GetTextEditor();
            // text editor not the current
            focus = textEditor.text == result;

            //showTools = EditorGUILayout.Foldout(showTools, new GUIContent("Tools"));
            //if (showTools)
            //{
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight * EditorGUI.indentLevel);
                if (GUILayout.Button("§", GUILayout.Width(20)))
                {
                    if (!focus) Focus();
                    int cursor = textEditor.cursorIndex;
                    int selection = textEditor.selectIndex;
                    if (COLOR_CODE_PATTERN.IsMatch(textEditor.SelectedText))
                    {
                        int max = Mathf.Max(cursor, selection);
                        int min = Mathf.Min(cursor, selection);
                        textEditor.text = textEditor.text[..min] + textEditor.text[(min + 8)..(max - 1)] + textEditor.text[max..];
                        if (min == selection) textEditor.cursorIndex = max - 9;
                        else textEditor.selectIndex = max - 9;
                    }
                    else if (COLOR_SIMPLE_PATTERN.IsMatch(textEditor.SelectedText))
                    {
                        int max = Mathf.Max(cursor, selection);
                        int min = Mathf.Min(cursor, selection);
                        textEditor.text = textEditor.text[..min] + textEditor.text[(min + 2)..(max - 1)] + textEditor.text[max..];
                        if (min == selection) textEditor.cursorIndex = max - 3;
                        else textEditor.selectIndex = max - 3;
                    }
                    else
                    {
                        string hexColor = ColorUtility.ToHtmlStringRGB(color);
                        Insert(textEditor, "§#" + hexColor, "§");
                    }
                }

                if (GUILayout.Button("$", GUILayout.Width(20)))
                {
                    if (!focus) Focus();
                    InsertOrRetract(textEditor, "$", "$");
                }

                if (GUILayout.Button("var", GUILayout.Width(60)))
                {
                    if (!focus) Focus();
                    InsertOrRetract(textEditor, "{", "}");
                }

                color = EditorGUILayout.ColorField("Color", color);
            }
            //}



            if (!focus)
            {
                return result;
            }
            return textEditor.text;

            void Focus()
            {
                focus = true;
                GUI.FocusControl(controlName);
                textEditor = GetTextEditor();
                textEditor.text = result;
                textEditor.cursorIndex = textEditor.selectIndex = result.Length;
            }
        }

        private string InsertOrRetract(UnityEngine.TextEditor textEditor, string begin, string end)
        {
            string result = textEditor.text;
            string selected = textEditor.SelectedText;
            if (selected.StartsWith(begin) && selected.EndsWith(end))
            {
                int cursor = textEditor.cursorIndex;
                int selection = textEditor.selectIndex;
                int max = Mathf.Max(cursor, selection);
                int min = Mathf.Min(cursor, selection);
                var newText = $"{result[..min]}{selected[begin.Length..(selected.Length - end.Length)]}{result[max..]}";
                return textEditor.text = newText;
            }
            return Insert(textEditor, begin, end);
        }

        private string Insert(UnityEngine.TextEditor textEditor, string begin, string end)
        {
            string result = textEditor.text;
            int cursor = textEditor.cursorIndex;
            int selection = textEditor.selectIndex;
            // no text selection, jump to middle
            if (cursor == selection)
            {
                int index = textEditor.cursorIndex;
                result = $"{result[..index]}{begin}{end}{result[index..]}";
                textEditor.text = result;
                textEditor.cursorIndex = textEditor.selectIndex = index + begin.Length;
            }
            else
            {
                int max = Mathf.Max(cursor, selection);
                int min = Mathf.Min(cursor, selection);
                int length = begin.Length + end.Length;
                result = $"{result[..min]}{begin}{textEditor.SelectedText}{end}{result[max..]}";
                textEditor.text = result;
                if (min == selection) textEditor.cursorIndex = max + length;
                else textEditor.selectIndex = max + length;
            }

            return result;
        }

        public UnityEngine.TextEditor GetTextEditor()
        {
            var editor = typeof(EditorGUI).GetField("s_RecycledEditor", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null);
            if (editor is UnityEngine.TextEditor textEditor)
            {
                return textEditor;
            }
            else return (UnityEngine.TextEditor)GUIUtility.GetStateObject(typeof(UnityEngine.TextEditor), GUIUtility.keyboardControl);
        }

    }
}