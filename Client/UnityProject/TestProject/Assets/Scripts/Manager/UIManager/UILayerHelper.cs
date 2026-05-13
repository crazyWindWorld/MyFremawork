using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public static class UILayerHelper
    {
        private const int Z_SPACING = 1000;
        private static readonly Dictionary<UILayer, Transform> _layerRoots = new Dictionary<UILayer, Transform>();

        public static int GetZ(UILayer layer)
        {
            return (int)layer * Z_SPACING;
        }

        public static string GetLayerName(UILayer layer)
        {
            return layer.ToString();
        }

        public static void RegisterLayerRoot(UILayer layer, Transform root)
        {
            if (!_layerRoots.ContainsKey(layer))
            {
                _layerRoots.Add(layer, root);
            }
            else
            {
                _layerRoots[layer] = root;
            }
        }

        public static Transform GetLayerRoot(UILayer layer)
        {
            return _layerRoots.TryGetValue(layer, out var root) ? root : null;
        }

        public static bool IsHigherLayer(UILayer layer1, UILayer layer2)
        {
            return (int)layer1 > (int)layer2;
        }
    }
}
