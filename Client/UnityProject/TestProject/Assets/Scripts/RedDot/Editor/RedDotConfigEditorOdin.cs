#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fuel.RedDot.RunTime;
using UnityEditor;
using UnityEngine;

namespace Fuel.RedDot.Editor
{
    public class RedDotConfigEditorOdin : EditorWindow
    {
        [MenuItem("Tools/红点数据编辑器 (Odin) #Y")]
        public static void ShowWindow()
        {
            GetWindow<RedDotConfigEditorOdin>("红点数据编辑器 (Odin)").minSize = new Vector2(1250, 800);
        }

        private const float DetailPanelWidth = 340f;
        private const float IdColumnWidth = 45f;
        private const float PathColumnWidth = 360f;
        private const float TypeColumnWidth = 55f;
        private const float PeriodColumnWidth = 65f;
        private const float ShowColumnWidth = 70f;
        private const float AliasColumnWidth = 120f;
        private const float SaveColumnWidth = 45f;
        private const float OperationColumnWidth = 90f;
        private const float TableContentWidth = IdColumnWidth + PathColumnWidth + TypeColumnWidth + PeriodColumnWidth + ShowColumnWidth + AliasColumnWidth + SaveColumnWidth;
        private const float TableWidth = TableContentWidth + OperationColumnWidth + 25f;

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

