using System;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor.Utilities
{
    /// <summary>
    /// Draws a paged editor list with optional header actions and sorting.
    /// </summary>
    public abstract class PageList
    {
        private int page;

        public Action OnDrawHeader;
        public Action OnSortList;

        public int windowMinWidth;

        private int FirstIndex => (page - 1) * LinesPerPage;
        private int MaxPage => (Size - 1) / LinesPerPage + 1;

        public int LinesPerPage { get; set; }
        public abstract int Size { get; }

        protected abstract void DrawElement(int index);

        public abstract void AddElement();

        public void Draw(string header = "Entries")
        {
            // Scroll-wheel paging mirrors the old localization editor behavior.
            if (Event.current.type == EventType.ScrollWheel)
            {
                page += Event.current.delta.y > 0 ? 1 : -1;
                page = Mathf.Min(MaxPage, Mathf.Max(0, page));
                if (EditorWindow.focusedWindow) EditorWindow.focusedWindow.Repaint();
                return;
            }

            page = Mathf.Max(Mathf.Min(MaxPage, page), 1);
            DrawHeader(header);

            using (new BackgroundColorScope(Color.white * (80 / 255f), out GUIStyle colorStyle))
            {
                GUILayout.BeginVertical(colorStyle, GUILayout.MinHeight(windowMinWidth + 60));
                EditorGUI.indentLevel++;
                for (int i = FirstIndex; i < FirstIndex + LinesPerPage && i < Size; i++)
                {
                    DrawElement(i);
                }
                EditorGUI.indentLevel--;
                GUILayout.FlexibleSpace();

                DrawPageScroll();
                DrawBottom();
                GUILayout.EndVertical();
            }
        }

        private void DrawPageScroll()
        {
            GUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(page <= 1))
            {
                if (GUILayout.Button("Last", GUILayout.MaxWidth(80))) page--;
            }

            EditorGUILayout.LabelField("Page", GUILayout.MaxWidth(30));
            if (MaxPage != 0) page = EditorGUILayout.IntSlider(page, 1, MaxPage);
            else EditorGUILayout.LabelField("-");
            GUILayout.Label($"of {MaxPage}", GUILayout.MaxWidth(40));
            page = Mathf.Max(Mathf.Min(MaxPage, page), 1);

            using (new EditorGUI.DisabledScope(page > MaxPage))
            {
                if (GUILayout.Button("Next", GUILayout.MaxWidth(80))) page++;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawHeader(string header)
        {
            if (Size == 0)
            {
                EditorGUILayout.LabelField($"{header} (-/0): ");
            }
            else
            {
                int maxIndex = Mathf.Min(Size, FirstIndex + LinesPerPage);
                EditorGUILayout.LabelField($"{header} ({FirstIndex + 1}~{maxIndex}/{Size}): ");
            }

            OnDrawHeader?.Invoke();
        }

        private void DrawBottom()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                AddElement();
            }
            if (OnSortList != null && GUILayout.Button("Sort")) OnSortList.Invoke();
            GUILayout.EndHorizontal();
        }

        private readonly struct BackgroundColorScope : IDisposable
        {
            private readonly Color lastColor;

            public BackgroundColorScope(Color color, out GUIStyle style)
            {
                lastColor = GUI.backgroundColor;
                GUI.backgroundColor = color;

                style = new GUIStyle();
                style.normal.background = Texture2D.whiteTexture;
            }

            public void Dispose()
            {
                GUI.backgroundColor = lastColor;
            }
        }
    }
}
