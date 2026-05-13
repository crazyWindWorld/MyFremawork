using System;
using System.Collections.Generic;
using Fuel.Singleton;
using UnityEngine;
using UI = UnityEngine.UI;

namespace Manager.UIManager
{
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField]
        private Camera _uiCamera;

        public Camera UICamera => _uiCamera;

        [SerializeField]
        private Vector2 _referenceResolution = new Vector2(1920, 1080);

        public Vector2 ReferenceResolution
        {
            get => _referenceResolution;
            set
            {
                _referenceResolution = value;
                UpdateCanvasScaler();
            }
        }

        private Dictionary<UILayer, Canvas> _layerCanvases = new Dictionary<UILayer, Canvas>();
        private Dictionary<UILayer, Transform> _layerRoots = new Dictionary<UILayer, Transform>();
        private Dictionary<UILayer, UI.CanvasScaler> _layerScalers = new Dictionary<UILayer, UI.CanvasScaler>();
        private UIStack _stack;
        private UIResourceManager _resourceManager;
        private Dictionary<string, Func<UIWindowData, UIWindow>> _windowFactory = new Dictionary<string, Func<UIWindowData, UIWindow>>();

        private int _maxStackCount = 10;

        public int MaxStackCount
        {
            get => _maxStackCount;
            set => _maxStackCount = Mathf.Max(1, value);
        }

        public UIStack Stack => _stack;
        public UIResourceManager ResourceManager => _resourceManager;

        public event Action<UIWindow> OnWindowShow;
        public event Action<UIWindow> OnWindowHide;

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        protected override void OnInit()
        {
            base.OnInit();

            _resourceManager = new UIResourceManager();
            _stack = new UIStack(OnWindowPop, OnWindowClear);
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            CreateUICamera();
            CreateLayerCanvases();

            Debug.Log("[UIManager] Initialized successfully");
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                UpdateCanvasScaler();
            }
        }

        private void CreateUICamera()
        {
            if (_uiCamera != null) return;

            var cameraObj = new GameObject("UICamera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = Vector3.back * 1000;
            _uiCamera = cameraObj.AddComponent<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.Depth;
            _uiCamera.cullingMask = ~0;
            _uiCamera.depth = -1;
            _uiCamera.orthographic = true;
            _uiCamera.orthographicSize = 1;
            _uiCamera.nearClipPlane = 1;
            _uiCamera.farClipPlane = 2000;
            _uiCamera.useOcclusionCulling = false;
        }

        private void CreateLayerCanvases()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerObj = new GameObject(layer.ToString());
                layerObj.transform.SetParent(transform);

                var canvas = layerObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _uiCamera;
                canvas.planeDistance = 100;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = (int)layer * 100;

                var scaler = layerObj.AddComponent<UI.CanvasScaler>();
                scaler.uiScaleMode = UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = _referenceResolution;
                scaler.screenMatchMode = UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = Screen.width > Screen.height ? 0f : 1f;

                layerObj.AddComponent<UI.GraphicRaycaster>();

                var rect = layerObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                _layerCanvases[layer] = canvas;
                _layerScalers[layer] = scaler;
                _layerRoots[layer] = layerObj.transform;
                UILayerHelper.RegisterLayerRoot(layer, layerObj.transform);
            }
        }

        private void UpdateCanvasScaler()
        {
            bool isLandscape = Screen.width > Screen.height;
            float matchValue = isLandscape ? 0f : 1f;

            foreach (var kvp in _layerScalers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.referenceResolution = _referenceResolution;
                    kvp.Value.matchWidthOrHeight = matchValue;
                }
            }
        }

        public void RegisterWindowFactory(string windowId, Func<UIWindowData, UIWindow> factory)
        {
            if (!_windowFactory.ContainsKey(windowId))
            {
                _windowFactory.Add(windowId, factory);
            }
            else
            {
                _windowFactory[windowId] = factory;
            }
        }

        public void RegisterWindowPrefab(string windowId, string prefabPath)
        {
            _resourceManager.RegisterPrefabPath(windowId, prefabPath);
        }

        public T OpenWindow<T>(UIWindowData data = null) where T : UIWindow
        {
            string windowId = typeof(T).Name;
            return OpenWindow(windowId, data) as T;
        }

        public UIWindow OpenWindow(string windowId, UIWindowData data = null)
        {
            UIWindow window = _resourceManager.GetWindow(windowId);

            if (window == null)
            {
                if (!_windowFactory.TryGetValue(windowId, out var factory))
                {
                    Debug.LogError($"[UIManager] No factory registered for window: {windowId}");
                    return null;
                }

                window = factory(data);
                _resourceManager.RegisterWindow(windowId, window);

                var layerRoot = GetLayerRoot(window.LayerId);
                var viewObj = _resourceManager.CreateInstance(windowId, layerRoot);
                window.ViewObject = viewObj;
                if (viewObj != null)
                {
                    viewObj.SetActive(false);
                    window.OnAwake();
                }
            }

            if (_stack.Contains(windowId))
            {
                _stack.PushToWindow(windowId);
                var topWindow = _stack.TopWindow;
                if (topWindow != null)
                {
                    topWindow.OnShow(data);
                    OnWindowShow?.Invoke(topWindow);
                }
                return topWindow;
            }

            HandleStackOverflow();

            bool layerCompare(UILayer topLayerId, UILayer newLayerId)
            {
                return UILayerHelper.IsHigherLayer(topLayerId, newLayerId);
            }

            _stack.Push(window, layerCompare);
            window.OnShow(data);
            OnWindowShow?.Invoke(window);
            return window;
        }

        public void CloseWindow(string windowId)
        {
            if (!_stack.Contains(windowId)) return;

            int index = _stack.FindIndex(windowId);
            _stack.PopToIndex(index);

            for (int i = _stack.Count; i <= index; i++)
            {
                _stack.Pop();
            }
        }

        public void CloseTopWindow()
        {
            _stack.Pop();
        }

        private void HandleStackOverflow()
        {
            while (_stack.Count >= _maxStackCount)
            {
                var window = _stack.TopWindow;
                if (window != null)
                {
                    window.OnRelease();
                }
                _stack.Pop();
            }
        }

        private void OnWindowPop(UIWindow window)
        {
            window.OnHide();
            OnWindowHide?.Invoke(window);
        }

        private void OnWindowClear(UIWindow window)
        {
            window.OnRelease();
        }

        public Transform GetLayerRoot(UILayer layer)
        {
            return _layerRoots.TryGetValue(layer, out var root) ? root : null;
        }

        public Transform GetLayerRoot(int layerId)
        {
            return GetLayerRoot((UILayer)layerId);
        }

        public Canvas GetLayerCanvas(UILayer layer)
        {
            return _layerCanvases.TryGetValue(layer, out var canvas) ? canvas : null;
        }

        public UIWindow ReloadWindow(string windowId)
        {
            var window = _resourceManager.GetWindow(windowId);
            if (window == null) return null;

            var layerRoot = GetLayerRoot(window.LayerId);
            var viewObj = _resourceManager.CreateInstance(windowId, layerRoot);
            window.ViewObject = viewObj;
            window.OnReload();

            if (window.IsShow)
            {
                viewObj?.SetActive(true);
            }
            else
            {
                viewObj?.SetActive(false);
            }

            return window;
        }
    }

    public static class UIStackExtensions
    {
        public static void PushToWindow(this UIStack stack, string windowId)
        {
            if (stack == null) return;
            int index = stack.FindIndex(windowId);
            if (index >= 0)
            {
                stack.PopToIndex(index);
            }
        }
    }
}
