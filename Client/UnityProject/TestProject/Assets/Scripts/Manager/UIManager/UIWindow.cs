using System;
using UnityEngine;

namespace Manager.UIManager
{
    public abstract class UIWindow
    {
        public abstract string WindowId { get; }
        public abstract UILayer LayerId { get; }
        public string WindowName { get; set; }
        public GameObject ViewObject { get; set; }
        public bool IsShow { get; private set; }
        public bool IsRelease { get; private set; }

        public event Action<UIWindow> OnShowEvent;
        public event Action<UIWindow> OnHideEvent;
        public event Action<UIWindow> OnDestroyEvent;

        public virtual void OnAwake()
        {
            InitNodeProvider();
        }

        /// <summary>
        /// Override in the generated window (via UIWindow&lt;TProvider&gt;) to pull the
        /// NodeProvider component from ViewObject. Called automatically by OnAwake().
        /// </summary>
        protected virtual void InitNodeProvider() { }

        // Fix #4: guard against calling Show/Hide after Release
        public virtual void OnShow(UIWindowData data = null)
        {
            if (IsRelease) return;

            IsShow = true;
            if (ViewObject != null)
            {
                ViewObject.SetActive(true);
            }
            RegisterEvents();
            OnShowEvent?.Invoke(this);
        }

        // Fix #4: guard against calling Hide after Release
        public virtual void OnHide()
        {
            if (IsRelease) return;

            IsShow = false;
            if (ViewObject != null)
            {
                ViewObject.SetActive(false);
            }
            UnregisterEvents();
            OnHideEvent?.Invoke(this);
        }

        public virtual void OnRelease()
        {
            IsRelease = true;
            IsShow = false;
            if (ViewObject != null)
            {
                UnityEngine.Object.Destroy(ViewObject);
                ViewObject = null;
            }
            OnDestroyEvent?.Invoke(this);
        }

        public virtual void OnReload()
        {
            IsRelease = false;
        }

        public virtual void RegisterEvents() { }
        public virtual void UnregisterEvents() { }
    }

    /// <summary>
    /// Strongly-typed UIWindow that knows its NodeProvider type.
    /// Generated windows inherit from UIWindow&lt;TProvider&gt; so that
    /// <see cref="Nodes"/> is available as the correct type without casting.
    /// </summary>
    public abstract class UIWindow<TProvider> : UIWindow where TProvider : UINodeProvider
    {
        public TProvider Nodes { get; private set; }

        protected override void InitNodeProvider()
        {
            if (ViewObject == null) return;
            Nodes = ViewObject.GetComponent<TProvider>();
            if (Nodes == null)
                Debug.LogWarning($"[UIWindow] NodeProvider '{typeof(TProvider).Name}' not found on '{ViewObject.name}'.");
        }
    }
}
