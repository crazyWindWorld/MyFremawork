using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public static class UILayerHelper
    {
        private const int Z_SPACING = 1000;

        public static int GetZ(UILayer layer)
        {
            return (int)layer * Z_SPACING;
        }

        public static string GetLayerName(UILayer layer)
        {
            return layer.ToString();
        }

        // Fix #5: removed static _layerRoots dictionary, RegisterLayerRoot, and GetLayerRoot.
        // That static dictionary duplicated UIManager._layerRoots and caused dangling Transform
        // references after scene reload because static fields are never cleared between scenes.
        // Use UIManager.Instance.GetLayerRoot() for runtime layer root lookups.

        public static bool IsHigherLayer(UILayer layer1, UILayer layer2)
        {
            return (int)layer1 > (int)layer2;
        }
    }
}
