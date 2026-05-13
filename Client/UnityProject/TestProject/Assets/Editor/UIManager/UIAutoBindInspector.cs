#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game.Config;

namespace Manager.UIManager.Editor
{
    [CustomEditor(typeof(GameObject), true)]
    public class UIAutoBindInspector : UnityEditor.Editor
    {
        protected GameObject CurrentTargetGo;
        protected string UIGUID;

        #region 绘制Unity原来的Inspector
        protected Type EditorType;
        protected UnityEditor.Editor Instance;
        protected static readonly object[] EmptyArray = Array.Empty<object>();
        protected static MethodInfo onHeaderGUI;
        #endregion

        protected UIAutoBindData RootUIAutoBindData;
        protected UIAutoBindData CurUIAutoBindData;
        protected UIAutoBindData ParentUIAutoBindData;

        protected bool IsShowGenetateMono;
        protected bool IsHeader;

        protected List<UGUINodeProviderMenuItemInfo> UGUINodeProviderMenuItemInfos = new();

        private bool _isCurDrawMenu;
        private bool _isReadScriptableMenu;
        private bool _isDrawHeaderDetail = true;
        private bool _initialize;
        private bool _isNeedBinding;

        private UIAutoBindDataScriptable _uIAutoBindDataScriptable;

        private string _variableName;
        private string _className;
        private UILayer _selectedLayer = UILayer.Normal;

        private PrefabStage _prefabStage;

        private void OnEnable()
        {
            if (_initialize) return;
            _initialize = true;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            EditorType ??= Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetTypes()
                .FirstOrDefault(m => m.Name == "GameObjectInspector");

            if (Instance == null)
                Instance = CreateEditor(targets, EditorType);

            if (onHeaderGUI == null)
                onHeaderGUI = EditorType?.GetMethod("OnHeaderGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

            _prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (_prefabStage == null)
            {
                IsShowGenetateMono = false;
                return;
            }

            CurrentTargetGo = target as GameObject;

            UpdateHeader();
            UpdateMenuInfos();
            UpdateIsNeedBinding();

            _isCurDrawMenu = EditorPrefs.GetBool("isCurDrawMenu", false);
            _isReadScriptableMenu = EditorPrefs.GetBool("isReadMenu", false);
            _isDrawHeaderDetail = EditorPrefs.GetBool("isDrawHeaderDetail", true);

            if (CurUIAutoBindData != null)
            {
                _uIAutoBindDataScriptable = AssetDatabase.LoadAssetAtPath<UIAutoBindDataScriptable>($"{UIAutoBindDataUtil.DatabaseRoot}/{CurUIAutoBindData.ClassName}.asset");
            }
        }

        private void OnHierarchyChanged()
        {
            RefreshHierarchy();
        }

        private void RefreshHierarchy()
        {
            if (RootUIAutoBindData == null) return;
            var coms = RootUIAutoBindData.GetComponentsInChildren<UIAutoBindData>(true);

            foreach (var com in coms)
            {
                var parent = EditorTools.FindComponentInParent<UIAutoBindData>(com.transform, false);
                if (parent == null) continue;
                if (string.IsNullOrEmpty(com.VariableName))
                    com.VariableName = EditorTools.GetVariableName(com.name);
                if (string.IsNullOrEmpty(com.ClassName))
                    com.ClassName = EditorTools.GetVariableName($"{parent.ClassName}_{com.name}");
            }
        }

        private void UpdateIsNeedBinding()
        {
            if (RootUIAutoBindData != null)
            {
                _isNeedBinding = UIAutoBindCopyEditor.IsBindingAllUGUINodeProvider(RootUIAutoBindData);
            }
        }

        private void UpdateMenuInfos()
        {
            UGUINodeProviderMenuItemInfos.Clear();
            UGUINodeProviderMenuItemInfos = GetUGUINodeProviderMenuItemInfos(CurrentTargetGo);
        }

        private List<UGUINodeProviderMenuItemInfo> GetUGUINodeProviderMenuItemInfos(GameObject go)
        {
            List<UGUINodeProviderMenuItemInfo> menuItemInfos = new List<UGUINodeProviderMenuItemInfo>();

            if (go == null) return menuItemInfos;

            Component[] components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null || comp is UnityEngine.Transform) continue;

                UGUINodeProviderInfo info = new UGUINodeProviderInfo
                {
                    name = UIAutoBindData.GetVariableName(comp.gameObject.name),
                    typeName = comp.GetType().Name,
                    parentClassName = "",
                    obj = comp
                };

                menuItemInfos.Add(new UGUINodeProviderMenuItemInfo(info, comp.GetType().Name));
            }

