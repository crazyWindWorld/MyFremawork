#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Config
{
    [Serializable]
    public class UIBindFolderConfig : ScriptableObject
    {
        private static UIBindFolderConfig _instance;
        public static UIBindFolderConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<UIBindFolderConfig>("Config/UIBindFolderConfig");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<UIBindFolderConfig>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField]
        private List<string> _uIBindFolders = new List<string>
        {
            "Assets/Resources/UIPrefabs",
            "Assets/Prefabs/UI",
            "Assets/UI/Prefabs",
            "Assets/Resources/UI"
        };

        public List<string> UIBindFolders => _uIBindFolders;
    }
}
#endif
