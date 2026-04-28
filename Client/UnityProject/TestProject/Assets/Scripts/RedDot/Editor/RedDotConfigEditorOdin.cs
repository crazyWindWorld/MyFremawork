#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetFramework.RedDot.RunTime;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace NetFramework.RedDot.Editor
{
    public class RedDotConfigEditorOdin : EditorWindow
    {
        [MenuItem("Tools/RedDotTree/红点数据编辑器 (Odin) #Y")]
        public static void ShowWindow()
        {
            GetWindow<RedDotConfigEditorOdin>("红点数据编辑器 (Odin)").minSize = new Vector2(1400, 800);
        }

        private RedDotConfigAsset _configAsset;
        private RedDotConfigAsset.RedDotConfigData _selectedItem;
        private ViewMode _viewMode = ViewMode.List;
        private string _search = "";
        private List<RedDotConfigAsset.RedDotConfigData> _searchList = new List<RedDotConfigAsset.RedDotConfigData>();
        private int _currentPage = 1;
        private int _pageSize = 10;
        private string _newPath = "";
        private Vector2 _scrollPos;
        private Dictionary<string, bool> _expandedStates = new Dictionary<string, bool>();

        private void OnEnable()
        {
            LoadConfigAsset();
        }

        private void LoadConfigAsset()
        {
            _configAsset = AssetDatabase.LoadAssetAtPath<RedDotConfigAsset>(
                "Assets/AssetsPackage/Main/RedDot/RedDotConfigAsset.asset");
        }

        private void OnGUI()
        {
            if (_configAsset == null)
            {
                EditorGUILayout.HelpBox("请先在 Assets/AssetsPackage/Main/RedDot 目录下创建 RedDotConfigAsset.asset 文件",
                    MessageType.Error);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawView();
            EditorGUILayout.Space(10);
            DrawSelectedItem();
            EditorGUILayout.Space(10);
            DrawOperations();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("视图模式:", GUILayout.Width(60));
            _viewMode = (ViewMode)EditorGUILayout.EnumPopup(_viewMode, GUILayout.Width(80));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            var newSearch = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (newSearch != _search)
            {
                _search = newSearch;
                UpdateSearchList();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_close"), EditorStyles.toolbarButton,
                    GUILayout.Width(30)))
            {
                _search = "";
                _searchList.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawView()
        {
            if (string.IsNullOrEmpty(_search))
            {
                switch (_viewMode)
                {
                    case ViewMode.List:
                        DrawListView();
                        break;
                    case ViewMode.Page:
                        DrawPageView();
                        break;
                    case ViewMode.Tree:
                        DrawTreeView();
                        break;
                }
            }
            else
            {
                DrawSearchView();
            }
        }

        private void DrawListView()
        {
            EditorGUILayout.LabelField("列表视图", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_configAsset.Data == null || _configAsset.Data.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无数据", MessageType.Info);
                return;
            }

            foreach (var item in _configAsset.Data.OrderBy(d => d.Id))
            {
                DrawRedDotItem(item);
            }
        }

        private void DrawPageView()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("翻页视图", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("上一页", GUILayout.Width(60)))
            {
                if (_currentPage > 1)
                    _currentPage--;
            }

            EditorGUILayout.LabelField($"第 {_currentPage} 页", GUILayout.Width(60));

            if (GUILayout.Button("下一页", GUILayout.Width(60)))
            {
                int totalPages = (int)Math.Ceiling((double)_configAsset.Data.Count / _pageSize);
                if (_currentPage < totalPages)
                    _currentPage++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            if (_configAsset.Data == null || _configAsset.Data.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无数据", MessageType.Info);
                return;
            }

            var sortedData = _configAsset.Data.OrderBy(d => d.Id).ToList();
            int startIndex = (_currentPage - 1) * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, sortedData.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                DrawRedDotItem(sortedData[i]);
            }
        }

        private void DrawTreeView()
        {
            EditorGUILayout.LabelField("树状图视图", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_configAsset.Data == null || _configAsset.Data.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无数据", MessageType.Info);
                return;
            }

            var rootNodes = BuildTree();
            foreach (var node in rootNodes)
            {
                DrawTreeNode(node, 0);
            }
        }

        private void DrawSearchView()
        {
            EditorGUILayout.LabelField($"搜索结果 ({_searchList.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_searchList.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到匹配项", MessageType.Info);
                return;
            }

            foreach (var item in _searchList)
            {
                DrawRedDotItem(item);
            }
        }

        private void DrawRedDotItem(RedDotConfigAsset.RedDotConfigData data)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUI.color = _selectedItem == data ? new Color(0.000f, 1.000f, 0.918f, 1.000f) : Color.white;

            EditorGUILayout.LabelField($"ID: {data.Id}", GUILayout.Width(80));

            var newPath = EditorGUILayout.TextField(data.Path, GUILayout.Width(300));
            if (newPath != data.Path)
            {
                data.Path = newPath;
                OnPathChanged(data);
            }

            EditorGUILayout.Space(10);

            var isView = EditorGUILayout.Toggle(data.IsView, GUILayout.Width(20));
            if (isView != data.IsView)
            {
                data.IsView = isView;
                if (!isView)
                {
                    data.ViewType = ViewType.Once;
                    data.BindRole = false;
                }
            }

            EditorGUILayout.LabelField(data.IsView
                ? EditorGUIUtility.IconContent("animationvisibilitytoggleon")
                : EditorGUIUtility.IconContent("animationvisibilitytoggleoff"), GUILayout.Width(20));

            if (data.IsView)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("查看类型:", GUILayout.Width(55));
                data.ViewType = (ViewType)EditorGUILayout.EnumPopup(data.ViewType);
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("绑定角色:", GUILayout.Width(55));
                data.BindRole = EditorGUILayout.Toggle(data.BindRole, GUILayout.Width(20));
            }
            else
            {
                EditorGUILayout.Space(20);
            }

            EditorGUILayout.LabelField("红点类型:", GUILayout.Width(55));
            data.ShowType = (RedDotShowType)EditorGUILayout.EnumPopup(data.ShowType);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("自定义枚举:", GUILayout.Width(70));
            data.Alias = EditorGUILayout.TextField(data.Alias, GUILayout.Width(150));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("本地储存:", GUILayout.Width(55));
            data.UseLocalSave = EditorGUILayout.Toggle(data.UseLocalSave, GUILayout.Width(20));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制路径", GUILayout.Width(100)))
            {
                _newPath = data.Path;
            }
            if (GUILayout.Button("编辑", GUILayout.Width(50)))
            {
                _selectedItem = data;
            }
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除红点 {data.Path} 吗？",
                    "确定", "取消"))
                {
                    _configAsset.Data.Remove(data);
                    if (_selectedItem == data)
                        _selectedItem = null;
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                }
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawTreeNode(TreeNode node, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20);
            GUI.color = _selectedItem == node.Data ? new Color(0.000f, 1.000f, 0.918f, 1.000f) : Color.white;

            if (!_expandedStates.ContainsKey(node.Data.Path))
            {
                _expandedStates[node.Data.Path] = false;
            }

            bool expanded = EditorGUILayout.Foldout(_expandedStates[node.Data.Path], node.Data.Path);
            _expandedStates[node.Data.Path] = expanded;
            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField($"ID: {node.Data.Id}", GUILayout.Width(80));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制路径", GUILayout.Width(100)))
            {
                _newPath = node.Data.Path;
            }
            if (GUILayout.Button("编辑", GUILayout.Width(50)))
            {
                _selectedItem = node.Data;
            }

            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除红点 {node.Data.Path} 吗？",
                    "确定", "取消"))
                {
                    _configAsset.Data.Remove(node.Data);
                    _expandedStates.Remove(node.Data.Path);
                    if (_selectedItem == node.Data)
                        _selectedItem = null;
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (expanded && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    DrawTreeNode(child, indent + 1);
                }
            }
        }

        private List<TreeNode> BuildTree()
        {
            var rootNodes = new List<TreeNode>();
            var allNodes = new Dictionary<string, TreeNode>();

            foreach (var item in _configAsset.Data)
            {
                allNodes[item.Path] = new TreeNode { Data = item };
            }

            foreach (var item in _configAsset.Data)
            {
                var parts = item.Path.Split('/');
                if (parts.Length == 1)
                {
                    rootNodes.Add(allNodes[item.Path]);
                }
                else
                {
                    string parentPath = string.Join("/", parts, 0, parts.Length - 1);
                    if (allNodes.ContainsKey(parentPath))
                    {
                        allNodes[parentPath].Children.Add(allNodes[item.Path]);
                    }
                }
            }

            rootNodes.Sort((a, b) => a.Data.Path.CompareTo(b.Data.Path));
            foreach (var node in allNodes.Values)
            {
                node.Children.Sort((a, b) => a.Data.Path.CompareTo(b.Data.Path));
            }

            return rootNodes;
        }

        private void DrawSelectedItem()
        {
            if (_selectedItem == null) return;
            GUI.color = new Color(0.000f, 1.000f, 0.918f, 1.000f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("编辑选中项", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField($"ID: {_selectedItem.Id}");
            EditorGUILayout.LabelField($"路径: {_selectedItem.Path}");

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("是否查看:");
            var isView = EditorGUILayout.Toggle(_selectedItem.IsView);
            if (isView != _selectedItem.IsView)
            {
                _selectedItem.IsView = isView;
                if (!isView)
                {
                    _selectedItem.ViewType = ViewType.Once;
                    _selectedItem.BindRole = false;
                }
            }

            if (_selectedItem.IsView)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("查看类型:");
                _selectedItem.ViewType = (ViewType)EditorGUILayout.EnumPopup(_selectedItem.ViewType);
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("绑定角色:");
                _selectedItem.BindRole = EditorGUILayout.Toggle(_selectedItem.BindRole);
            }            
            else
            {
                if (_selectedItem.UseLocalSave)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("绑定角色:", GUILayout.Width(55));
                    _selectedItem.BindRole = EditorGUILayout.Toggle(_selectedItem.BindRole, GUILayout.Width(20));
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("红点类型:");
            _selectedItem.ShowType = (RedDotShowType)EditorGUILayout.EnumPopup(_selectedItem.ShowType);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("自定义枚举:");
            _selectedItem.Alias = EditorGUILayout.TextField(_selectedItem.Alias);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("本地储存:");
            _selectedItem.UseLocalSave = EditorGUILayout.Toggle(_selectedItem.UseLocalSave);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("取消选择", GUILayout.Width(100)))
            {
                _selectedItem = null;
            }

            EditorGUILayout.EndVertical();
             GUI.color = Color.white;
        }

        private void DrawOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("新红点路径:");
            _newPath = EditorGUILayout.TextField(_newPath);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("添加红点", GUILayout.Width(100)))
            {
                AddRedDot();
                SortData();
            }
            if (GUILayout.Button("保存", GUILayout.Width(100)))
            {
                GenerateCode();
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("成功", "保存成功！", "确定");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void UpdateSearchList()
        {
            _searchList.Clear();
            if (_configAsset?.Data != null)
            {
                foreach (var item in _configAsset.Data)
                {
                    if (item.Path.Contains(_search))
                    {
                        _searchList.Add(item);
                    }
                }
            }
        }

        private void OnPathChanged(RedDotConfigAsset.RedDotConfigData data)
        {
            if (string.IsNullOrEmpty(data.Path)) return;

            var splitData = data.Path.Split('/');
            string currentPath = "";

            for (int i = 0; i < splitData.Length; i++)
            {
                currentPath += splitData[i];
                var item = _configAsset.Data.Find(_ => _.Path == currentPath);
                if (item == null)
                {
                    item = new RedDotConfigAsset.RedDotConfigData
                    {
                        Id = _configAsset.Data.Count == 0 ? 1 : _configAsset.Data.Max(_ => _.Id) + 1,
                        Path = currentPath,
                        IsView = false
                    };
                    _configAsset.Data.Add(item);
                }
                if (i < splitData.Length - 1)
                {
                    currentPath += "/";
                }
            }

            SortData();
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
        }

        private void AddRedDot()
        {
            if (string.IsNullOrEmpty(_newPath))
            {
                EditorUtility.DisplayDialog("提示", "请输入红点路径", "确定");
                return;
            }

            var splitData = _newPath.Split('/');
            string path = "";

            for (int i = 0; i < splitData.Length; i++)
            {
                path += splitData[i];
                var item = _configAsset.Data.Find(_ => _.Path == path);
                if (item == null)
                {
                    item = new RedDotConfigAsset.RedDotConfigData
                    {
                        Id = _configAsset.Data.Count == 0 ? 1 : _configAsset.Data.Max(_ => _.Id) + 1,
                        Path = path,
                        IsView = false
                    };
                    _configAsset.Data.Add(item);
                }
                if (i < splitData.Length - 1)
                {
                    path += "/";
                }
            }

            SortData();
            _newPath = "";
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
        }

        private void GenerateCode()
        {
            AutoGenEnum();
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("成功", "代码生成成功！", "确定");
        }

        private void SortData()
        {
            if (_configAsset?.Data == null) return;

            _configAsset.Data.Sort((a, b) =>
            {
                string[] aParts = a.Path.Split('/');
                string[] bParts = b.Path.Split('/');

                for (int i = 0; i < Mathf.Min(aParts.Length, bParts.Length); i++)
                {
                    int compare = string.Compare(aParts[i], bParts[i], StringComparison.Ordinal);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }

                return aParts.Length.CompareTo(bParts.Length);
            });
        }

        private void AutoGenEnum()
        {
            if (_configAsset == null) return;

            SortData();

            string enumPath = Application.dataPath + "\\HotUpdate\\RedDotNew";
            if (!Directory.Exists(Application.dataPath + "\\HotUpdate\\RedDotNew"))
            {
                Directory.CreateDirectory(enumPath);
            }
            string path = Application.dataPath + "\\HotUpdate\\RedDotNew\\RedDotEnum.cs";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("namespace HotUpdate.RedDotView");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("  public enum RedDotEnum");
            stringBuilder.AppendLine("  {");
            stringBuilder.AppendLine("      Default = -1,");

            for (int i = 0; i < _configAsset.Data.Count; i++)
            {
                if (_configAsset.Data[i].Path.Contains("{") && _configAsset.Data[i].UseLocalSave)
                {
                    EditorUtility.DisplayDialog("警告",
                        $"红点{_configAsset.Data[i].Id}中红点路径{_configAsset.Data[i].Path}包含{{}}（参数化的数据）不能自动本地储存，请手动完成",
                        "确定");
                    _configAsset.Data[i].UseLocalSave = false;
                }

                string alias = _configAsset.Data[i].Alias;
                if (string.IsNullOrEmpty(alias))
                {
                    alias = _configAsset.Data[i].Path.Replace("/", "_");
                    string pattern = "\\{.*?\\}";
                    alias = Regex.Replace(alias, pattern, "");
                    alias = Regex.Replace(alias, "__", "_");
                }

                if (alias.EndsWith("_"))
                {
                    alias = alias.Remove(alias.Length - 1, 1);
                }

                alias += " = " + _configAsset.Data[i].Id;
                stringBuilder.AppendLine($"      {alias},");
            }

            stringBuilder.AppendLine("  }");
            stringBuilder.AppendLine("}");
            File.WriteAllText(path, stringBuilder.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private class TreeNode
        {
            public RedDotConfigAsset.RedDotConfigData Data;
            public List<TreeNode> Children = new List<TreeNode>();
        }

        private enum ViewMode
        {
            List,
            Page,
            Tree
        }
    }
}
#endif