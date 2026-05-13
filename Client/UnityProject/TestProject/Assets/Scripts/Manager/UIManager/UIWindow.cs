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
            AutoBindComponents();
        }

        private void AutoBindComponents()
        {
            if (ViewObject == null) return;

            var components = ViewObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp is IAutoBindable bindable)
                {
                    bindable.AutoBind();
                }
            }
        }

        public virtual void OnShow(UIWindowData data = null)
        {
            IsShow = true;
            if (ViewObject != null)
            {
                ViewObject.SetActive(true);
            }
            RegisterEvents();
            OnShowEvent?.Invoke(this);
        }

        public virtual void OnHide()
        {
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

    public interface IAutoBindable
    {
        void AutoBind();
    }
}
