using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Manager.UIManager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(GameObject))]
    public class UIBindEditor : UnityEditor.Editor
    {
        private const string BindDataRoot = "Assets/Resources/UIBindData";
        private const float HierarchyIconSize = 16f;
        private const float HierarchyIconGap = 2f;
        private const float HierarchyIconOffsetX = -2f;
        private const float HierarchyFoldoutWidth = 14f;

        private static Type _gameObjectInspectorType;
        private static System.Reflection.MethodInfo _onHeaderGUIMethod;
        private UnityEditor.Editor _defaultEditor;
        private PrefabStage _prefabStage;
        private GameObject _currentGo;
        private UIBindData _bindData;
        private bool _isRoot;
        private bool _initialized;
        private bool _showBindings = true;
        private bool _showAvailable = true;
        private readonly List<BindableComponentInfo> _bindableComponents = new List<BindableComponentInfo>();

        static UIBindEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        private struct BindableComponentInfo
        {
            public Component Component;
            public string TypeName;
            public string FullTypeName;
            public bool IsBound;
        }

        private void OnEnable()
        {
            if (_initialized) return;
            _initialized = true;

            if (_gameObjectInspectorType == null)
            {
                _gameObjectInspectorType = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor))
                    .GetTypes().FirstOrDefault(t => t.Name == "GameObjectInspector");
            }

            if (_defaultEditor == null && _gameObjectInspectorType != null)
                _defaultEditor = CreateEditor(targets, _gameObjectInspectorType);

            if (_onHeaderGUIMethod == null && _gameObjectInspectorType != null)
                _onHeaderGUIMethod = _gameObjectInspectorType.GetMethod("OnHeaderGUI",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            _prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (_prefabStage == null) return;

            _currentGo = target as GameObject;
            if (_currentGo == null) return;

            _isRoot = _prefabStage.prefabContentsRoot == _currentGo;
            _bindData = FindBindDataAsset(_prefabStage.assetPath);
            RestoreMissingTargets();
            RefreshBindableComponents();
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            _initialized = false;

            if (_defaultEditor != null)
            {
                DestroyImmediate(_defaultEditor);
                _defaultEditor = null;
            }
        }

        protected override void OnHeaderGUI()
        {
            if (_onHeaderGUIMethod != null && _defaultEditor != null)
                _onHeaderGUIMethod.Invoke(_defaultEditor, null);
        }

        public override void OnInspectorGUI()
        {
            if (_prefabStage == null || _currentGo == null) return;
            if (_isRoot) DrawRootPanel();
            DrawBindablePanel();
        }

        private void DrawRootPanel()
        {
            EditorGUILayout.Space(5);
            DrawSeparator("UI Bind 配置");

            if (_bindData == null)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("创建 UI 绑定数据文件", GUILayout.Height(30)))
                {
                    _bindData = CreateBindDataAsset(_prefabStage.assetPath);
                    RefreshBindableComponents();
                }
                GUI.color = Color.white;
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("绑定数据文件", _bindData, typeof(UIBindData), false);
            EditorGUILayout.TextField("Prefab 路径", _bindData.PrefabPath);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            _bindData.ClassName = EditorGUILayout.TextField("生成类名", _bindData.ClassName);
            _bindData.Layer = (UILayer)EditorGUILayout.EnumPopup("UI 层级", _bindData.Layer);
            if (EditorGUI.EndChangeCheck()) MarkBindDataDirty();

            _showBindings = EditorGUILayout.Foldout(_showBindings, $"已绑定列表 ({_bindData.Entries.Count})", true);
            if (_showBindings) DrawBindingList();

            DrawGenerateButtons();

            GUI.color = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("删除绑定数据文件") && EditorUtility.DisplayDialog("确认", "将删除绑定数据 .asset 文件，此操作不可恢复！", "确定", "取消"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_bindData));
                _bindData = null;
                RefreshBindableComponents();
            }
            GUI.color = Color.white;
        }

        private void DrawBindingList()
        {
            if (_bindData.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无绑定条目，选择子节点添加组件绑定。", MessageType.Info);
                return;
            }

            int removeIndex = -1;
            for (int i = 0; i < _bindData.Entries.Count; i++)
            {
                var entry = _bindData.Entries[i];
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(entry.VariableName, GUILayout.MinWidth(120));

                EditorGUILayout.LabelField(GetShortTypeName(entry.ComponentTypeName), EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField(entry.TargetPath, EditorStyles.miniLabel, GUILayout.MinWidth(100));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(entry.Target, typeof(UnityEngine.Object), true, GUILayout.MinWidth(100));
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("定位", GUILayout.Width(40)))
                {
                    if (entry.Target is Component comp) Selection.activeGameObject = comp.gameObject;
                    else if (entry.Target is GameObject go) Selection.activeGameObject = go;
                }

                GUI.color = Color.red;
                if (GUILayout.Button("×", GUILayout.Width(22))) removeIndex = i;
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                _bindData.RemoveEntry(removeIndex);
                MarkBindDataDirty();
                RefreshBindableComponents();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawBindablePanel()
        {
            if (_bindData == null) return;

            EditorGUILayout.Space(3);
            DrawSeparator("可绑定组件");
            _showAvailable = EditorGUILayout.Foldout(_showAvailable, $"当前节点组件 ({_bindableComponents.Count})", true);
            if (!_showAvailable) return;

            UnityEngine.Object pendingRemoveTarget = null;
            string pendingRemoveType = null;
            BindableComponentInfo? pendingAdd = null;

            for (int i = 0; i < _bindableComponents.Count; i++)
            {
                var info = _bindableComponents[i];
                int entryIndex = FindEntryIndex(GetTargetPath(_currentGo), info.FullTypeName);
                EditorGUILayout.BeginHorizontal();
                DrawComponentIcon(info.Component.GetType());
                EditorGUILayout.LabelField((info.IsBound ? "✓ " : "  ") + info.TypeName, GUILayout.MinWidth(100));
                DrawVariableNameField(entryIndex);
                GUI.color = info.IsBound ? new Color(1f, 0.8f, 0.5f) : new Color(0.5f, 0.9f, 1f);
                if (GUILayout.Button(info.IsBound ? "移除绑定" : "+ 绑定", GUILayout.Width(80)))
                {
                    if (info.IsBound)
                    {
                        pendingRemoveTarget = info.Component;
                        pendingRemoveType = info.FullTypeName;
                    }
                    else
                    {
                        pendingAdd = info;
                    }
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            if (pendingRemoveTarget != null)
            {
                RemoveBindingByTarget(pendingRemoveTarget, pendingRemoveType);
                GUIUtility.ExitGUI();
            }

            if (pendingAdd.HasValue)
            {
                AddBinding(pendingAdd.Value);
                GUIUtility.ExitGUI();
            }

            DrawGameObjectBinding();
        }

        private void DrawGameObjectBinding()
        {
            bool isBound = _bindData.HasEntry(GetTargetPath(_currentGo), typeof(GameObject).FullName);
            int entryIndex = FindEntryIndex(GetTargetPath(_currentGo), typeof(GameObject).FullName);
            EditorGUILayout.BeginHorizontal();
            DrawComponentIcon(typeof(GameObject));
            EditorGUILayout.LabelField((isBound ? "✓ " : "  ") + "GameObject", GUILayout.MinWidth(100));
            DrawVariableNameField(entryIndex);
            GUI.color = isBound ? new Color(1f, 0.8f, 0.5f) : new Color(0.5f, 0.9f, 1f);
            bool shouldToggleBinding = false;
            if (GUILayout.Button(isBound ? "移除绑定" : "+ 绑定", GUILayout.Width(80)))
                shouldToggleBinding = true;
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (!shouldToggleBinding) return;
            if (isBound) RemoveBindingByTarget(_currentGo, typeof(GameObject).FullName);
            else AddGameObjectBinding();
            GUIUtility.ExitGUI();
        }

        private void DrawGenerateButtons()
        {
            if (_bindData == null || _bindData.Entries.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成绑定代码", GUILayout.Height(28))) UIBindCodeGenerator.Generate(_bindData);
            if (GUILayout.Button("绑定到 Prefab", GUILayout.Height(28))) UIBindCodeGenerator.BindToPrefab(_bindData, _prefabStage);
            EditorGUILayout.EndHorizontal();
        }

        public static UIBindData FindBindDataAsset(string prefabPath)
        {
            if (!AssetDatabase.IsValidFolder(BindDataRoot)) return null;

            var guids = AssetDatabase.FindAssets("t:UIBindData", new[] { BindDataRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UIBindData>(path);
                if (asset != null && asset.PrefabPath == prefabPath) return asset;
            }

            string expectedPath = $"{BindDataRoot}/{Path.GetFileNameWithoutExtension(prefabPath)}.asset";
            return AssetDatabase.LoadAssetAtPath<UIBindData>(expectedPath);
        }

        private static UIBindData CreateBindDataAsset(string prefabPath)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(BindDataRoot)) AssetDatabase.CreateFolder("Assets/Resources", "UIBindData");

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{BindDataRoot}/{prefabName}.asset");
            var asset = ScriptableObject.CreateInstance<UIBindData>();
            asset.PrefabPath = prefabPath;
            asset.ClassName = prefabName;
            asset.Layer = UILayer.Normal;

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private void RefreshBindableComponents()
        {
            RestoreMissingTargets();
            _bindableComponents.Clear();
            if (_currentGo == null) return;

            var components = _currentGo.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                if (type == typeof(Transform)) continue;
                if (typeof(UINodeProvider).IsAssignableFrom(type)) continue;
                if (type == typeof(CanvasRenderer)) continue;

                _bindableComponents.Add(new BindableComponentInfo
                {
                    Component = comp,
                    TypeName = type.Name,
                    FullTypeName = type.FullName,
                    IsBound = _bindData != null && _bindData.HasEntry(GetTargetPath(comp.gameObject), type.FullName)
                });
            }
        }

        private void AddBinding(BindableComponentInfo info)
        {
            string variableName = GenerateVariableName(_currentGo.name, info.FullTypeName);
            if (HasDuplicateVariableName(variableName))
            {
                Debug.LogError($"[UIBindEditor] 绑定名称重复：{variableName}。请修改已绑定名称后再添加。");
                return;
            }

            _bindData.AddEntry(variableName, info.FullTypeName, GetTargetPath(_currentGo), info.Component);
            MarkBindDataDirty();
            RefreshBindableComponents();
        }

        private void AddGameObjectBinding()
        {
            string variableName = GenerateVariableName(_currentGo.name, typeof(GameObject).FullName);
            if (HasDuplicateVariableName(variableName))
            {
                Debug.LogError($"[UIBindEditor] 绑定名称重复：{variableName}。请修改已绑定名称后再添加。");
                return;
            }

            _bindData.AddEntry(variableName, typeof(GameObject).FullName, GetTargetPath(_currentGo), _currentGo);
            MarkBindDataDirty();
            RefreshBindableComponents();
        }

        private void RestoreMissingTargets()
        {
            if (_bindData == null || _prefabStage == null || _prefabStage.prefabContentsRoot == null) return;

            bool changed = false;
            var root = _prefabStage.prefabContentsRoot;
            foreach (var entry in _bindData.Entries)
            {
                var target = ResolveEntryTarget(root, entry);
                if (target == null) continue;
                if (entry.Target == target) continue;

                entry.Target = target;
                changed = true;
            }

            if (changed) MarkBindDataDirty();
        }

        private UnityEngine.Object ResolveEntryTarget(GameObject root, UIBindEntry entry)
        {
            if (entry == null) return null;

            GameObject targetGo;
            if (string.IsNullOrEmpty(entry.TargetPath))
            {
                targetGo = root;
            }
            else
            {
                var targetTransform = root.transform.Find(entry.TargetPath);
                if (targetTransform == null) return null;
                targetGo = targetTransform.gameObject;
            }

            if (entry.ComponentTypeName == typeof(GameObject).FullName)
                return targetGo;

            var componentType = FindTypeByFullName(entry.ComponentTypeName);
            return componentType == null ? null : targetGo.GetComponent(componentType);
        }

        private static Type FindTypeByFullName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null) return type;
            }
            return null;
        }

        private void DrawVariableNameField(int entryIndex)
        {
            if (entryIndex < 0)
            {
                GUILayout.Space(120);
                return;
            }

            var entry = _bindData.Entries[entryIndex];
            EditorGUI.BeginChangeCheck();
            string variableName = EditorGUILayout.TextField(entry.VariableName, GUILayout.MinWidth(120));
            if (!EditorGUI.EndChangeCheck()) return;

            entry.VariableName = variableName;
            MarkBindDataDirty();
            if (!IsVariableNameUnique(variableName, entryIndex))
                Debug.LogError($"[UIBindEditor] 绑定名称重复：{variableName}。重复名称会导致生成脚本字段冲突。");
        }

        private static void OnHierarchyWindowItemGUI(int instanceId, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null) return;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.prefabContentsRoot == null) return;

            var bindData = FindBindDataAsset(prefabStage.assetPath);
            if (bindData == null || bindData.Entries.Count == 0) return;

            string targetPath = GetTargetPath(prefabStage.prefabContentsRoot, go);
            var icons = GetBindIconTextures(bindData, targetPath);
            if (icons.Count == 0) return;

            float reservedFoldoutWidth = go.transform.childCount > 0 ? HierarchyFoldoutWidth : 0f;
            float x = selectionRect.x + HierarchyIconOffsetX - reservedFoldoutWidth - icons.Count * (HierarchyIconSize + HierarchyIconGap);
            var iconRect = new Rect(x, selectionRect.y + 1f, HierarchyIconSize, HierarchyIconSize);

            for (int i = 0; i < icons.Count; i++)
            {
                GUI.DrawTexture(iconRect, icons[i], ScaleMode.ScaleToFit);
                iconRect.x += HierarchyIconSize + HierarchyIconGap;
            }
        }

        private static List<Texture> GetBindIconTextures(UIBindData bindData, string targetPath)
        {
            var icons = new List<Texture>();
            for (int i = 0; i < bindData.Entries.Count; i++)
            {
                var entry = bindData.Entries[i];
                if (entry.TargetPath != targetPath) continue;

                var type = entry.ComponentTypeName == typeof(GameObject).FullName
                    ? typeof(GameObject)
                    : FindTypeByFullName(entry.ComponentTypeName);
                if (type == null) continue;

                var content = EditorGUIUtility.ObjectContent(null, type);
                if (content.image == null) continue;

                icons.Add(content.image);
            }
            return icons;
        }

        private void DrawComponentIcon(Type componentType)
        {
            var content = EditorGUIUtility.ObjectContent(null, componentType);
            GUILayout.Label(content.image, GUILayout.Width(18), GUILayout.Height(18));
        }

        private int FindEntryIndex(string targetPath, string typeName)
        {
            if (_bindData == null) return -1;
            for (int i = 0; i < _bindData.Entries.Count; i++)
            {
                var entry = _bindData.Entries[i];
                if (entry.TargetPath == targetPath && entry.ComponentTypeName == typeName)
                    return i;
            }
            return -1;
        }

        private void RemoveBindingByTarget(UnityEngine.Object target, string typeName)
        {
            for (int i = _bindData.Entries.Count - 1; i >= 0; i--)
            {
                if (_bindData.Entries[i].TargetPath == GetTargetPath(target) && _bindData.Entries[i].ComponentTypeName == typeName)
                {
                    _bindData.Entries.RemoveAt(i);
                    MarkBindDataDirty();
                    RefreshBindableComponents();
                    return;
                }
            }
        }

        private string GenerateVariableName(string goName, string componentTypeName)
        {
            string cleanName = new string(goName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (string.IsNullOrEmpty(cleanName)) cleanName = "Item";
            cleanName = char.ToUpper(cleanName[0]) + cleanName.Substring(1);
            string prefix = GetVariablePrefix(componentTypeName);
            return cleanName.StartsWith(prefix, StringComparison.Ordinal) ? cleanName : prefix + cleanName;
        }

        private string GetVariablePrefix(string componentTypeName)
        {
            string shortTypeName = GetShortTypeName(componentTypeName);
            switch (shortTypeName)
            {
                case "Button": return "Btn";
                case "Image": return "Img";
                case "Text": return "Text";
                case "Toggle": return "Toggle";
                case "RawImage": return "RImg";
                case "TextMeshPro":
                case "TextMeshProUGUI": return "Tmp";
                case "Animation": return "Ani";
                case "Animator": return "Anim";
                case "RectTransform": return "Rect";
                case "InputField":
                case "TMP_InputField": return "Ipt";
                case "GameObject": return "Go";
                default: return shortTypeName;
            }
        }

        private bool HasDuplicateVariableName(string variableName)
        {
            return _bindData.Entries.Any(entry => entry.VariableName == variableName);
        }

        private bool IsVariableNameUnique(string variableName, int currentIndex)
        {
            for (int i = 0; i < _bindData.Entries.Count; i++)
            {
                if (i != currentIndex && _bindData.Entries[i].VariableName == variableName)
                    return false;
            }
            return true;
        }

        private string GetTargetPath(UnityEngine.Object target)
        {
            if (target is Component comp) return GetTargetPath(comp.gameObject);
            if (target is GameObject go) return GetTargetPath(go);
            return string.Empty;
        }

        private string GetTargetPath(GameObject go)
        {
            if (_prefabStage == null || go == null) return string.Empty;
            var root = _prefabStage.prefabContentsRoot.transform;
            return GetTargetPath(root.gameObject, go);
        }

        private static string GetTargetPath(GameObject rootGo, GameObject go)
        {
            if (rootGo == null || go == null) return string.Empty;
            var root = rootGo.transform;
            if (go.transform == root) return string.Empty;

            var names = new Stack<string>();
            var current = go.transform;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names);
        }

        private string GetShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return "Unknown";
            int lastDot = fullTypeName.LastIndexOf('.');
            return lastDot >= 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;
        }

        private void MarkBindDataDirty()
        {
            EditorUtility.SetDirty(_bindData);
            AssetDatabase.SaveAssets();
        }

        private void DrawSeparator(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(2);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null) return;
            var bindData = FindBindDataAsset(prefabStage.assetPath);
            if (bindData == null) return;
            UIBindCodeGenerator.BindToPrefab(bindData, prefabStage);
        }
    }
}
