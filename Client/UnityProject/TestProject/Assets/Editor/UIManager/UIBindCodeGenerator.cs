using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Manager.UIManager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    /// <summary>
    /// UI 绑定代码生成器。
    /// 根据 UIBindData 生成：
    /// 1. {ClassName}NodeProvider.cs — 继承 UINodeProvider，包含所有序列化字段
    /// 2. {ClassName}Window.cs — 继承 UIWindow&lt;TProvider&gt;，业务逻辑模板（仅首次生成）
    /// </summary>
    public static class UIBindCodeGenerator
    {
        /// <summary>生成代码的根目录</summary>
        private const string GeneratedCodeRoot = "Assets/Scripts/Game/UI";

        /// <summary>
        /// 生成绑定代码
        /// </summary>
        public static void Generate(UIBindData bindData)
        {
            if (bindData == null)
            {
                Debug.LogError("[UIBindCodeGenerator] UIBindData is null.");
                return;
            }

            if (string.IsNullOrEmpty(bindData.ClassName))
            {
                Debug.LogError("[UIBindCodeGenerator] ClassName is empty.");
                return;
            }

            if (bindData.Entries.Count == 0)
            {
                Debug.LogWarning("[UIBindCodeGenerator] No entries to generate.");
                return;
            }

            string className = bindData.ClassName;
            string folderPath = $"{GeneratedCodeRoot}/{className}";

            // 确保目录存在
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // 1. 生成 NodeProvider（始终覆盖）
            string providerPath = $"{folderPath}/{className}NodeProvider.cs";
            if (File.Exists(providerPath) && !EditorUtility.DisplayDialog(
                    "覆盖绑定代码",
                    $"绑定代码文件已存在：\n{providerPath}\n\n是否覆盖重新生成？",
                    "覆盖",
                    "取消"))
            {
                Debug.Log($"[UIBindCodeGenerator] Generate canceled: {providerPath}");
                return;
            }

            string providerCode = GenerateNodeProviderCode(bindData);
            File.WriteAllText(providerPath, providerCode, Encoding.UTF8);
            Debug.Log($"[UIBindCodeGenerator] Generated: {providerPath}");

            // 2. 生成 Window（仅首次，不覆盖已有业务代码）
            string windowPath = $"{folderPath}/{className}Window.cs";
            if (!File.Exists(windowPath))
            {
                string windowCode = GenerateWindowCode(bindData);
                File.WriteAllText(windowPath, windowCode, Encoding.UTF8);
                Debug.Log($"[UIBindCodeGenerator] Generated: {windowPath}");
            }
            else
            {
                Debug.Log($"[UIBindCodeGenerator] Window file already exists, skipped: {windowPath}");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将生成的 NodeProvider 绑定到 Prefab 上
        /// </summary>
        public static void BindToPrefab(UIBindData bindData, PrefabStage prefabStage)
        {
            if (bindData == null || prefabStage == null) return;

            string className = bindData.ClassName;
            string providerTypeName = $"{className}NodeProvider";

            // 查找生成的 NodeProvider 类型
            Type providerType = FindType(providerTypeName);
            if (providerType == null)
            {
                Debug.LogWarning($"[UIBindCodeGenerator] Type '{providerTypeName}' not found. Generate code first and wait for compilation.");
                return;
            }

            GameObject root = prefabStage.prefabContentsRoot;

            // 获取或添加 NodeProvider 组件
            var existingProvider = root.GetComponent(providerType);
            if (existingProvider == null)
            {
                existingProvider = root.AddComponent(providerType);
            }

            // 通过 SerializedObject 赋值
            var so = new SerializedObject(existingProvider);
            int boundCount = 0;

            foreach (var entry in bindData.Entries)
            {
                var target = ResolveTarget(root, entry);
                if (target == null)
                {
                    Debug.LogError($"[UIBindCodeGenerator] Bind target missing: {entry.VariableName}, path='{entry.TargetPath}', type='{entry.ComponentTypeName}'.");
                    continue;
                }

                var prop = so.FindProperty(entry.VariableName);
                if (prop == null)
                {
                    Debug.LogWarning($"[UIBindCodeGenerator] Field '{entry.VariableName}' not found on {providerTypeName}. Regenerate code.");
                    continue;
                }

                prop.objectReferenceValue = target;
                entry.Target = target;
                boundCount++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bindData);
            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);

            Debug.Log($"[UIBindCodeGenerator] Bound {boundCount}/{bindData.Entries.Count} entries to {providerTypeName}.");
        }

        private static UnityEngine.Object ResolveTarget(GameObject root, UIBindEntry entry)
        {
            if (root == null || entry == null) return null;

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

        // ═══════════════════════════════════════════════════════════════
        // 代码生成
        // ═══════════════════════════════════════════════════════════════

        private static string GenerateNodeProviderCode(UIBindData bindData)
        {
            string className = bindData.ClassName;
            var sb = new StringBuilder();

            // 收集需要的 using
            var usings = new HashSet<string>
            {
                "UnityEngine",
                "Manager.UIManager"
            };

            foreach (var entry in bindData.Entries)
            {
                string ns = GetNamespace(entry.ComponentTypeName);
                if (!string.IsNullOrEmpty(ns))
                    usings.Add(ns);
            }

            // 写入文件
            sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
            sb.AppendLine("// Auto-generated by UIBindCodeGenerator. DO NOT EDIT.");
            sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            foreach (var ns in usings.OrderBy(s => s))
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace Game.UI.{className}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}NodeProvider : UINodeProvider");
            sb.AppendLine("    {");

            foreach (var entry in bindData.Entries)
            {
                string shortType = GetShortTypeName(entry.ComponentTypeName);
                sb.AppendLine($"        [SerializeField] public {shortType} {entry.VariableName};");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateWindowCode(UIBindData bindData)
        {
            string className = bindData.ClassName;
            var sb = new StringBuilder();

            sb.AppendLine($"using Manager.UIManager;");
            sb.AppendLine();
            sb.AppendLine($"namespace Game.UI.{className}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {className} 业务逻辑窗口");
            sb.AppendLine($"    /// 通过 Nodes.xxx 访问绑定的 UI 组件引用");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className}Window : UIWindow<{className}NodeProvider>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public override string WindowId => \"{className}\";");
            sb.AppendLine($"        public override UILayer LayerId => UILayer.{bindData.Layer};");
            sb.AppendLine();
            sb.AppendLine("        public override void OnAwake()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnAwake();");
            sb.AppendLine("            // 初始化逻辑");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnShow(UIWindowData data = null)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnShow(data);");
            sb.AppendLine("            // 显示逻辑");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnHide()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnHide();");
            sb.AppendLine("            // 隐藏逻辑");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void RegisterEvents()");
            sb.AppendLine("        {");
            sb.AppendLine("            // 注册事件，例如：");
            sb.AppendLine("            // Nodes.BtnClose.onClick.AddListener(OnCloseClick);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void UnregisterEvents()");
            sb.AppendLine("        {");
            sb.AppendLine("            // 注销事件，例如：");
            sb.AppendLine("            // Nodes.BtnClose.onClick.RemoveListener(OnCloseClick);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════
        // 工具方法
        // ═══════════════════════════════════════════════════════════════

        private static string GetNamespace(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            int lastDot = fullTypeName.LastIndexOf('.');
            return lastDot > 0 ? fullTypeName.Substring(0, lastDot) : null;
        }

        private static string GetShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return "Object";
            int lastDot = fullTypeName.LastIndexOf('.');
            return lastDot >= 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;
        }

        private static Type FindTypeByFullName(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullTypeName);
                    if (type != null) return type;
                }
                catch
                {
                    // 忽略无法加载的程序集
                }
            }
            return null;
        }

        /// <summary>
        /// 在所有已加载程序集中查找类型
        /// </summary>
        private static Type FindType(string typeName)
        {
            // 先尝试在所有程序集中按短名称查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == typeName)
                            return type;
                    }
                }
                catch
                {
                    // 忽略无法加载的程序集
                }
            }
            return null;
        }
    }
}
