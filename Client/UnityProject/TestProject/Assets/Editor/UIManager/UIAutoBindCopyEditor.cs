#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    public static class UIAutoBindCopyEditor
    {
        private const string OutputRoot = "Assets/Scripts/Game/UI";

        public static bool IsBindingAllUGUINodeProvider(UIAutoBindData rootData)
        {
            if (rootData == null) return false;

            foreach (var info in rootData.UGUINodeProviderInfos)
            {
                if (info.obj == null)
                {
                    return true;
                }
            }

            return false;
        }

        public static void BindingAllUGUINodeProvider(GameObject root, UIAutoBindData parentData, UIAutoBindData currentData)
        {
            if (root == null || parentData == null || currentData == null) return;

            foreach (var info in currentData.UGUINodeProviderInfos)
            {
                if (info.obj == null)
                {
                    Component comp = FindComponentInChildren(root.transform, info.name, info.typeName, parentData.ClassName);
                    info.obj = comp;
                }
            }

            EditorUtility.SetDirty(root);
        }

        private static Component FindComponentInChildren(Transform parent, string name, string typeName, string parentClassName)
        {
            foreach (Transform child in parent)
            {
                string expectedName = EditorTools.GetVariableName(child.name);

                if (child.name == name || expectedName == name)
                {
                    Component[] components = child.GetComponents<Component>();
                    foreach (Component comp in components)
                    {
                        if (comp == null) continue;
                        if (comp.GetType().Name == typeName)
                        {
                            return comp;
                        }
                    }
                }

                Component found = FindComponentInChildren(child, name, typeName, parentClassName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public static void CopyUIByUIAutoBindData(GameObject go, UIAutoBindData curData, UIAutoBindData rootData)
        {
            if (go == null || curData == null || rootData == null) return;

            foreach (var info in curData.UGUINodeProviderInfos)
            {
                if (info.obj != null) continue;

                Transform target = FindChildRecursive(go.transform, info.name);
                if (target != null)
                {
                    Component comp = target.GetComponent(info.typeName);
                    if (comp != null)
                    {
                        info.obj = comp;
                    }
                }
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static void GenerateAllMonoByData(UIAutoBindData rootData)
        {
            if (rootData == null || rootData.EGenerateType != EGenerateType.Window)
            {
                Debug.LogError("GenerateAllMonoByData: rootData is null or not a Window");
                return;
            }

            string className = rootData.ClassName;
            if (string.IsNullOrEmpty(className))
            {
                className = rootData.name;
            }

            GenerateWindowCode(rootData, className);
            GenerateBindCode(rootData, className);

            UIAutoBindData[] subItems = rootData.GetComponentsInChildren<UIAutoBindData>(true);
            foreach (var subItem in subItems)
            {
                if (subItem == rootData) continue;
                if (subItem.EGenerateType == EGenerateType.SubItem)
                {
                    GenerateSubItemCode(subItem, rootData);
                }
            }

            AssetDatabase.Refresh();
        }

        private static void GenerateWindowCode(UIAutoBindData rootData, string className)
        {
            string folderPath = $"{OutputRoot}/{className}";
            CreateFolderHierarchy(folderPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Manager.UIManager;");
            sb.AppendLine();
            sb.AppendLine("namespace Game.UI");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {className} : UIWindow");
            sb.AppendLine("    {");
            sb.AppendLine($"        public override string WindowId => nameof({className});");
            sb.AppendLine($"        public override UILayer LayerId => UILayer.{rootData.UILayer};");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnAwake()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnAwake();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnShow(UIWindowData data)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnShow(data);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnHide()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnHide();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override void OnDestroy()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnDestroy();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string filePath = $"{folderPath}/{className}.cs";
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"Generated: {filePath}");
        }

        private static void GenerateBindCode(UIAutoBindData rootData, string className)
        {
            string folderPath = $"{OutputRoot}/{className}";
            CreateFolderHierarchy(folderPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using Manager.UIManager;");
            sb.AppendLine();
            sb.AppendLine("namespace Game.UI");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {className} : IAutoBindable");
            sb.AppendLine("    {");

            if (rootData.UGUINodeProviderInfos != null && rootData.UGUINodeProviderInfos.Count > 0)
            {
                foreach (var info in rootData.UGUINodeProviderInfos)
                {
                    string typeName = GetFullTypeName(info.typeName);
                    sb.AppendLine($"        [SerializeField] private {typeName} {info.name};");
                }
            }
            else
            {
                sb.AppendLine("        [SerializeField] private GameObject _gameObject;");
            }

            sb.AppendLine();
            sb.AppendLine("        public void AutoBind()");
            sb.AppendLine("        {");

            if (rootData.UGUINodeProviderInfos != null && rootData.UGUINodeProviderInfos.Count > 0)
            {
                foreach (var info in rootData.UGUINodeProviderInfos)
                {
                    string typeName = GetFullTypeName(info.typeName);
                    string path = GetNodePath(info);
                    sb.AppendLine($"            {info.name} = transform.Find(\"{path}\")?.GetComponent<{typeName}>();");
                }
            }
            else
            {
                sb.AppendLine("            _gameObject = transform.Find(gameObject.name)?.GetComponent<GameObject>();");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string filePath = $"{folderPath}/{className}.Bind.cs";
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"Generated: {filePath}");
        }

        private static void GenerateSubItemCode(UIAutoBindData subItem, UIAutoBindData rootData)
        {
            if (subItem == null || string.IsNullOrEmpty(subItem.ClassName)) return;

            string folderPath = $"{OutputRoot}/{rootData.ClassName}";
            CreateFolderHierarchy(folderPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine();
            sb.AppendLine("namespace Game.UI");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {rootData.ClassName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        [Serializable]");
            sb.AppendLine($"        public class {subItem.ClassName}");
            sb.AppendLine("        {");

            if (subItem.UGUINodeProviderInfos != null && subItem.UGUINodeProviderInfos.Count > 0)
            {
                foreach (var info in subItem.UGUINodeProviderInfos)
                {
                    string typeName = GetFullTypeName(info.typeName);
                    sb.AppendLine($"            [SerializeField] private {typeName} {info.name};");
                }
            }
            else
            {
                sb.AppendLine("            [SerializeField] private GameObject _gameObject;");
            }

            sb.AppendLine();
            sb.AppendLine($"            public void Bind(Transform parent)");
            sb.AppendLine("            {");

            if (subItem.UGUINodeProviderInfos != null && subItem.UGUINodeProviderInfos.Count > 0)
            {
                string subItemPath = subItem.ClassName.Substring(rootData.ClassName.Length + 1);
                foreach (var info in subItem.UGUINodeProviderInfos)
                {
                    string typeName = GetFullTypeName(info.typeName);
                    sb.AppendLine($"                {info.name} = parent.Find(\"{subItemPath}/{info.name}\")?.GetComponent<{typeName}>();");
                }
            }
            else
            {
                sb.AppendLine("                _gameObject = parent.Find(\"\")?.GetComponent<GameObject>();");
            }

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string filePath = $"{folderPath}/{subItem.ClassName}.cs";
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"Generated: {filePath}");
        }

        private static string GetNodePath(UGUINodeProviderInfo info)
        {
            if (info == null || info.obj == null) return "";

            Transform targetTransform = null;
            if (info.obj is Component comp)
            {
                targetTransform = comp.transform;
            }
            else if (info.obj is GameObject go)
            {
                targetTransform = go.transform;
            }

            if (targetTransform == null) return "";

            return EditorTools.GetTransformPath(targetTransform);
        }

        private static string GetFullTypeName(string typeName)
        {
            switch (typeName)
            {
                case "GameObject": return "GameObject";
                case "Transform": return "Transform";
                case "RectTransform": return "RectTransform";
                case "Image": return "UnityEngine.UI.Image";
                case "Text": return "UnityEngine.UI.Text";
                case "Button": return "UnityEngine.UI.Button";
                case "RawImage": return "UnityEngine.UI.RawImage";
                case "Toggle": return "UnityEngine.UI.Toggle";
                case "Slider": return "UnityEngine.UI.Slider";
                case "Scrollbar": return "UnityEngine.UI.Scrollbar";
                case "Dropdown": return "UnityEngine.UI.Dropdown";
                case "InputField": return "UnityEngine.UI.InputField";
                case "ScrollRect": return "UnityEngine.UI.ScrollRect";
                case "ToggleGroup": return "UnityEngine.UI.ToggleGroup";
                default: return typeName;
            }
        }

        private static void CreateFolderHierarchy(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(currentPath + "/" + parts[i]))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath += "/" + parts[i];
            }
        }
    }
}
#endif