            foreach (Transform child in go.transform)
            {
                Component[] childComponents = child.GetComponents<Component>();
                foreach (Component comp in childComponents)
                {
                    if (comp == null || comp is UnityEngine.Transform) continue;

                    UGUINodeProviderInfo info = new UGUINodeProviderInfo
                    {
                        name = UIAutoBindData.GetVariableName(comp.gameObject.name),
                        typeName = comp.GetType().Name,
                        parentClassName = "",
                        obj = comp
                    };

                    menuItemInfos.Add(new UGUINodeProviderMenuItemInfo(info, comp.GetType().Name));
                }
            }

            return menuItemInfos;
        }

        private void UpdateHeader()
        {
            if (CurrentTargetGo == null || _prefabStage == null || _prefabStage.prefabContentsRoot == null)
            {
                IsShowGenetateMono = false;
                return;
            }

            IsShowGenetateMono = _prefabStage != null && UIBindFolderConfig.Instance.UIBindFolders.Any(v => _prefabStage.assetPath.Contains(v));
            IsHeader = _prefabStage.prefabContentsRoot == Selection.activeGameObject;
            RootUIAutoBindData = _prefabStage.prefabContentsRoot.GetComponent<UIAutoBindData>();

            CurUIAutoBindData = CurrentTargetGo.GetComponent<UIAutoBindData>();
            if (CurUIAutoBindData != null)
            {
                if (string.IsNullOrEmpty(CurUIAutoBindData.ClassName))
                {
                    CurUIAutoBindData.ClassName = CurrentTargetGo.name;
                }
            }

            ParentUIAutoBindData = EditorTools.FindComponentInParent<UIAutoBindData>(CurrentTargetGo.transform);
            if (ParentUIAutoBindData == null) return;
            if (string.IsNullOrEmpty(ParentUIAutoBindData.ClassName))
            {
                ParentUIAutoBindData.ClassName = ParentUIAutoBindData.gameObject.name;
            }
        }

        public static void SetDirty(GameObject gameObject)
        {
            EditorUtility.SetDirty(gameObject);
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }

        protected override void OnHeaderGUI()
        {
            if (onHeaderGUI != null)
            {
                onHeaderGUI?.Invoke(Instance, EmptyArray);
            }
        }

        private void OnDisable()
        {
            if (!_initialize) return;
            _initialize = false;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            if (Instance != null)
            {
                DestroyImmediate(Instance);
                Instance = null;
            }
            _prefabStage = null;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var _prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (_prefabStage == null)
            {
                return;
            }

            var _rootUIAutoBindData = _prefabStage.prefabContentsRoot.GetComponent<UIAutoBindData>();
            if (_rootUIAutoBindData != null)
            {
                UIAutoBindCopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, _rootUIAutoBindData, _rootUIAutoBindData);
            }

