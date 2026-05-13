#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    public static class UIAutoBindDataUtil
    {
        public const string DatabaseRoot = "Assets/Resources/UIBindData";

        public static void SerializeToUIAutoBindDataScriptable(UIAutoBindData source, string prefabPath)
        {
            if (source == null || string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("SerializeToUIAutoBindDataScriptable: source or prefabPath is null");
                return;
            }

            if (!AssetDatabase.IsValidFolder(DatabaseRoot))
            {
                string[] parts = DatabaseRoot.Split('/');
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

            string className = string.IsNullOrEmpty(source.ClassName) ? source.name : source.ClassName;
            string assetPath = $"{DatabaseRoot}/{className}.asset";

            UIAutoBindDataScriptable scriptable = AssetDatabase.LoadAssetAtPath<UIAutoBindDataScriptable>(assetPath);
            if (scriptable == null)
            {
                scriptable = ScriptableObject.CreateInstance<UIAutoBindDataScriptable>();
                AssetDatabase.CreateAsset(scriptable, assetPath);
            }

            scriptable.Clear();
            scriptable.Owner = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            scriptable.ClassName = className;
            scriptable.PrefabPath = prefabPath;
            scriptable.EGenerateType = source.EGenerateType;
            scriptable.UILayer = source.UILayer;

            if (source.UGUINodeProviderInfos != null)
            {
                foreach (var info in source.UGUINodeProviderInfos)
                {
                    scriptable.UGUINodeProviderInfos.Add(new UGUINodeProviderInfo
                    {
                        name = info.name,
                        typeName = info.typeName,
                        parentClassName = info.parentClassName,
                        obj = info.obj
                    });
                }
            }

            if (source.EGenerateType == EGenerateType.Window)
            {
                UIAutoBindData[] subItems = source.GetComponentsInChildren<UIAutoBindData>(true);
                foreach (var subItem in subItems)
                {
                    if (subItem == source) continue;
                    if (subItem.EGenerateType == EGenerateType.SubItem)
                    {
                        SerializeSubItemToScriptable(scriptable, subItem);
                    }
                }
            }

            EditorUtility.SetDirty(scriptable);
            AssetDatabase.SaveAssets();
        }

        private static void SerializeSubItemToScriptable(UIAutoBindDataScriptable parent, UIAutoBindData subItem)
        {
            UIAutoBindDataScriptable subScriptable = ScriptableObject.CreateInstance<UIAutoBindDataScriptable>();

            subScriptable.Owner = null;
            subScriptable.ClassName = subItem.ClassName;
            subScriptable.PrefabPath = "";
            subScriptable.EGenerateType = subItem.EGenerateType;
            subScriptable.UILayer = subItem.UILayer;

            if (subItem.UGUINodeProviderInfos != null)
            {
                foreach (var info in subItem.UGUINodeProviderInfos)
                {
                    subScriptable.UGUINodeProviderInfos.Add(new UGUINodeProviderInfo
                    {
                        name = info.name,
                        typeName = info.typeName,
                        parentClassName = info.parentClassName,
                        obj = info.obj
                    });
                }
            }

            parent.SubItems.Add(subScriptable);
        }

        public static void DeserializeToUIAutoBindData(UIAutoBindDataScriptable scriptable, string prefabPath)
        {
            if (scriptable == null)
            {
                Debug.LogError("DeserializeToUIAutoBindData: scriptable is null");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"DeserializeToUIAutoBindData: prefab not found at {prefabPath}");
                return;
            }

            UIAutoBindData rootData = prefab.GetComponent<UIAutoBindData>();
            if (rootData == null)
            {
                rootData = prefab.AddComponent<UIAutoBindData>();
            }

            rootData.EGenerateType = scriptable.EGenerateType;
            rootData.ClassName = scriptable.ClassName;
            rootData.UILayer = scriptable.UILayer;
            rootData.UGUINodeProviderInfos.Clear();

            foreach (var info in scriptable.UGUINodeProviderInfos)
            {
                rootData.UGUINodeProviderInfos.Add(new UGUINodeProviderInfo
                {
                    name = info.name,
                    typeName = info.typeName,
                    parentClassName = info.parentClassName,
                    obj = info.obj
                });
            }

            foreach (var subScriptable in scriptable.SubItems)
            {
                DeserializeSubItemToUIAutoBindData(prefab.transform, rootData, subScriptable);
            }

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        private static void DeserializeSubItemToUIAutoBindData(Transform parent, UIAutoBindData rootData, UIAutoBindDataScriptable subScriptable)
        {
            string[] pathParts = subScriptable.ClassName.Split('_');
            if (pathParts.Length < 2) return;

            string subItemPath = subScriptable.ClassName.Substring(rootData.ClassName.Length + 1);
            Transform target = parent.Find(subItemPath);

            if (target == null)
            {
                Debug.LogWarning($"DeserializeToUIAutoBindData: sub item not found at path {subItemPath}");
                return;
            }

            UIAutoBindData subData = target.GetComponent<UIAutoBindData>();
            if (subData == null)
            {
                subData = target.gameObject.AddComponent<UIAutoBindData>();
            }

            subData.EGenerateType = subScriptable.EGenerateType;
            subData.ClassName = subScriptable.ClassName;
            subData.UILayer = subScriptable.UILayer;
            subData.UGUINodeProviderInfos.Clear();

            foreach (var info in subScriptable.UGUINodeProviderInfos)
            {
                subData.UGUINodeProviderInfos.Add(new UGUINodeProviderInfo
                {
                    name = info.name,
                    typeName = info.typeName,
                    parentClassName = info.parentClassName,
                    obj = info.obj
                });
            }
        }

        public static UIAutoBindDataScriptable LoadScriptable(string className)
        {
            string assetPath = $"{DatabaseRoot}/{className}.asset";
            return AssetDatabase.LoadAssetAtPath<UIAutoBindDataScriptable>(assetPath);
        }

        public static List<UIAutoBindDataScriptable> LoadAllScriptables()
        {
            List<UIAutoBindDataScriptable> scriptables = new List<UIAutoBindDataScriptable>();
            string[] guids = AssetDatabase.FindAssets("t:UIAutoBindDataScriptable", new[] { DatabaseRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UIAutoBindDataScriptable scriptable = AssetDatabase.LoadAssetAtPath<UIAutoBindDataScriptable>(path);
                if (scriptable != null)
                {
                    scriptables.Add(scriptable);
                }
            }

            return scriptables;
        }
    }
}
#endif
