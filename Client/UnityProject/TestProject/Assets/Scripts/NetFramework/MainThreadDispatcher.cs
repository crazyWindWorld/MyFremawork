using System;
using System.Collections.Concurrent;
using Singleton;
using UnityEngine;

namespace NetFramework
{
    public class MainThreadDispatcher : MonoSingleton<MainThreadDispatcher>
    {
        private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        public void Enqueue(Action action)
        {
            _actions.Enqueue(action);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
