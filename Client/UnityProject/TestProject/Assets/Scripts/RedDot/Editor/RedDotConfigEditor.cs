#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetFramework.RedDot.RunTime;
using UnityEditor;
using UnityEngine;

namespace NetFramework.RedDot.Editor
{
    public class RedDotConfigEditor : EditorWindow
    {
        [MenuItem("Tools/RedDotTree/红点数据编辑器 #T")]
        public static void ShowWindow()
        {
            GetWindow<RedDotConfigEditor>("红点数据编辑器").minSize = new Vector2(1200, 600);
        }

        private GUIStyle nodeStyle;
        private string m_selectedItem = "列表";
        private string m_search = "";
        private string m_lastSearch = "";

        private Vector2 m_scrollPos;
        private RedDotConfigAsset _configAsset;
        private readonly int m_pageCount = 10;
        private int m_page = 1;
        private string m_newPath = "";
        private List<RedDotConfigAsset.RedDotConfigData> m_searchList = new List<RedDotConfigAsset.RedDotConfigData>();

        private void OnGUI()
        {
            nodeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(5, 5, 2, 2),
                padding = new RectOffset(10, 10, 5, 5)
            };
            if (_configAsset == null)
            {
                _configAsset = AssetDatabase.LoadAssetAtPath<RedDotConfigAsset>(
                    "Assets/AssetsPackage/Main/RedDot/RedDotConfigAsset.asset");
            }

            if (_configAsset == null)
            {
                EditorGUILayout.HelpBox("请先在Assets/AssetsPackage/Main/RedDot目录下创建RedDotConfigAsset.asset文件",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(new GUIContent(m_selectedItem), FocusType.Keyboard,
                    GUILayout.Width(60)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("列表"), m_selectedItem == "列表", OnOptionSelected, "列表");
                menu.AddItem(new GUIContent("翻页"), m_selectedItem == "翻页", OnOptionSelected, "翻页");
                menu.AddItem(new GUIContent("树状图"), m_selectedItem == "树状图", OnOptionSelected, "树状图");
                menu.ShowAsContext();
            }


            m_search = EditorGUILayout.TextField(m_search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_close"), GUILayout.Height(18),
                    GUILayout.Width(40)))
            {
                m_search = "";
                m_searchList.Clear();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("SaveActive"), GUILayout.Height(18),
                    GUILayout.Width(40)))
            {
                AutoGenEnum();
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
            }

