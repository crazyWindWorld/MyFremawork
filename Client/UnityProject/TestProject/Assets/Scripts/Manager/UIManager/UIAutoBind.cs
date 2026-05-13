using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public abstract class UIAutoBind : MonoBehaviour
    {
        public abstract void AutoBind();
        public abstract List<string> GetBindNames();
    }

    public abstract class UIAutoBind<T> : UIAutoBind where T : Component
    {
        public T Target => _target;
        [SerializeField] protected T _target;

        public override void AutoBind()
        {
            GetComponentSelf();
        }

        public override List<string> GetBindNames()
        {
            return new List<string>();
        }

        protected virtual void GetComponentSelf()
        {
            if (_target == null)
            {
                _target = GetComponent<T>();
            }
        }
    }
}
