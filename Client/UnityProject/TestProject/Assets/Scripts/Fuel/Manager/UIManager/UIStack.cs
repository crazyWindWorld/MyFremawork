using System;
using System.Collections.Generic;

namespace Manager.UIManager
{
    public class UIStack
    {
        private readonly List<UIWindow> _stack = new List<UIWindow>();

        // Fix #10: O(1) Contains lookup, eliminates double O(n) traversal in OpenWindow/CloseWindow
        private readonly Dictionary<string, UIWindow> _windowMap = new Dictionary<string, UIWindow>();

        private Action<UIWindow> _onPop;
        private Action<UIWindow> _onClear;

        public int Count => _stack.Count;
        public UIWindow TopWindow => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        // Fix #8: expose bottom for overflow eviction
        public UIWindow BottomWindow => _stack.Count > 0 ? _stack[0] : null;

        public IReadOnlyList<UIWindow> Stack => _stack;

        public UIStack(Action<UIWindow> onPop = null, Action<UIWindow> onClear = null)
        {
            _onPop = onPop;
            _onClear = onClear;
        }

        public void SetCallbacks(Action<UIWindow> onPop, Action<UIWindow> onClear)
        {
            _onPop = onPop;
            _onClear = onClear;
        }

        public int FindIndex(string windowId)
        {
            return _stack.FindIndex(x => x.WindowId == windowId);
        }

        public int FindIndex(UIWindow window)
        {
            return FindIndex(window.WindowId);
        }

        // Fix #10: O(1) via Dictionary
        public bool Contains(string windowId)
        {
            return _windowMap.ContainsKey(windowId);
        }

        public bool Contains(UIWindow window)
        {
            return Contains(window.WindowId);
        }

        public int PopToIndex(int index)
        {
            int popCount = _stack.Count - index - 1;
            for (int i = 0; i < popCount; i++)
            {
                Pop();
            }
            return popCount;
        }

        public int PopToWindow(string windowId)
        {
            int index = FindIndex(windowId);
            if (index >= 0)
            {
                return PopToIndex(index);
            }
            return 0;
        }

        // Fix #2: removed layerCompare — clearing higher-layer windows when pushing a lower-layer
        // window is semantically wrong and destroys existing UI unintentionally.
        public void Push(UIWindow window)
        {
            int index = FindIndex(window.WindowId);
            if (index >= 0)
            {
                PopToIndex(index);
                return;
            }

            _stack.Add(window);
            _windowMap[window.WindowId] = window;
        }

        public UIWindow Pop()
        {
            if (_stack.Count == 0) return null;

            UIWindow window = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _windowMap.Remove(window.WindowId);
            _onPop?.Invoke(window);
            return window;
        }

        // Fix #8: evict the oldest (bottom) window during overflow, triggers OnRelease via _onClear
        public UIWindow PopBottom()
        {
            if (_stack.Count == 0) return null;

            UIWindow window = _stack[0];
            _stack.RemoveAt(0);
            _windowMap.Remove(window.WindowId);
            _onClear?.Invoke(window);
            return window;
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                UIWindow window = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                _windowMap.Remove(window.WindowId);
                _onClear?.Invoke(window);
            }
        }
    }
}
