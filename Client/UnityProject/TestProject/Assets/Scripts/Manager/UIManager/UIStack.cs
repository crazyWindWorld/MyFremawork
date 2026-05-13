using System;
using System.Collections.Generic;

namespace Manager.UIManager
{
    public class UIStack
    {
        private List<UIWindow> _stack = new List<UIWindow>();
        private Action<UIWindow> _onPop;
        private Action<UIWindow> _onClear;

        public int Count => _stack.Count;
        public UIWindow TopWindow => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
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
            return _stack.FindIndex(x => x.WindowId == window.WindowId);
        }

        public bool Contains(string windowId)
        {
            return _stack.Exists(x => x.WindowId == windowId);
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

        public void Push(UIWindow window, Func<UILayer, UILayer, bool> layerCompare = null)
        {
            if (_stack.Count > 0 && layerCompare != null)
            {
                var topWindow = TopWindow;
                if (layerCompare(topWindow.LayerId, window.LayerId))
                {
                    Clear();
                }
            }

            int index = FindIndex(window.WindowId);
            if (index >= 0)
            {
                PopToIndex(index);
                return;
            }

            _stack.Add(window);
        }

        public UIWindow Pop()
        {
            if (_stack.Count == 0) return null;

            UIWindow window = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _onPop?.Invoke(window);
            return window;
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                UIWindow window = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                _onClear?.Invoke(window);
            }
        }
    }
}