            var selectGo = Selection.gameObjects;
            if (selectGo.Length != 1 || selectGo[0] == null) return;
            var com = selectGo[0].GetComponent<UIAutoBindData>();
            if (com == null)
            {
                return;
            }
            var parent = EditorTools.FindComponentInParent<UIAutoBindData>(selectGo[0].transform, false);
            UIAutoBindCopyEditor.BindingAllUGUINodeProvider(selectGo[0], parent == null ? com : parent, com);
        }

        public override void OnInspectorGUI()
        {
            if (_prefabStage == null)
            {
                return;
            }

            if (!IsShowGenetateMono)
            {
                GUILayout.Space(-5f);
                return;
            }

            DrawOtherConnect();

            EditorGUI.BeginChangeCheck();
            _isReadScriptableMenu = EditorGUILayout.Foldout(_isReadScriptableMenu, "持久化");
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("isReadMenu", _isReadScriptableMenu);
            }
            if (_isReadScriptableMenu)
            {
                DrawScriptableReadItem();
                DrawScriptableItem();
            }

            DrawInspectorConnect();

            EditorGUI.BeginChangeCheck();
            _isCurDrawMenu = EditorGUILayout.Foldout(_isCurDrawMenu, "菜单");
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("isCurDrawMenu", _isCurDrawMenu);
            }
            if (_isCurDrawMenu)
            {
                DrawMenuItem();
            }

            GUILayout.Space(5f);
        }

        private void DrawInspectorConnect()
        {
            if (CurUIAutoBindData == null)
            {
                EmptyGenerateDraw();
                return;
            }

            EditorGUI.BeginChangeCheck();
            _isDrawHeaderDetail = EditorGUILayout.Foldout(_isDrawHeaderDetail, "HeaderDetail");
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("isDrawHeaderDetail", _isDrawHeaderDetail);
            }
            if (_isDrawHeaderDetail)
            {
                DrawHeaderDetail();
            }

            HeaderGenerateRmoveDraw();

            if (CurUIAutoBindData != null && CurUIAutoBindData.EGenerateType != EGenerateType.None)
            {
                DrawHeaderGenerateMenu();
            }
        }

        private void DrawOtherConnect()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前文件夹:");
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                UpdateHeader();
            }
            EditorGUILayout.EndHorizontal();

            DrawSeparator();
            GUILayout.Space(5);
        }

        private void DrawGenerateMenu()
        {
            if (GUILayout.Button("一键生成"))
            {
                if (RootUIAutoBindData == null)
                {
                    Debug.LogError("_rootUIAutoBindData is null");
                    return;
                }

                UIAutoBindDataUtil.SerializeToUIAutoBindDataScriptable(RootUIAutoBindData, _prefabStage.assetPath);

                var allDatas = EditorTools.GetAllComponentsInPrefab<UIAutoBindData>(_prefabStage.prefabContentsRoot);
                foreach (var com in allDatas)
                {
                    UIAutoBindCopyEditor.CopyUIByUIAutoBindData(com.gameObject, com, RootUIAutoBindData);
                }

                UIAutoBindCopyEditor.GenerateAllMonoByData(RootUIAutoBindData);
            }

            EditorGUI.BeginDisabledGroup(!_isNeedBinding);
            if (GUILayout.Button("一键绑定"))
            {
                if (_prefabStage.prefabContentsRoot == null)
                {
                    Debug.LogError("_prefabStage.prefabContentsRoot is null");
                    return;
                }

                UIAutoBindCopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, RootUIAutoBindData, RootUIAutoBindData);
                UpdateIsNeedBinding();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawHeaderGenerateMenu()
        {
            if (IsHeader)
            {
                DrawGenerateMenu();
            }
            else
            {
                if (GUILayout.Button("一键生成"))
                {
                    if (CurrentTargetGo == null)
                    {
                        Debug.LogError("CurrentTargetGo is null");
                        return;
                    }

                    UIAutoBindDataUtil.SerializeToUIAutoBindDataScriptable(CurUIAutoBindData, _prefabStage.assetPath);
                    UIAutoBindCopyEditor.CopyUIByUIAutoBindData(CurrentTargetGo, CurUIAutoBindData, RootUIAutoBindData);
                    UIAutoBindCopyEditor.GenerateAllMonoByData(CurUIAutoBindData);
                }

                EditorGUI.BeginDisabledGroup(!_isNeedBinding);
                if (GUILayout.Button("一键绑定"))
                {
                    if (_prefabStage.prefabContentsRoot == null)
                    {
                        Debug.LogError("_prefabStage.prefabContentsRoot is null");
                        return;
                    }

                    var parent = EditorTools.FindComponentInParent<UIAutoBindData>(CurrentTargetGo.transform, false);
                    UIAutoBindCopyEditor.BindingAllUGUINodeProvider(CurrentTargetGo, parent == null ? CurUIAutoBindData : parent, CurUIAutoBindData);
                    UpdateIsNeedBinding();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space();
        }

        private void HeaderGenerateRmoveDraw()
        {
            if (!IsHeader)
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button("remove generate item mono script"))
                {
                    CurUIAutoBindData = CurrentTargetGo.GetComponent<UIAutoBindData>();
                    if (CurUIAutoBindData == null)
                    {
                        Debug.LogError("_curUIAutoBindData is null. remove fail");
                        return;
                    }
                    var parent = EditorTools.FindComponentInParent<UIAutoBindData>(CurrentTargetGo.transform, IsHeader);
                    RemoveSubUIAutoBindData(parent, CurUIAutoBindData);

                    GameObject.DestroyImmediate(CurUIAutoBindData);
                    CurUIAutoBindData = null;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    OnEnable();
                    return;
                }
                GUI.color = GUI.backgroundColor;
            }
            else
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button("remove generate view mono script"))
                {
                    if (EditorUtility.DisplayDialog("警告", "你的操作将移除这个界面所有绑定，不可恢复！！！", "确定", "取消"))
                    {
                        var coms = EditorTools.GetAllComponentsInPrefab<UIAutoBindData>(_prefabStage.prefabContentsRoot);
                        foreach (var com in coms)
                        {
                            GameObject.DestroyImmediate(com);
                        }
                        RootUIAutoBindData = null;
                        ParentUIAutoBindData = null;
                        CurUIAutoBindData = null;
                        SetDirty(_prefabStage.prefabContentsRoot);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        OnEnable();
                        return;
                    }
                }
                GUI.color = GUI.backgroundColor;
            }
        }

        private void DrawHeaderDetail()
        {
            EditorGUILayout.LabelField($"生成类型: {CurUIAutoBindData.EGenerateType}", EditorStyles.boldLabel);

            if (CurUIAutoBindData.EGenerateType == EGenerateType.None) return;

            if (CurUIAutoBindData.EGenerateType == EGenerateType.SubItem)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Variable Name");
                _variableName = EditorGUILayout.TextField(CurUIAutoBindData.VariableName, EditorStyles.textField);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(CurUIAutoBindData, "_curUIAutoBindData.VariableName");
                    CurUIAutoBindData.VariableName = _variableName;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Class Name");
            _className = EditorGUILayout.TextField(CurUIAutoBindData.ClassName, EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(CurUIAutoBindData, "_curUIAutoBindData.ClassName");
                CurUIAutoBindData.ClassName = _className;
            }
            EditorGUILayout.EndHorizontal();

            if (CurUIAutoBindData.EGenerateType == EGenerateType.Window)
            {
                EditorGUI.BeginChangeCheck();
                CurUIAutoBindData.UILayer = (UILayer)EditorGUILayout.EnumPopup("UI层级:", CurUIAutoBindData.UILayer);
                if (EditorGUI.EndChangeCheck())
                {
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
            }
        }

        private void EmptyGenerateDraw()
        {
            GUI.color = Color.green;
            if (RootUIAutoBindData == null)
            {
                if (GUILayout.Button("generate view mono script"))
                {
                    RootUIAutoBindData = _prefabStage.prefabContentsRoot.AddComponent<UIAutoBindData>();
                    RootUIAutoBindData.EGenerateType = EGenerateType.Window;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    RefreshHierarchy();
                }
            }
            else if (CurUIAutoBindData == null && !IsHeader)
            {
                if (GUILayout.Button("generate item mono script"))
                {
                    CurUIAutoBindData = CurrentTargetGo.AddComponent<UIAutoBindData>();
                    CurUIAutoBindData.EGenerateType = EGenerateType.SubItem;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    RefreshHierarchy();
                }
            }
            GUI.color = GUI.backgroundColor;
        }

        private void DrawScriptableReadItem()
        {
            GUI.color = Color.cyan;
            if (GUILayout.Button("读取持久化绑定数据"))
            {
                var guids = AssetDatabase.FindAssets("t:UIAutoBindDataScriptable", new[] { UIAutoBindDataUtil.DatabaseRoot });
                foreach (var guid in guids)
                {
                    var scriptable = AssetDatabase.LoadAssetAtPath<UIAutoBindDataScriptable>(AssetDatabase.GUIDToAssetPath(guid));
                    if (scriptable != null && scriptable.Owner == null)
                    {
                        Debug.LogError($"读取失败 scriptable.Owner {scriptable.Owner}");
                        continue;
                    }
                    if (scriptable != null && scriptable.Owner == AssetDatabase.LoadAssetAtPath<GameObject>(_prefabStage.assetPath))
                    {
                        UIAutoBindDataUtil.DeserializeToUIAutoBindData(scriptable, _prefabStage.assetPath);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (_prefabStage.assetPath != null)
                {
                    _prefabStage = PrefabStageUtility.OpenPrefab(_prefabStage.assetPath);
                }
            }

            if (GUILayout.Button("Revert All Nested Prefabs"))
            {
                RevertNestedPrefabsRecursively(_prefabStage.prefabContentsRoot);
            }

            GUI.color = GUI.backgroundColor;
        }

        static void RevertNestedPrefabsRecursively(GameObject obj)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(obj) && PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.UserAction);
                Debug.Log("Reverted nested Prefab: " + obj.name);
            }

            foreach (Transform child in obj.transform)
            {
                RevertNestedPrefabsRecursively(child.gameObject);
            }
        }

        private void DrawScriptableItem()
        {
            if (CurUIAutoBindData != null)
            {
                EditorGUILayout.BeginVertical();
                GUI.color = Color.green;
                if (GUILayout.Button("创建Root绑定持久化数据"))
                {
                    UIAutoBindDataUtil.SerializeToUIAutoBindDataScriptable(RootUIAutoBindData, _prefabStage.assetPath);
                }
                if (GUILayout.Button("创建CurrentItem绑定持久化数据"))
                {
                    if (CurUIAutoBindData != null)
                    {
                        UIAutoBindDataUtil.SerializeToUIAutoBindDataScriptable(CurUIAutoBindData, _prefabStage.assetPath);
                    }
                    else
                    {
                        Debug.LogWarning("_curUIAutoBindData is null");
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                GUI.color = GUI.backgroundColor;
                if (_uIAutoBindDataScriptable != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(_uIAutoBindDataScriptable, typeof(UIAutoBindDataScriptable), false);
                    EditorGUI.EndDisabledGroup();
                }
                GUI.color = Color.cyan;
                if (GUILayout.Button("Ping"))
                {
                    EditorGUIUtility.PingObject(_uIAutoBindDataScriptable);
                }
                GUI.color = GUI.backgroundColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RemoveSubUIAutoBindData(UIAutoBindData parentUIAutoBindData, UIAutoBindData curUIAutoBindData)
        {
            if (parentUIAutoBindData == null)
            {
                Debug.LogError("RemoveSubUIAutoBindData parentUIAutoBindData is null");
                return;
            }
        }

        private UGUINodeProviderInfo _isAddedUGUINodeProviderInfo;
        private void DrawMenuItem()
        {
            if (ParentUIAutoBindData == null)
            {
                UpdateHeader();
                return;
            }

            GUI.color = Color.cyan;
            EditorGUILayout.BeginVertical();
            foreach (var info in UGUINodeProviderMenuItemInfos)
            {
                EditorGUILayout.BeginHorizontal();

                _isAddedUGUINodeProviderInfo = ParentUIAutoBindData.GetAdded(info.UGUINodeProviderInfo);

                if (_isAddedUGUINodeProviderInfo != null)
                {
                    GUI.color = GUI.backgroundColor;
                    EditorGUI.BeginChangeCheck();
                    _isAddedUGUINodeProviderInfo.name = EditorGUILayout.TextField(_isAddedUGUINodeProviderInfo.name, GUILayout.MinWidth(200));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetDirty(_prefabStage.prefabContentsRoot);
                    }
                    GUI.color = Color.cyan;
                }

                EditorGUI.BeginDisabledGroup(_isAddedUGUINodeProviderInfo != null);
                if (GUILayout.Button($"+ {info.TypeName}", GUILayout.MinWidth(100)))
                {
                    Undo.RecordObject(ParentUIAutoBindData, "_parentUIAutoBindData.AddUGUINodeProviderInfo");
                    ParentUIAutoBindData.AddUGUINodeProviderInfo(info.UGUINodeProviderInfo);
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(_isAddedUGUINodeProviderInfo == null);
                if (GUILayout.Button($"- {info.TypeName}", GUILayout.MinWidth(100)))
                {
                    Undo.RecordObject(ParentUIAutoBindData, "_parentUIAutoBindData.RemoveUGUINodeProviderInfo");
                    ParentUIAutoBindData.RemoveUGUINodeProviderInfo(info.UGUINodeProviderInfo);
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                GUI.color = GUI.backgroundColor;
                EditorGUILayout.LabelField(info.UGUINodeProviderInfo.parentClassName, EditorStyles.boldLabel);
                GUI.color = Color.cyan;

                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.color = GUI.backgroundColor;
        }
    }
}
#endif
