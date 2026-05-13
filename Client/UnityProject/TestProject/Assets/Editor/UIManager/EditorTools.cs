#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    public static class EditorTools
    {
        private static readonly Dictionary<Type, string> TypeAliasMap = new Dictionary<Type, string>
        {
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(string), "string" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(short), "short" },
            { typeof(long), "long" },
            { typeof(double), "double" },
            { typeof(char), "char" },
            { typeof(object), "object" },
            { typeof(void), "void" },
        };

        public static string GetVariableName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string result = name;

            result = Regex.Replace(result, @"[\s\-]+", "");
            result = Regex.Replace(result, @"[^a-zA-Z0-9_]", "");

            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            if (result.Length == 0)
                return "_unknown";

            char[] chars = result.ToCharArray();
            if (char.IsLower(chars[0]))
            {
                chars[0] = char.ToUpper(chars[0]);
            }

            for (int i = 1; i < chars.Length; i++)
            {
                if (chars[i] == '_' && i + 1 < chars.Length)
                {
                    chars[i + 1] = char.ToUpper(chars[i + 1]);
                }
            }

            return new string(chars).Replace("_", "");
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (type == null)
                return "null";

            if (TypeAliasMap.TryGetValue(type, out string alias))
                return alias;

            if (type.IsArray)
            {
                string elementType = GetTypeDisplayName(type.GetElementType());
                return $"{elementType}[]";
            }

            if (type.IsGenericType)
            {
                string baseName = type.Name.Split('`')[0];
                Type[] genericArgs = type.GetGenericArguments();
                string[] argNames = new string[genericArgs.Length];
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    argNames[i] = GetTypeDisplayName(genericArgs[i]);
                }
                return $"{baseName}<{string.Join(", ", argNames)}>";
            }

            return type.Name;
        }

        public static T FindComponentInParent<T>(Transform transform, bool includeSelf = true) where T : Component
        {
            if (transform == null) return null;

            if (includeSelf)
            {
                T component = transform.GetComponent<T>();
                if (component != null)
                    return component;
            }

            Transform parent = transform.parent;
            while (parent != null)
            {
                T component = parent.GetComponent<T>();
                if (component != null)
                    return component;
                parent = parent.parent;
            }

            return null;
        }

        public static T[] GetAllComponentsInPrefab<T>(GameObject root) where T : Component
        {
            if (root == null) return new T[0];
            return root.GetComponentsInChildren<T>(true);
        }

        public static string GetTransformPath(Transform transform, Transform relativeTo = null)
        {
            if (transform == null) return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null && parent != relativeTo)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public static string GetValidFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Untitled";

            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }
}
#endif