            if (m_selectedItem == "翻页")
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"), GUILayout.Height(18),
                        GUILayout.Width(20)))
                {
                    if (m_page > 1)
                    {
                        m_page--;
                    }
                }

                EditorGUILayout.LabelField("第" + m_page + "页", GUILayout.Width(40));
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUILayout.Height(18),
                        GUILayout.Width(20)))
                {
                    if (m_page < _configAsset.Data.Count / m_pageCount + 1)
                    {
                        m_page++;
                    }
                }
            }

            if (GUI.changed)
            {
                if (m_lastSearch != m_search)
                {
                    m_lastSearch = m_search;
                    if (!string.IsNullOrEmpty(m_search))
                    {
                        SearchNode(m_search);
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            if (string.IsNullOrEmpty(m_search))
            {
                if (m_selectedItem == "列表")
                {
                    DrawScrollView();
                }
                else if (m_selectedItem == "翻页")
                {
                    DrawPage();
                }
                else
                {
                    DrawTreeView();
                }
            }
            else
            {
                DrawSearch();
            }
        }


        private void DrawScrollView()
        {
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            if (_configAsset != null)
            {
                for (int i = 0; i < _configAsset.Data.Count; i++)
                {
                    DrawRedDotConfigItemData(_configAsset.Data[i]);
                }
            }

            EditorGUILayout.EndScrollView();

            m_newPath = EditorGUILayout.TextField("新增红点路径", m_newPath);
            if (GUILayout.Button("添加"))
            {
                OnClickAdd();
            }
        }

        private void DrawTreeView()
        {
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            if (_configAsset != null)
            {
                for (int i = 0; i < _configAsset.Data.Count; i++)
                {
                    DrawRedDotConfigItemData(_configAsset.Data[i]);
                }
            }

            EditorGUILayout.EndScrollView();

            m_newPath = EditorGUILayout.TextField("新增红点路径", m_newPath);
            if (GUILayout.Button("添加"))
            {
                OnClickAdd();
            }
        }

        private void OnClickAdd()
        {
            if (string.IsNullOrEmpty(m_newPath))
            {
                return;
            }
            var splitData = m_newPath.Split('/');
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
            //排序
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

        private void DrawPage()
        {
            int end = m_page * m_pageCount >= _configAsset.Data.Count ? _configAsset.Data.Count : m_page * m_pageCount;
            for (int i = (m_page - 1) * m_pageCount; i < end; i++)
            {
                int endCount = m_page * m_pageCount >= _configAsset.Data.Count ? _configAsset.Data.Count : m_page * m_pageCount;
                if (i < endCount)
                {
                    DrawRedDotConfigItemData(_configAsset.Data[i]);
                }
            }

            if (m_page == _configAsset.Data.Count / m_pageCount + (_configAsset.Data.Count % m_pageCount == 0 ? 0 : 1))
            {

            }
            m_newPath = EditorGUILayout.TextField("新增红点路径", m_newPath);
            if (GUILayout.Button("添加"))
            {
                OnClickAdd();
                m_page = _configAsset.Data.Count / m_pageCount + (_configAsset.Data.Count % m_pageCount == 0 ? 0 : 1);
            }
        }

        private void DrawSearch()
        {
            for (int i = 0; i < m_searchList.Count; i++)
            {
                DrawRedDotConfigItemData(m_searchList[i]);
            }
        }

        private void DrawRedDotConfigItemData(RedDotConfigAsset.RedDotConfigData data)
        {
            EditorGUILayout.BeginHorizontal(nodeStyle);
            data.Id = EditorGUILayout.IntField(data.Id, GUILayout.Width(40));
            EditorGUILayout.Space(5);
            data.Path = EditorGUILayout.TextField(data.Path, GUILayout.Width(300));
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(data.IsView
                ? EditorGUIUtility.IconContent("animationvisibilitytoggleon")
                : EditorGUIUtility.IconContent("animationvisibilitytoggleoff"), GUILayout.Width(20));
            data.IsView = EditorGUILayout.Toggle(data.IsView, GUILayout.Width(20));
            EditorGUILayout.Space(20);
            if (data.IsView)
            {
                EditorGUILayout.LabelField("查看类型:", GUILayout.Width(55));
                data.ViewType = (ViewType)EditorGUILayout.EnumPopup(data.ViewType);
                EditorGUILayout.Space(20);
            }
            else
            {
                EditorGUILayout.Space(20);
            }

            EditorGUILayout.LabelField("红点类型:", GUILayout.Width(55));
            data.ShowType = (RedDotShowType)EditorGUILayout.EnumPopup(data.ShowType);
            //EditorGUILayout.Toggle(data.IsChild);
            EditorGUILayout.Space(20);
            if (data.IsView||data.UseLocalSave)
            {
                EditorGUILayout.LabelField("绑定角色:", GUILayout.Width(55));
                data.BindRole = EditorGUILayout.Toggle(data.BindRole, GUILayout.Width(20));
                EditorGUILayout.Space(20);
            }

            EditorGUILayout.LabelField("自定义枚举:", GUILayout.Width(70));
            data.Alias = EditorGUILayout.TextField(data.Alias, GUILayout.Width(200));
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("本地储存:", GUILayout.Width(55));
            data.UseLocalSave = EditorGUILayout.Toggle(data.UseLocalSave, GUILayout.Width(20));

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                _configAsset.Data.Remove(data);
                if (m_selectedItem == "翻页")
                {
                    m_page = _configAsset.Data.Count / m_pageCount + (_configAsset.Data.Count % m_pageCount == 0 ? 0 : 1);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnOptionSelected(object option)
        {
            m_selectedItem = option.ToString();
            if (m_selectedItem == "翻页")
            {
                m_page = 1;
            }
        }

        private void SearchNode(string search)
        {
            m_searchList.Clear();
            if (_configAsset == null)
            {
                return;
            }

            for (int i = 0; i < _configAsset.Data.Count; i++)
            {
                if (_configAsset.Data[i].Path.Contains(search))
                {
                    m_searchList.Add(_configAsset.Data[i]);
                }
            }
        }

        private void AutoGenEnum()
        {
            if (_configAsset == null)
            {
                return;
            }
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
                    EditorUtility.DisplayDialog("警告", $"红点{_configAsset.Data[i].Id}中红点路径{_configAsset.Data[i].Path}包含{{}}（参数化的数据）不能自动本地储存，请手动完成", "确定");
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
    }
}
#endif