            DrawHeader();
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(TableWidth));
            DrawView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(DetailPanelWidth));
            DrawSelectedItem();
            EditorGUILayout.Space(6);
            DrawOperations();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("红点配置", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("视图:", GUILayout.Width(35));
            _viewMode = (ViewMode)EditorGUILayout.EnumPopup(_viewMode, GUILayout.Width(80));

            if (_viewMode == ViewMode.Tree)
            {
                if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    SetAllTreeFoldState(true);
                }
                if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    SetAllTreeFoldState(false);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(35));
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

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存并生成枚举", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                GenerateCode();
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("成功", "保存成功！", "确定");
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

            DrawTableHeader();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(900));
            foreach (var item in _configAsset.Data.OrderBy(d => d.Id).ToList())
            {
                DrawRedDotItem(item);
            }
            EditorGUILayout.EndScrollView();
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

            DrawTableHeader();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = startIndex; i < endIndex; i++)
            {
                DrawRedDotItem(sortedData[i]);
            }
            EditorGUILayout.EndScrollView();
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

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            var rootNodes = BuildTree();
            foreach (var node in rootNodes)
            {
                DrawTreeNode(node, 0);
            }
            EditorGUILayout.EndScrollView();
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

            DrawTableHeader();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var item in _searchList.ToList())
            {
                DrawRedDotItem(item);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(TableWidth));
            EditorGUILayout.LabelField("ID", GUILayout.Width(IdColumnWidth));
            EditorGUILayout.LabelField("Path", GUILayout.Width(PathColumnWidth));
            EditorGUILayout.LabelField("类型", GUILayout.Width(TypeColumnWidth));
            EditorGUILayout.LabelField("周期", GUILayout.Width(PeriodColumnWidth));
            EditorGUILayout.LabelField("显示", GUILayout.Width(ShowColumnWidth));
            EditorGUILayout.LabelField("枚举名", GUILayout.Width(AliasColumnWidth));
            EditorGUILayout.LabelField("存储", GUILayout.Width(SaveColumnWidth));
            EditorGUILayout.LabelField("操作", GUILayout.Width(OperationColumnWidth));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRedDotItem(RedDotConfigAsset.RedDotConfigData data)
        {
            GUI.color = _selectedItem == data ? new Color(0.000f, 1.000f, 0.918f, 1.000f) : Color.white;
            float contentWidth = TableContentWidth;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(TableWidth));

            Rect selectableRect = EditorGUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            EditorGUILayout.LabelField(data.Id.ToString(), GUILayout.Width(IdColumnWidth));
            EditorGUILayout.LabelField(data.Path, GUILayout.Width(PathColumnWidth));
            EditorGUILayout.LabelField(data.IsView ? "查看" : "数量", GUILayout.Width(TypeColumnWidth));
            EditorGUILayout.LabelField(data.IsView ? data.ViewType.ToString() : "-", GUILayout.Width(PeriodColumnWidth));
            EditorGUILayout.LabelField(data.ShowType.ToString(), GUILayout.Width(ShowColumnWidth));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(data.Alias) ? "-" : data.Alias, GUILayout.Width(AliasColumnWidth));
            EditorGUILayout.LabelField(data.UseLocalSave ? "是" : "否", GUILayout.Width(SaveColumnWidth));
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && selectableRect.Contains(Event.current.mousePosition))
            {
                _selectedItem = _selectedItem == data ? null : data;
                Event.current.Use();
                Repaint();
            }

            if (GUILayout.Button("复制", GUILayout.Width(45)))
            {
                _newPath = data.Path;
            }
            if (GUILayout.Button("删除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除红点 {data.Path} 吗？",
                    "确定", "取消"))
                {
                    _configAsset.Data.Remove(data);
                    if (_selectedItem == data)
                        _selectedItem = null;
                    EditorUtility.SetDirty(_configAsset);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;
        }

        private void DrawTreeNode(TreeNode node, int indent)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Space(indent * 20);
            GUI.color = _selectedItem == node.Data ? new Color(0.000f, 1.000f, 0.918f, 1.000f) : Color.white;

            bool hasChildren = node.Children.Count > 0;
            bool expanded = false;
            if (hasChildren)
            {
                if (!_expandedStates.ContainsKey(node.Data.Path))
                {
                    _expandedStates[node.Data.Path] = false;
                }

                EditorGUILayout.BeginHorizontal(GUILayout.Width(PathColumnWidth));
                expanded = EditorGUILayout.Foldout(_expandedStates[node.Data.Path], node.Data.Path, true);
                EditorGUILayout.EndHorizontal();
                _expandedStates[node.Data.Path] = expanded;
            }
            else
            {
                EditorGUILayout.LabelField(node.Data.Path, GUILayout.Width(PathColumnWidth));
            }
            EditorGUILayout.LabelField($"ID: {node.Data.Id}", GUILayout.Width(IdColumnWidth));
            EditorGUILayout.LabelField(node.Data.IsView ? "查看" : "数量", GUILayout.Width(TypeColumnWidth));
            EditorGUILayout.LabelField(node.Data.IsView ? node.Data.ViewType.ToString() : "-", GUILayout.Width(PeriodColumnWidth));
            EditorGUILayout.LabelField(node.Data.ShowType.ToString(), GUILayout.Width(ShowColumnWidth));
            EditorGUILayout.LabelField(node.Data.UseLocalSave ? "是" : "否", GUILayout.Width(SaveColumnWidth));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制", GUILayout.Width(45)))
            {
                _newPath = node.Data.Path;
            }
            if (GUILayout.Button("编辑", GUILayout.Width(45)))
            {
                _selectedItem = _selectedItem == node.Data ? null : node.Data;
            }

            if (GUILayout.Button("删除", GUILayout.Width(45)))
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
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (hasChildren && expanded)
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("选中项详情", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            if (_selectedItem == null || !_configAsset.Data.Contains(_selectedItem))
            {
                _selectedItem = null;
                EditorGUILayout.HelpBox("从左侧列表或树中选择一个红点进行编辑。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("ID", _selectedItem.Id.ToString());

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("路径", GUILayout.Width(70));
            var newPath = EditorGUILayout.TextField(_selectedItem.Path);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            var isView = EditorGUILayout.Toggle("查看红点", _selectedItem.IsView);
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
                _selectedItem.ViewType = (ViewType)EditorGUILayout.EnumPopup("查看周期", _selectedItem.ViewType);
                _selectedItem.BindRole = EditorGUILayout.Toggle("绑定角色", _selectedItem.BindRole);
            }
            else if (_selectedItem.UseLocalSave)
            {
                _selectedItem.BindRole = EditorGUILayout.Toggle("绑定角色", _selectedItem.BindRole);
            }

            _selectedItem.ShowType = (RedDotShowType)EditorGUILayout.EnumPopup("显示样式", _selectedItem.ShowType);
            _selectedItem.Alias = EditorGUILayout.TextField("枚举名", _selectedItem.Alias);
            _selectedItem.UseLocalSave = EditorGUILayout.Toggle("本地储存", _selectedItem.UseLocalSave);

            if (EditorGUI.EndChangeCheck())
            {
                if (newPath != _selectedItem.Path)
                {
                    _selectedItem.Path = newPath;
                    OnPathChanged(_selectedItem);
                }
                else
                {
                    EditorUtility.SetDirty(_configAsset);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("复制路径"))
            {
                _newPath = _selectedItem.Path;
            }
            if (GUILayout.Button("取消选择"))
            {
                _selectedItem = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("新增红点", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("新增路径（全路径自动拆分）");
            _newPath = EditorGUILayout.TextField(_newPath);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("添加红点"))
            {
                AddRedDot();
                SortData();
            }
            if (GUILayout.Button("保存"))
            {
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
            if (_configAsset?.Data == null || string.IsNullOrEmpty(_search))
            {
                return;
            }

            foreach (var item in _configAsset.Data)
            {
                if (ContainsIgnoreCase(item.Path, _search) ||
                    ContainsIgnoreCase(item.Id.ToString(), _search) ||
                    ContainsIgnoreCase(item.Alias, _search))
                {
                    _searchList.Add(item);
                }
            }
        }

        private bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetAllTreeFoldState(bool expanded)
        {
            if (_configAsset?.Data == null)
            {
                return;
            }

            foreach (var item in _configAsset.Data)
            {
                _expandedStates[item.Path] = expanded;
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

            RedDotConfigAsset.RedDotConfigData selectedItem = null;
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
                selectedItem = item;
                if (i < splitData.Length - 1)
                {
                    path += "/";
                }
            }

            SortData();
            _selectedItem = selectedItem;
            _newPath = "";
            EditorUtility.SetDirty(_configAsset);
        }

        private void GenerateCode()
        {
            AutoGenEnum();
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("枚举生成成功！"));
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