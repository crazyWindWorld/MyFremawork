using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public enum EGenerateType
    {
        None = 0,
        Window = 1,
        SubItem = 2,
    }

    [Serializable]
    public class UGUINodeProviderInfo
    {
        public string name;
        public string typeName;
        public string parentClassName;
        public UnityEngine.Object obj;

        public UGUINodeProviderInfo()
        {
        }

        public UGUINodeProviderInfo(string name, string typeName, string parentClassName, UnityEngine.Object obj)
        {
            this.name = name;
            this.typeName = typeName;
            this.parentClassName = parentClassName;
            this.obj = obj;
        }
    }

    [Serializable]
    public class UGUINodeProviderMenuItemInfo
    {
        public UGUINodeProviderInfo UGUINodeProviderInfo;
        public string TypeName;

        public UGUINodeProviderMenuItemInfo()
        {
        }

        public UGUINodeProviderMenuItemInfo(UGUINodeProviderInfo info, string typeName)
        {
            UGUINodeProviderInfo = info;
            TypeName = typeName;
        }
    }

    public class UIAutoBindData : MonoBehaviour
    {
        [SerializeField]
        public EGenerateType EGenerateType = EGenerateType.None;

        [SerializeField]
        public string VariableName;

        [SerializeField]
        public string ClassName;

        [SerializeField]
        public UILayer UILayer = UILayer.Normal;

        [SerializeField]
        private List<UGUINodeProviderInfo> _uGUINodeProviderInfos = new List<UGUINodeProviderInfo>();

        public List<UGUINodeProviderInfo> UGUINodeProviderInfos => _uGUINodeProviderInfos;

        public void AddUGUINodeProviderInfo(UGUINodeProviderInfo info)
        {
            if (info == null) return;

            if (_uGUINodeProviderInfos == null)
                _uGUINodeProviderInfos = new List<UGUINodeProviderInfo>();

            if (!_uGUINodeProviderInfos.Exists(x => x.name == info.name && x.typeName == info.typeName))
            {
                _uGUINodeProviderInfos.Add(new UGUINodeProviderInfo
                {
                    name = info.name,
                    typeName = info.typeName,
                    parentClassName = info.parentClassName,
                    obj = info.obj
                });
            }
        }

        public void RemoveUGUINodeProviderInfo(UGUINodeProviderInfo info)
        {
            if (info == null) return;
            _uGUINodeProviderInfos?.RemoveAll(x => x.name == info.name && x.typeName == info.typeName);
        }

        public UGUINodeProviderInfo GetAdded(UGUINodeProviderInfo info)
        {
            if (info == null || _uGUINodeProviderInfos == null) return null;
            return _uGUINodeProviderInfos.Find(x => x.name == info.name && x.typeName == info.typeName);
        }

        public static string GetVariableName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            input = System.Text.RegularExpressions.Regex.Replace(input, @"[\s\-]+", "");
            input = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "");

            if (input.Length > 0 && char.IsDigit(input[0]))
            {
                input = "_" + input;
            }

            if (input.Length == 0)
                return "_unknown";

            char[] chars = input.ToCharArray();
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
    }
}
