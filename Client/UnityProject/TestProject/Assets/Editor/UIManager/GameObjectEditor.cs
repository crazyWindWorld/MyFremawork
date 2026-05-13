/* using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Framework.Editor.UIControlBinding.Scripts;
using Framework.Hot.UIFramework.UI;
using Framework.Hot.UIFramework.UIControlBinding.Scripts;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Framework.Hot.UIFramework.UIControlBinding.Scripts.UIControlData;
using NamingConvention = Framework.Hot.UIFramework.UIControlBinding.Scripts.NamingConvention;
using UIControlData = Framework.Hot.UIFramework.UIControlBinding.Scripts.UIControlData;
using HotFramework.Config;

namespace Framework.Editor.UIFramework
{
    [CustomEditor(typeof(GameObject))]
    public class GameObjectEditor : UnityEditor.Editor
    {
        protected GameObject CurrentTargetGo;
        protected string UIGUID;

        #region 绘制Unity原来的Inspector
        protected Type EditorType;
        protected UnityEditor.Editor Instance;
        protected static readonly object[] EmptyArray = Array.Empty<object>();
        protected static MethodInfo onHeaderGUI;
        #endregion

        /// <summary>
        /// View的UIControlData
        /// </summary>
        protected UIControlData RootUIControlData;
        /// <summary>
        /// 包含自己的UIControlData父节点，只用于普通ctrl
        /// subdata不能使用它
        /// </summary>
        protected UIControlData ParentUIControlData;
        /// <summary>
        /// 当前选择UIControlData节点
        /// </summary>
        protected UIControlData CurUIControlData;

        protected bool IsShowGenetateMono;
        protected bool IsHeader;

        protected List<UGUINodeProviderMenuItemInfo> UGUINodeProviderMenuItemInfos = new();
        protected bool IsFixedButton;

        private bool _isCurDrawMenu;
        private bool _isReadScriptableMenu;
        private bool _animationDrawMenu;
        private bool _isDrawHeaderDetail = true;

        /// <summary>
        /// 是否开启Hierarchy检测自动修复Missing
        /// 如果Hierarchy节点太多，很卡可以尝试关闭它
        /// </summary>
        public static bool IsAutoFixedMissing = true;


        private UIControlDataScriptable _uIControlDataScriptable;
        private List<GameObject> _missingList = new();

        private bool _initialize;

        private void OnEnable()
        {
            if (_initialize) return;
            _initialize = true;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            //获取编辑器类型
            EditorType ??= Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetTypes()
                .FirstOrDefault(m => m.Name == "GameObjectInspector");

            //创建编辑器类实例
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
            _animationDrawMenu = EditorPrefs.GetBool("animationDrawMenu", false);
            _isDrawHeaderDetail = EditorPrefs.GetBool("isDrawHeaderDetail", true);

            IsAutoFixedMissing = EditorPrefs.GetBool("IsAutoFixedMissing", false);

            if (CurUIControlData != null)
            {
                _uIControlDataScriptable = AssetDatabase.LoadAssetAtPath<UIControlDataScriptable>($"{UIControlDataScriptableUtil.DatabaseRoot}/{CurUIControlData.ClassName}.asset");
            }

            _missingList.Clear();
            UIControlDataScriptableUtil.CollectMissingScriptsRecursively(_prefabStage.prefabContentsRoot, _missingList);
            var isSave = _missingList.Count > 0;
            foreach (var missGo in _missingList)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(missGo);
            }
            if (isSave)
            {
                SetDirty(_prefabStage.prefabContentsRoot);
            }
        }

        private List<CtrlItemData> tempAllCtrlDatas = new List<CtrlItemData>();
        private void RefreshHierarchy()
        {
            if (RootUIControlData == null) return;
            var coms = RootUIControlData.GetComponentsInChildren<UIControlData>(true);
            tempAllCtrlDatas.Clear();
            foreach (var com in coms)
            {
                // 处理ctrl的层级关系
                foreach (var ctrl in com.ctrlItemDatas)
                {
                    tempAllCtrlDatas.Add(ctrl);
                }
                // ctrl只能先全部存入allCtrlDatas后续再进行统一分配
                com.ctrlItemDatas.Clear();
                // sub可以通过UIControlData来找到
                com.subUIItemDatas.Clear();

                // 处理sub的层级关系
                var parent = EditorTools.FindComponentInParent<UIControlData>(com.transform, false);
                if (parent == null) continue;
                //if (com.EGenerateType == EGenerateType.None)
                //    com.EGenerateType = EGenerateType.SubItem;
                if (string.IsNullOrEmpty(com.VariableName))
                    com.VariableName = EditorTools.GetVariableName(com.name, NamingConvention.PascalCase);
                if (string.IsNullOrEmpty(com.ClassName))
                    com.ClassName = EditorTools.GetVariableName($"{parent.ClassName}_{com.name}", NamingConvention.PascalCase);

                // loopsubitem不加入window生成
                if (com.EGenerateType == EGenerateType.LoopSubItem || com.EGenerateType == EGenerateType.None) continue;

                var subData = new SubUIItemData() { subUIData = com };
                parent.AddSubControlData(subData);
            }
            foreach (var ctrl in tempAllCtrlDatas)
            {
                // ctrl可以加到自己下，所以搜索包括自己
                if (ctrl.targets[0] == null) continue;
                var parent = EditorTools.FindComponentInParent<UIControlData>(ctrl.type.Equals(nameof(GameObject)) ? (ctrl.targets[0] as GameObject)?.transform : (ctrl.targets[0] as Component)?.transform);
                parent.AddControlData(ctrl);
            }
        }

        private void OnHierarchyChanged()
        {
            //CheckHierarchy(_rootUIControlData, _rootUIControlData);
            RefreshHierarchy();
        }

        private void UpdateIsNeedBinding()
        {
            if (RootUIControlData != null)
            {
                _isNeedBinding = UICopyEditor.IsBindingAllUGUINodeProvider(RootUIControlData);
            }
        }

        private void UpdateMenuInfos()
        {
            UGUINodeProviderMenuItemInfos.Clear();
            // 给 UIControlData 进行支持，这里就不判断了
            //if (_curUIControlData == null && !isHeader)
            {
                UGUINodeProviderMenuItemInfos = UIControlData.GetUGUINodeProviderMenuItemInfos(CurrentTargetGo);
            }
        }

        private PrefabStage _prefabStage;
        private void UpdateHeader()
        {
            if (CurrentTargetGo == null || _prefabStage == null || _prefabStage.prefabContentsRoot == null)
            {
                IsShowGenetateMono = false;
                return;
            }

            IsShowGenetateMono = _prefabStage != null && ProjectSettingConfig.Instance.UIBindFolders.Any(v => _prefabStage.assetPath.Contains(v));
            IsHeader = _prefabStage.prefabContentsRoot == Selection.activeGameObject;
            RootUIControlData = _prefabStage.prefabContentsRoot.GetComponent<UIControlData>();

            CurUIControlData = CurrentTargetGo.GetComponent<UIControlData>();
            if (CurUIControlData != null)
            {
                //_curUIControlData.EGenerateType = isHeader ? EGenerateType.Window : EGenerateType.SubItem;
                if (string.IsNullOrEmpty(CurUIControlData.ClassName))
                {
                    CurUIControlData.ClassName = CurrentTargetGo.name;
                }
            }

            ParentUIControlData = EditorTools.FindComponentInParent<UIControlData>(CurrentTargetGo.transform);
            if (ParentUIControlData == null) return;
            if (string.IsNullOrEmpty(ParentUIControlData.ClassName))
            {
                ParentUIControlData.ClassName = ParentUIControlData.gameObject.name;
            }
        }

        public static void SetDirty(GameObject gameObject)
        {
            EditorUtility.SetDirty(gameObject);
#if UNITY_2021_1_OR_NEWER
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
#else
            var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
#endif
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
            //base.OnHeaderGUI();
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
            SupportUnityEngineComponentTypeMap();
            var _prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (_prefabStage == null)
            {
                return;
            }

            var _rootUIControlData = _prefabStage.prefabContentsRoot.GetComponent<UIControlData>();
            if (_rootUIControlData != null)
            {
                UICopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, _rootUIControlData, _rootUIControlData);
            }

            // 为单独生成做的绑定，单独生成的不会被root扫描成subdata
            var selectGo = Selection.gameObjects;
            if (selectGo.Length != 1 || selectGo[0] == null) return;
            var com = selectGo[0].GetComponent<UIControlData>();
            if (com == null)
            {
                return;
            }
            var parent = EditorTools.FindComponentInParent<UIControlData>(selectGo[0].transform, false);
            UICopyEditor.BindingAllUGUINodeProvider(selectGo[0], parent == null ? com : parent, com);
        }

        private string _variableName;
        private string _className;
        private bool _isNeedBinding;
        public override void OnInspectorGUI()
        {
            //if (instance)
            //{
            //    instance.OnInspectorGUI();
            //}
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

            EditorGUI.BeginChangeCheck();
            IsAutoFixedMissing = EditorGUILayout.Toggle("自动修复Missing", IsAutoFixedMissing);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("IsAutoFixedMissing", IsAutoFixedMissing);
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
            if (CurUIControlData == null)
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

            if (CurUIControlData != null && CurUIControlData.EGenerateType != EGenerateType.None)
            {
                DrawHeaderGenerateMenu();
            }
        }
        private void DrawOtherConnect()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前已扫描Mono脚本的程序集：");
            if (SirenixEditorGUI.IconButton(EditorIcons.Refresh))
            {
                SupportUnityEngineComponentTypeMap();
                OnEnable();
            }
            EditorGUILayout.EndHorizontal();
            foreach (var assemblieName in AssemblieNames)
            {
                EditorGUILayout.LabelField(assemblieName);
            }

            DrawSeparator();
            GUILayout.Space(5);
        }

        private void DrawGenerateMenu()
        {
            if (GUILayout.Button("一键生成"))
            {
                if (RootUIControlData == null)
                {
                    Debug.LogError("_rootUIControlData is null");
                    return;
                }
                // generate all

                // generate scriptable
                UIControlDataScriptableUtil.SerializeToControlDataScriptable(RootUIControlData, _prefabStage.assetPath);

                // generate allDatas
                var allDatas = EditorTools.GetAllComponentsInPrefab<UIControlData>(_prefabStage.prefabContentsRoot);
                foreach (var com in allDatas)
                {
                    // generate .cs
                    UICopyEditor.CopyUIByUIControlData(com.gameObject, com, RootUIControlData);
                }

                UICopyEditor.GenerateAllMonoByData(RootUIControlData);

                // 只能等编译完成再进行绑定，不然这个mono脚本还没注入unity这时候无法把这个cs给添加到gameObject上
                // UICopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, _rootUIControlData, _rootUIControlData);
            }


            EditorGUI.BeginDisabledGroup(!_isNeedBinding);
            if (GUILayout.Button("一键绑定"))
            {
                if (_prefabStage.prefabContentsRoot == null)
                {
                    Debug.LogError("_prefabStage.prefabContentsRoot is null");
                    return;
                }

                UICopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, RootUIControlData, RootUIControlData);
                UpdateIsNeedBinding();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawHeaderGenerateMenu()
        {
            if (IsHeader)
            {
                if (GUILayout.Button("一键生成"))
                {
                    if (RootUIControlData == null)
                    {
                        Debug.LogError("_rootUIControlData is null");
                        return;
                    }
                    // generate all

                    // generate scriptable
                    UIControlDataScriptableUtil.SerializeToControlDataScriptable(RootUIControlData, _prefabStage.assetPath);

                    // generate allDatas
                    var allDatas = EditorTools.GetAllComponentsInPrefab<UIControlData>(_prefabStage.prefabContentsRoot);
                    foreach (var com in allDatas)
                    {
                        // generate .cs
                        UICopyEditor.CopyUIByUIControlData(com.gameObject, com, RootUIControlData);
                    }

                    UICopyEditor.GenerateAllMonoByData(RootUIControlData);

                    // 只能等编译完成再进行绑定，不然这个mono脚本还没注入unity这时候无法把这个cs给添加到gameObject上
                    // UICopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, _rootUIControlData, _rootUIControlData);
                }


                EditorGUI.BeginDisabledGroup(!_isNeedBinding);
                if (GUILayout.Button("一键绑定"))
                {
                    if (_prefabStage.prefabContentsRoot == null)
                    {
                        Debug.LogError("_prefabStage.prefabContentsRoot is null");
                        return;
                    }

                    UICopyEditor.BindingAllUGUINodeProvider(_prefabStage.prefabContentsRoot, RootUIControlData, RootUIControlData);
                    UpdateIsNeedBinding();
                }
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

                    // generate scriptable
                    UIControlDataScriptableUtil.SerializeToControlDataScriptable(CurUIControlData, _prefabStage.assetPath);

                    UICopyEditor.CopyUIByUIControlData(CurrentTargetGo, CurUIControlData, RootUIControlData);
                    UICopyEditor.GenerateAllMonoByData(CurUIControlData);
                }


                EditorGUI.BeginDisabledGroup(!_isNeedBinding);
                if (GUILayout.Button("一键绑定"))
                {
                    if (_prefabStage.prefabContentsRoot == null)
                    {
                        Debug.LogError("_prefabStage.prefabContentsRoot is null");
                        return;
                    }

                    var parent = EditorTools.FindComponentInParent<UIControlData>(CurrentTargetGo.transform, false);
                    UICopyEditor.BindingAllUGUINodeProvider(CurrentTargetGo, parent == null ? CurUIControlData : parent, CurUIControlData);
                    UpdateIsNeedBinding();
                }
            }

            EditorGUI.EndDisabledGroup();
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
                    CurUIControlData = CurrentTargetGo.GetComponent<UIControlData>();
                    if (CurUIControlData == null)
                    {
                        Debug.LogError("_curUIControlData is null. remove fail");
                        return;
                    }
                    var parent = EditorTools.FindComponentInParent<UIControlData>(CurrentTargetGo.transform, IsHeader);
                    RmoveSubUIControlData(parent, CurUIControlData);

                    GameObject.DestroyImmediate(CurUIControlData);
                    CurUIControlData = null;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    OnEnable(); // 这里不会触发onenable自己手动调用刷新下
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
                        var coms = EditorTools.GetAllComponentsInPrefab<UIControlData>(_prefabStage.prefabContentsRoot);
                        foreach (var com in coms)
                        {
                            GameObject.DestroyImmediate(com);
                        }
                        RootUIControlData = null;
                        ParentUIControlData = null;
                        CurUIControlData = null;
                        SetDirty(_prefabStage.prefabContentsRoot);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        OnEnable(); // 这里不会触发onenable自己手动调用刷新下
                        return;
                    }
                }
                GUI.color = GUI.backgroundColor;
            }
        }

        private void DrawHeaderDetail()
        {
            //EditorGUI.BeginDisabledGroup(true);
            EditorGUI.BeginChangeCheck();
            CurUIControlData.EGenerateType = (EGenerateType)EditorGUILayout.EnumPopup("生成类型：", CurUIControlData.EGenerateType);
            if (EditorGUI.EndChangeCheck())
            {
                SetDirty(_prefabStage.prefabContentsRoot);
            }

            if (CurUIControlData.EGenerateType == EGenerateType.None) return;

            //EditorGUI.EndDisabledGroup();
            if (CurUIControlData.EGenerateType == EGenerateType.SubItem)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Generate Variable Name");
                _variableName = EditorGUILayout.TextField(CurUIControlData.VariableName, EditorStyles.textField);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(CurUIControlData, "_curUIControlData.VariableName");
                    CurUIControlData.VariableName = _variableName;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Generate Class Name");
            _className = EditorGUILayout.TextField(CurUIControlData.ClassName, EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(CurUIControlData, "_curUIControlData.ClassName");
                CurUIControlData.ClassName = _className;
            }
            EditorGUILayout.EndHorizontal();

            if (CurUIControlData.EGenerateType == EGenerateType.Window)
            {
                EditorGUI.BeginChangeCheck();
                CurUIControlData.UILayer = (UILayer)EditorGUILayout.EnumPopup("UI层级：", CurUIControlData.UILayer);
                CurUIControlData.IsAutoCreateCtrl = EditorGUILayout.Toggle("是否生成Ctrl层", CurUIControlData.IsAutoCreateCtrl);
                CurUIControlData.IsJustOne = EditorGUILayout.Toggle("只生成单个UI实例", CurUIControlData.IsJustOne);
                if (EditorGUI.EndChangeCheck())
                {
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
                EditorGUI.BeginChangeCheck();
                _animationDrawMenu = EditorGUILayout.Foldout(_animationDrawMenu, "Open Close Animation");
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool("animationDrawMenu", _animationDrawMenu);
                }
                if (_animationDrawMenu)
                {
                    DrawAnimationClipItem();
                }
            }
        }

        private void EmptyGenerateDraw()
        {
            GUI.color = Color.green;
            if (RootUIControlData == null)
            {
                if (GUILayout.Button("generate view mono script"))
                {
                    RootUIControlData = _prefabStage.prefabContentsRoot.AddComponent<UIControlData>();
                    RootUIControlData.EGenerateType = EGenerateType.Window;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    RefreshHierarchy();
                }
            }
            else if (CurUIControlData == null && !IsHeader)
            {
                if (GUILayout.Button("generate item mono script"))
                {
                    CurUIControlData = CurrentTargetGo.AddComponent<UIControlData>();
                    CurUIControlData.EGenerateType = EGenerateType.SubItem;
                    SetDirty(_prefabStage.prefabContentsRoot);
                    RefreshHierarchy();
                }

                // 只给不是UIControlData的做menu支持
                //DrawMenuItem();
            }
            GUI.color = GUI.backgroundColor;
        }

        private void DrawScriptableReadItem()
        {
            GUI.color = Color.cyan;
            if (GUILayout.Button("读取持久化绑定数据"))
            {
                var guids = AssetDatabase.FindAssets("t:UIControlDataScriptable", new[] { UIControlDataScriptableUtil.DatabaseRoot });
                foreach (var guid in guids)
                {
                    var scriptable = AssetDatabase.LoadAssetAtPath<UIControlDataScriptable>(AssetDatabase.GUIDToAssetPath(guid));
                    if (scriptable != null && scriptable.Owner == null)
                    {
                        Debug.LogError($"读取失败 scriptable.Owner {scriptable.Owner}");
                        continue;
                    }
                    if (scriptable != null && scriptable.Owner == AssetDatabase.LoadAssetAtPath<GameObject>(_prefabStage.assetPath))
                    {
                        UIControlDataScriptableUtil.DeserializeToUIControlData(scriptable, _prefabStage.assetPath);
                    }
                }

                //EditorSceneManager.ClosePreviewScene(_prefabStage.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //_prefabStage.ClearDirtiness();
                if (_prefabStage.assetPath != null)
                {
                    _prefabStage = PrefabStageUtility.OpenPrefab(_prefabStage.assetPath);
                }

                //PrefabUtility.RevertPrefabInstance(_prefabStage.prefabContentsRoot, InteractionMode.UserAction);
                //SetDirty(_prefabStage.prefabContentsRoot);
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Revert All Nested Prefabs"))
            {
                // Revert 根对象的所有嵌套 Prefab（如果涉及有嵌套prefab的话
                // 它保留的是基于这个prefab上的信息，而不会直接应用最新嵌套prefab的修改）
                // 需要手动指定Reverted all
                RevertNestedPrefabsRecursively(_prefabStage.prefabContentsRoot);
            }

            GUI.color = GUI.backgroundColor;
        }

        static void RevertNestedPrefabsRecursively(GameObject obj)
        {
            // 检查当前对象是否是嵌套的Prefab实例
            if (PrefabUtility.IsAnyPrefabInstanceRoot(obj) && PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                // 执行Revert All操作
                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.UserAction);
                Debug.Log("Reverted nested Prefab: " + obj.name);
            }

            // 递归检查子对象
            foreach (Transform child in obj.transform)
            {
                RevertNestedPrefabsRecursively(child.gameObject);
            }
        }

        private void DrawScriptableItem()
        {
            if (CurUIControlData != null)
            {
                EditorGUILayout.BeginVertical();
                GUI.color = Color.green;
                if (GUILayout.Button("创建Root绑定持久化数据"))
                {
                    UIControlDataScriptableUtil.SerializeToControlDataScriptable(RootUIControlData, _prefabStage.assetPath);
                }
                if (GUILayout.Button("创建CurrentItem绑定持久化数据"))
                {
                    if (CurUIControlData != null)
                    {
                        // generate scriptable
                        UIControlDataScriptableUtil.SerializeToControlDataScriptable(CurUIControlData, _prefabStage.assetPath);
                    }
                    else
                    {
                        Debug.LogWarning("_curUIControlData is null");
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                GUI.color = GUI.backgroundColor;
                if (_uIControlDataScriptable != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(_uIControlDataScriptable, typeof(UIControlDataScriptable), false);
                    EditorGUI.EndDisabledGroup();
                }
                GUI.color = Color.cyan;
                if (GUILayout.Button("Ping"))
                {
                    EditorGUIUtility.PingObject(_uIControlDataScriptable);
                }
                GUI.color = GUI.backgroundColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RmoveSubUIControlData(UIControlData parentUIControlData, UIControlData curUIControlData)
        {
            if (parentUIControlData == null)
            {
                Debug.LogError("RmoveSubUIControlData parentUIControlData is null");
                return;
            }
            // 把item从parent中移除，移除之前先把当前的item的ctrl控件进行移交给parent
            foreach (var ctrl in curUIControlData.ctrlItemDatas)
            {
                parentUIControlData.AddControlData(ctrl);
            }
            foreach (var subCtrl in curUIControlData.subUIItemDatas)
            {
                parentUIControlData.AddSubControlData(subCtrl);
            }
            // 把item从parent中移除
            parentUIControlData.RemoveSubControlData(new SubUIItemData()
            {
                subUIData = curUIControlData
            });
        }

        private void DrawAnimationClipItem()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Open动画");
            EditorGUI.BeginChangeCheck();
            CurUIControlData.OpenAnimaClip = (AnimationClip)EditorGUILayout.ObjectField(CurUIControlData.OpenAnimaClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetDirty(_prefabStage.prefabContentsRoot);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Close动画");
            EditorGUI.BeginChangeCheck();
            CurUIControlData.CloseAnimaClip = (AnimationClip)EditorGUILayout.ObjectField(CurUIControlData.CloseAnimaClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetDirty(_prefabStage.prefabContentsRoot);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Open音效");
            EditorGUI.BeginChangeCheck();
            CurUIControlData.OpenUIAudio = (AudioClip)EditorGUILayout.ObjectField(CurUIControlData.OpenUIAudio, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetDirty(_prefabStage.prefabContentsRoot);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();
        }

        private List<CtrlItemData> _tempCtrlItemDatas = new List<CtrlItemData>();
        private CtrlItemData _isAddedCtrlItemData;
        private void DrawMenuItem()
        {
            if (ParentUIControlData == null)
            {
                UpdateHeader();
                return;
            }

            if (!IsAutoFixedMissing)
            {
                EditorGUILayout.BeginHorizontal();

                if (CurUIControlData == null)
                {
                    if (GUILayout.Button($"手动修复当前的Ctrl Missing绑定"))
                    {
                        foreach (var info in UGUINodeProviderMenuItemInfos)
                        {
                            ParentUIControlData.UpdateUIControlData(info.CtrlItemData);
                        }
                    }
                }
                else
                {
                    if (CurUIControlData.EGenerateType == EGenerateType.SubItem)
                    {
                        if (GUILayout.Button($"手动修复当前的Item Missing绑定"))
                        {
                            var parent = EditorTools.FindComponentInParent<UIControlData>(CurrentTargetGo.transform, false);
                            if (parent != null)
                            {
                                parent.FixedUpdateSubUIControlData(CurrentTargetGo.GetComponent<UIControlData>());
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.color = Color.cyan;
            EditorGUILayout.BeginVertical();
            foreach (var info in UGUINodeProviderMenuItemInfos)
            {
                EditorGUILayout.BeginHorizontal();


                _isAddedCtrlItemData = ParentUIControlData.GetAdded(info.CtrlItemData);

                //if (isAddedCtrlItemData == null)
                //{
                //    GUI.color = GUI.backgroundColor;
                //    EditorGUI.BeginChangeCheck();
                //    info.CtrlItemData.name = EditorGUILayout.TextField(info.CtrlItemData.name, GUILayout.MinWidth(200));
                //    if (EditorGUI.EndChangeCheck())
                //    {
                //        SetDirty(_prefabStage.prefabContentsRoot);
                //    }
                //    GUI.color = Color.cyan;
                //}
                if (_isAddedCtrlItemData != null)
                {
                    GUI.color = GUI.backgroundColor;
                    EditorGUI.BeginChangeCheck();
                    _isAddedCtrlItemData.name = EditorGUILayout.TextField(_isAddedCtrlItemData.name, GUILayout.MinWidth(200));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetDirty(_prefabStage.prefabContentsRoot);
                    }
                    GUI.color = Color.cyan;
                }

                EditorGUI.BeginDisabledGroup(_isAddedCtrlItemData != null);
                if (GUILayout.Button($"+ {info.TypeName}", GUILayout.MinWidth(100)))
                {
                    Undo.RecordObject(ParentUIControlData, "_parentUIControlData.AddControlData");
                    ParentUIControlData.AddControlData(info.CtrlItemData);
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(_isAddedCtrlItemData == null);
                if (GUILayout.Button($"- {info.TypeName}", GUILayout.MinWidth(100)))
                {
                    //var data = new CtrlItemData()
                    //{
                    //    name = EditorTools.GetVariableName(CurrentTargetGo, NamingConvention.PascalCase),
                    //    type = info.TypeName,
                    //};
                    //data.targets[0] = info.Object;
                    Undo.RecordObject(ParentUIControlData, "_parentUIControlData.RemoveControlData");
                    ParentUIControlData.RemoveControlData(info.CtrlItemData);
                    SetDirty(_prefabStage.prefabContentsRoot);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                GUI.color = GUI.backgroundColor;
                EditorGUILayout.LabelField(info.CtrlItemData.parentClassName, EditorStyles.boldLabel);
                GUI.color = Color.cyan;

                EditorGUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();
            GUI.color = GUI.backgroundColor;
        }
    }
} */