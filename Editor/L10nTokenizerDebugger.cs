using Minerva.Localizations.EscapePatterns;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    public class L10nTokenizerDebugger : EditorWindow
    {
        private string inputText = "Hello §R{damage}§ world $Item.Name$ test";
        private L10nToken token;
        private bool autoUpdate = true;

        private TreeViewState treeViewState;
        private MultiColumnHeaderState multiColumnHeaderState;
        private L10nTokenTreeView treeView;
        private SearchField searchField;

        [MenuItem("Window/Localization/Tokenizer Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<L10nTokenizerDebugger>("L10n Tokenizer Debugger");
            window.minSize = new Vector2(800, 500);
        }

        private void OnEnable()
        {
            treeViewState ??= new TreeViewState();

            var headerState = CreateHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
            multiColumnHeaderState = headerState;

            var header = new MultiColumnHeader(multiColumnHeaderState);
            header.ResizeToFit();

            treeView = new L10nTokenTreeView(treeViewState, header);
            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;

            ParseInput();
        }

        private MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "Token Type"),
                    width = 120,
                    minWidth = 80,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Content", "Token Content"),
                    width = 300,
                    minWidth = 150,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Metadata", "Additional Information"),
                    width = 150,
                    minWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Flags", "Token Flags"),
                    width = 80,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                }
            };

            return new MultiColumnHeaderState(columns);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawInputArea();
            DrawTreeView();
            DrawQuickTests();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Parse", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ParseInput();
                }

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    inputText = string.Empty;
                    token = null;
                    treeView.SetTokens(null);
                }

                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    treeView.ExpandAll();
                }

                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    treeView.CollapseAll();
                }

                GUILayout.Space(10);
                autoUpdate = GUILayout.Toggle(autoUpdate, "Auto Update", EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                // Search field
                treeView.searchString = searchField.OnToolbarGUI(treeView.searchString);
            }
        }

        private void DrawInputArea()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Input String:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            inputText = EditorGUILayout.TextArea(inputText, GUILayout.Height(80));
            bool inputChanged = EditorGUI.EndChangeCheck();

            if (inputChanged && autoUpdate)
            {
                ParseInput();
            }

            EditorGUILayout.Space(5);
        }

        private void DrawTreeView()
        {
            if (token != null && token.Children.Count > 0)
            {
                EditorGUILayout.LabelField($"Parsed {token.Children.Count} token(s):", EditorStyles.boldLabel);

                Rect treeViewRect = GUILayoutUtility.GetRect(0, position.height - 250, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                treeView.OnGUI(treeViewRect);
            }
            else
            {
                EditorGUILayout.HelpBox("No tokens parsed yet. Enter text and click Parse.", MessageType.Info);
            }
        }

        private void DrawQuickTests()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Tests:", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Color + Dynamic", GUILayout.Height(30)))
                {
                    inputText = "§R{damage:F1}§";
                    ParseInput();
                }
                if (GUILayout.Button("Nested Color", GUILayout.Height(30)))
                {
                    inputText = "§Gouter§Rinner§outer§";
                    ParseInput();
                }
                if (GUILayout.Button("Key Reference", GUILayout.Height(30)))
                {
                    inputText = "Hello $Item.Sword.name$ world";
                    ParseInput();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Mixed HTML", GUILayout.Height(30)))
                {
                    inputText = "<u>§R{damage}§</u> from $Entity.Player.name$";
                    ParseInput();
                }
                if (GUILayout.Button("Escape Chars", GUILayout.Height(30)))
                {
                    inputText = "Use \\$ to escape \\{test\\}";
                    ParseInput();
                }
                if (GUILayout.Button("Complex", GUILayout.Height(30)))
                {
                    inputText = "deal §#FF0000{damage:f1}§ §G{element}§ damage";
                    ParseInput();
                }
            }
        }

        private void ParseInput()
        {
            if (string.IsNullOrEmpty(inputText))
            {
                token = null;
                treeView.SetTokens(null);
                return;
            }

            try
            {
                var tokenizer = new L10nTokenizer(inputText);
                token = tokenizer.Tokenize();
                treeView.SetTokens(token.Children);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                token = null;
                treeView.SetTokens(null);
            }
        }
    }

    /// <summary>
    /// Multi-column TreeView for displaying L10n tokens
    /// </summary>
    internal class L10nTokenTreeView : TreeView
    {
        private List<L10nToken> tokens;
        private const float ROW_HEIGHT = 22f;

        enum ColumnId
        {
            Type,
            Content,
            Metadata,
            Flags
        }

        public L10nTokenTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = ROW_HEIGHT;
            Reload();
        }

        public void SetTokens(List<L10nToken> newTokens)
        {
            tokens = newTokens;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            if (tokens == null || tokens.Count == 0)
            {
                root.AddChild(new TreeViewItem { id = 1, depth = 0, displayName = "No tokens" });
                return root;
            }

            int idCounter = 1;
            foreach (var token in tokens)
            {
                var treeItem = BuildTreeItemRecursive(token, 0, ref idCounter);
                root.AddChild(treeItem);
            }

            return root;
        }

        private TreeViewItem BuildTreeItemRecursive(L10nToken token, int depth, ref int idCounter)
        {
            var item = new L10nTokenTreeViewItem(idCounter++, depth, token);

            if (token.Children != null && token.Children.Count > 0)
            {
                foreach (var child in token.Children)
                {
                    var childItem = BuildTreeItemRecursive(child, depth + 1, ref idCounter);
                    item.AddChild(childItem);
                }
            }

            return item;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as L10nTokenTreeViewItem;
            if (item?.Token == null)
            {
                base.RowGUI(args);
                return;
            }

            var token = item.Token;

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, L10nTokenTreeViewItem item, int column, ref RowGUIArgs args)
        {
            var token = item.Token;
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch ((ColumnId)column)
            {
                case ColumnId.Type:
                    cellRect.x += item.depth * 20f;
                    DrawTypeCell(cellRect, token, args);
                    break;

                case ColumnId.Content:
                    DrawContentCell(cellRect, token);
                    break;

                case ColumnId.Metadata:
                    DrawMetadataCell(cellRect, token);
                    break;

                case ColumnId.Flags:
                    DrawFlagsCell(cellRect, token);
                    break;
            }
        }

        private void DrawTypeCell(Rect rect, L10nToken token, RowGUIArgs args)
        {
            // Icon
            var iconRect = new Rect(rect.x + 16, rect.y, 20, rect.height);
            var icon = GetTokenIcon(token.Type);
            var iconColor = GetTokenIconColor(token.Type);

            var oldColor = GUI.color;
            GUI.color = iconColor;
            GUI.Label(iconRect, icon, EditorStyles.label);
            GUI.color = oldColor;

            // Type name
            var typeRect = new Rect(rect.x + 38, rect.y, rect.width - 38, rect.height);
            var typeColor = GetTokenTypeColor(token.Type);
            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = typeColor },
                fontStyle = FontStyle.Bold
            };
            GUI.Label(typeRect, token.Type.ToString(), style);
        }

        private void DrawContentCell(Rect rect, L10nToken token)
        {
            string content = token.Content.ToString();

            if (string.IsNullOrEmpty(content))
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.gray },
                    fontStyle = FontStyle.Italic
                };
                GUI.Label(rect, "(empty)", style);
            }
            else
            {
                if (content.Length > 60)
                    content = content.Substring(0, 57) + "...";

                var style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black }
                };

                EditorGUI.LabelField(rect, $"\"{content}\"", style);
            }
        }

        private void DrawMetadataCell(Rect rect, L10nToken token)
        {
            if (token.Metadata.Length > 0)
            {
                string metadata = token.Metadata.ToString();
                if (metadata.Length > 30)
                    metadata = metadata.Substring(0, 27) + "...";

                var style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.6f, 0.8f, 1f) }
                };
                GUI.Label(rect, metadata, style);
            }
        }

        private void DrawFlagsCell(Rect rect, L10nToken token)
        {
            if (token.IsTooltip)
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.8f, 1f) },
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(rect, "🛈 Tip", style);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as L10nTokenTreeViewItem;
            if (item?.Token != null)
            {
                ShowTokenDetails(item.Token);
            }
        }

        private void ShowTokenDetails(L10nToken token)
        {
            var window = EditorWindow.GetWindow<L10nTokenDetailWindow>("Token Details");
            window.SetToken(token);
        }

        private string GetTokenIcon(TokenType type)
        {
            return type switch
            {
                TokenType.Literal => "📄",
                TokenType.KeyReference => "🔑",
                TokenType.DynamicValue => "⚡",
                TokenType.ColorTag => "🎨",
                _ => "❓"
            };
        }

        private Color GetTokenIconColor(TokenType type)
        {
            return type switch
            {
                TokenType.Literal => new Color(0.7f, 0.7f, 0.7f),
                TokenType.KeyReference => new Color(0.4f, 0.7f, 1f),
                TokenType.DynamicValue => new Color(1f, 0.8f, 0.3f),
                TokenType.ColorTag => new Color(1f, 0.4f, 0.7f),
                _ => Color.white
            };
        }

        private Color GetTokenTypeColor(TokenType type)
        {
            if (EditorGUIUtility.isProSkin)
            {
                return type switch
                {
                    TokenType.Literal => new Color(0.8f, 0.8f, 0.8f),
                    TokenType.KeyReference => new Color(0.5f, 0.8f, 1f),
                    TokenType.DynamicValue => new Color(1f, 0.85f, 0.4f),
                    TokenType.ColorTag => new Color(1f, 0.5f, 0.8f),
                    _ => Color.white
                };
            }
            else
            {
                return type switch
                {
                    TokenType.Literal => new Color(0.3f, 0.3f, 0.3f),
                    TokenType.KeyReference => new Color(0.1f, 0.4f, 0.8f),
                    TokenType.DynamicValue => new Color(0.8f, 0.5f, 0.0f),
                    TokenType.ColorTag => new Color(0.8f, 0.2f, 0.5f),
                    _ => Color.black
                };
            }
        }
    }

    internal class L10nTokenTreeViewItem : TreeViewItem
    {
        public L10nToken Token { get; }

        public L10nTokenTreeViewItem(int id, int depth, L10nToken token) : base(id, depth)
        {
            Token = token;
            displayName = token.Type.ToString();
        }
    }

    internal class L10nTokenDetailWindow : EditorWindow
    {
        private L10nToken token;
        private Vector2 scrollPosition;

        public void SetToken(L10nToken newToken)
        {
            token = newToken;
            titleContent = new GUIContent($"Token: {newToken.Type}");
            Repaint();
        }

        private void OnGUI()
        {
            if (token == null)
            {
                EditorGUILayout.HelpBox("No token selected", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawContent();
            EditorGUILayout.Space(10);
            DrawMetadata();
            EditorGUILayout.Space(10);
            DrawChildren();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Token Type:", EditorStyles.boldLabel, GUILayout.Width(100));

                var color = GetTypeColor(token.Type);
                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(token.Type.ToString(), EditorStyles.boldLabel);
                GUI.color = oldColor;
            }

            if (token.IsTooltip)
            {
                EditorGUILayout.HelpBox("🛈 This is a Tooltip Reference", MessageType.Info);
            }
        }

        private void DrawContent()
        {
            EditorGUILayout.LabelField("Content:", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string content = token.Content.ToString();
                if (string.IsNullOrEmpty(content))
                {
                    EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.SelectableLabel(content, EditorStyles.textArea,
                        GUILayout.Height(Mathf.Max(40, content.Length / 50f * 18)));
                }
            }
        }

        private void DrawMetadata()
        {
            if (token.Metadata.Length > 0)
            {
                EditorGUILayout.LabelField("Metadata:", EditorStyles.boldLabel);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.SelectableLabel(token.Metadata.ToString(), GUILayout.Height(20));
                }
            }
        }

        private void DrawChildren()
        {
            if (token.Children != null && token.Children.Count > 0)
            {
                EditorGUILayout.LabelField($"Children ({token.Children.Count}):", EditorStyles.boldLabel);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var child in token.Children)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var color = GetTypeColor(child.Type);
                            var oldColor = GUI.color;
                            GUI.color = color;

                            EditorGUILayout.LabelField($"• {child.Type}", GUILayout.Width(120));
                            GUI.color = oldColor;

                            string childContent = child.Content.ToString();
                            if (childContent.Length > 40)
                                childContent = childContent.Substring(0, 37) + "...";
                            EditorGUILayout.LabelField($"\"{childContent}\"");
                        }
                    }
                }
            }
        }

        private Color GetTypeColor(TokenType type)
        {
            return type switch
            {
                TokenType.KeyReference => new Color(0.4f, 0.7f, 1f),
                TokenType.DynamicValue => new Color(1f, 0.8f, 0.3f),
                TokenType.ColorTag => new Color(1f, 0.4f, 0.7f),
                _ => Color.white
            };
        }
    }
}