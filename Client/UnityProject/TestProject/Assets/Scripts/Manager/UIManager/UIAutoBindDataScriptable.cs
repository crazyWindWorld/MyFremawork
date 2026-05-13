using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager.Editor
{
    public enum NamingConvention
    {
        PascalCase,
        CamelCase,
        Unchanged,
    }

    [Serializable]
    public class UIAutoBindDataScriptable : ScriptableObject
    {
        [SerializeField]
        public GameObject Owner;

        [SerializeField]
        public string ClassName;

        [SerializeField]
        public string PrefabPath;

        [SerializeField]
        public EGenerateType EGenerateType;

        [SerializeField]
        public UILayer UILayer;

        [SerializeField]
        public List<UGUINodeProviderInfo> UGUINodeProviderInfos = new List<UGUINodeProviderInfo>();

        [SerializeField]
        public List<UIAutoBindDataScriptable> SubItems = new List<UIAutoBindDataScriptable>();

        public void Clear()
        {
            Owner = null;
            ClassName = string.Empty;
            PrefabPath = string.Empty;
            EGenerateType = EGenerateType.None;
            UILayer = UILayer.Normal;
            UGUINodeProviderInfos.Clear();
            SubItems.Clear();
        }
    }
}
