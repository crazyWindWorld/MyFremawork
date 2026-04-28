#if  UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using NetFramework.RedDot.RunTime;

namespace NetFramework.RedDot.Editor
{
    public class RedDotTreeEditor : EditorWindow
    {
        private Dictionary<RedDotNodeBase, bool> nodeFoldStates = new Dictionary<RedDotNodeBase, bool>();
        private Vector2 scrollPos;
        private bool m_showTree = true;
        private string m_selectedItem = "树状结构";
        private string m_search = "";
        private string m_lastSearch = "";

        private List<RedDotNodeBase> m_searchNodes = new List<RedDotNodeBase>();

        [MenuItem("Tools/RedDotTree/红点树编辑器 #R")]
        public static void ShowWindow()
        {
            var window = GetWindow<RedDotTreeEditor>("红点树编辑器");
            window.minSize = new Vector2(400, 600);
            window.maxSize = new Vector2(1200, 600);
        }

        private GUIStyle nodeStyle;

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("当前编辑器未处于播放状态!!!", MessageType.Error);
                return;
            }

            // 在OnGUI初始化时添加：
            nodeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(5, 5, 2, 2),
                padding = new RectOffset(10, 10, 5, 5)
            };
            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(new GUIContent(m_selectedItem), FocusType.Keyboard,
                    GUILayout.Width(200)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("树状结构"), m_selectedItem == "树状结构", OnOptionSelected, "树状结构");
                menu.AddItem(new GUIContent("路径结构"), m_selectedItem == "路径结构", OnOptionSelected, "路径结构");
                menu.ShowAsContext();
            }


            m_search = EditorGUILayout.TextField(m_search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_close"), GUILayout.Height(18),
                    GUILayout.Width(40)))
            {
                m_search = "";
            }

            if (GUI.changed)
            {
                if (m_lastSearch != m_search)
                {
                    m_lastSearch = m_search;
                    if (!string.IsNullOrEmpty(m_search))
                    {
                        m_searchNodes.Clear();
                        SearchNode(ref m_searchNodes, m_search, RedDotTree.Instance.Root);
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            if (string.IsNullOrEmpty(m_search))
            {
                switch (m_selectedItem)
                {
                    case "树状结构":
                        m_showTree = true;
                        break;
                    case "路径结构":
                        m_showTree = false;
                        break;
                }

                if (m_showTree)
                {
                    DrawTree();
                }
                else
                {
                    DrawPath();
                }
            }
            else
            {
                DrawSearchNode();
            }
        }

        private void OnOptionSelected(object option)
        {
            m_selectedItem = option.ToString();
        }

        private void SearchNode(ref List<RedDotNodeBase> nodes, string search, RedDotNodeBase node)
        {
            if (node.NodeName.Contains(search))
            {
                nodes.Add(node);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    SearchNode(ref nodes, search, child.Value);
                }
            }
        }

        private void DrawTree()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawTreeNode(RedDotTree.Instance.Root, 0);
            EditorGUILayout.EndScrollView();
        }

        private void DrawPath()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawPathNode(RedDotTree.Instance.Root, "");
            EditorGUILayout.EndScrollView();
        }

        void DrawTreeNode(RedDotNodeBase node, int indentLevel)
        {
            if (node == null) return;
            EditorGUILayout.BeginHorizontal(nodeStyle);

            // 缩进处理
            GUILayout.Space(indentLevel * 20);

            // 折叠箭头
            bool isExpanded = false;
            if (node.Children != null && node.Children.Count > 0)
            {
                isExpanded = nodeFoldStates.ContainsKey(node) ? nodeFoldStates[node] : false;
                if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20)))
                {
                    nodeFoldStates[node] = !isExpanded;
                }
            }
            else
            {
                GUILayout.Space(25); // 对齐叶子节点
            }

            DrawItem(node);
            EditorGUILayout.EndHorizontal();

            // 绘制子节点
            if (isExpanded && node.Children != null)
            {
                foreach (var child in node.Children.Values)
                {
                    DrawTreeNode(child, indentLevel + 1);
                }
            }
        }

        void DrawPathNode(RedDotNodeBase node, string parentPath)
        {
            if (node == null) return;
            EditorGUILayout.BeginHorizontal(nodeStyle);
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentPath += "/" + node.NodeName;
            }
            else
            {
                parentPath = node.NodeName;
            }

            DrawItem(node);
            EditorGUILayout.EndHorizontal();

            // 绘制子节点
            if (node.Children != null)
            {
                foreach (var child in node.Children.Values)
                {
                    DrawPathNode(child, parentPath);
                }
            }
        }

        void DrawSearchNode()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < m_searchNodes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(nodeStyle);
                var node = m_searchNodes[i];
                DrawItem(node);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawItem(RedDotNodeBase node)
        {
            GUI.color = GetColor(node);
            EditorGUILayout.LabelField(node.NodeName, GUILayout.Width(150));
            // 数量编辑
            if (node.Children == null || node.Children.Count == 0)
            {
                if (node is RedDotViewNode viewNode)
                {
                    EditorGUILayout.LabelField(viewNode.Viewed ? "已查看" : "待查看");
                    GUI.color = Color.white;
                    if (GUILayout.Button(viewNode.Viewed
                            ? EditorGUIUtility.IconContent("animationvisibilitytoggleoff")
                            : EditorGUIUtility.IconContent("animationvisibilitytoggleon"), GUILayout.Width(40)))
                    {
                        viewNode.Viewed = !viewNode.Viewed;
                    }

                    GUI.color = !viewNode.Viewed ? Color.red : Color.white;
                }
                else if (node is RedDotNumberNode numberNode)
                {
                    EditorGUILayout.LabelField("通用红点");
                    int newCount = EditorGUILayout.IntField(numberNode.RedDotCount);
                    if (newCount != numberNode.RedDotCount)
                    {
                        numberNode.SetStatus(newCount);
                    }
                }
            }
            else
            {
                if (node is RedDotNumberNode numberNode)
                {
                    EditorGUILayout.LabelField($"红点数量: {numberNode.RedDotCount}", GUILayout.Width(100));
                }
                else
                {
                    GUI.color = Color.magenta;
                    EditorGUILayout.LabelField($"错误！！,该节点不是数字节点");
                }
            }

            GUI.color = Color.white;
        }

        private Color GetColor(RedDotNodeBase node)
        {
            if (node is RedDotNumberNode numberNode)
            {
                return numberNode.RedDotCount > 0 ? Color.red : Color.white;
            }
            else if (node is RedDotViewNode viewNode)
            {
                return !viewNode.Viewed ? Color.red : Color.white;
            }
            else
            {
                return Color.white;
            }
        }
    }
}
#endif