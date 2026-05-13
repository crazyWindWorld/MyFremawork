using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public static class UIAutoBindHelper
    {
        public static void BindAll(UIWindow window)
        {
            if (window == null || window.ViewObject == null) return;

            var binds = window.ViewObject.GetComponentsInChildren<UIAutoBind>(true);
            foreach (var bind in binds)
            {
                if (bind != null)
                {
                    bind.AutoBind();
                }
            }
        }
    }
}
