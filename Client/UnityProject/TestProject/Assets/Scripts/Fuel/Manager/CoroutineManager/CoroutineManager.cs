using System;
using System.Collections;
using System.Collections.Generic;
using Fuel.Singleton;
using UnityEngine;
namespace Fuel.Manager.CoroutineManager
{
    public class CoroutineManager : MonoSingleton<CoroutineManager>
    {
        /// <summary>
        /// 用于按 key 管理协程
        /// </summary>
        private readonly Dictionary<string, Coroutine> _coroutineDict = new Dictionary<string, Coroutine>();

        /// <summary>
        /// 启动一个普通协程
        /// </summary>
        public Coroutine StartRoutine(IEnumerator routine)
        {
            if (routine == null)
            {
                Debug.LogWarning("CoroutineManager.StartRoutine: routine is null");
                return null;
            }

            return StartCoroutine(routine);
        }

        /// <summary>
        /// 停止一个普通协程
        /// </summary>
        public void StopRoutine(Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止所有由该 MonoBehaviour 启动的协程
        /// </summary>
        public void StopAllRoutines()
        {
            StopAllCoroutines();
            _coroutineDict.Clear();
        }

        /// <summary>
        /// 使用 key 启动协程。
        /// 如果 key 已存在，会先停止旧协程，再启动新协程。
        /// </summary>
        public Coroutine StartRoutineByKey(string key, IEnumerator routine)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("CoroutineManager.StartRoutineByKey: key is null or empty");
                return null;
            }

            if (routine == null)
            {
                Debug.LogWarning("CoroutineManager.StartRoutineByKey: routine is null");
                return null;
            }

            StopRoutineByKey(key);

            Coroutine coroutine = StartCoroutine(RunWithKey(key, routine));
            _coroutineDict[key] = coroutine;

            return coroutine;
        }

        /// <summary>
        /// 根据 key 停止协程
        /// </summary>
        public void StopRoutineByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_coroutineDict.TryGetValue(key, out Coroutine coroutine))
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }

                _coroutineDict.Remove(key);
            }
        }

        /// <summary>
        /// 判断某个 key 的协程是否正在运行
        /// </summary>
        public bool IsRoutineRunning(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _coroutineDict.ContainsKey(key);
        }

        /// <summary>
        /// 协程自然结束后，自动从字典中移除
        /// </summary>
        private IEnumerator RunWithKey(string key, IEnumerator routine)
        {
            yield return routine;

            if (_coroutineDict.ContainsKey(key))
            {
                _coroutineDict.Remove(key);
            }
        }

        /// <summary>
        /// 延迟执行，受 Time.timeScale 影响
        /// </summary>
        public Coroutine Delay(float seconds, Action callback)
        {
            return StartRoutine(DelayCoroutine(seconds, callback));
        }

        private IEnumerator DelayCoroutine(float seconds, Action callback)
        {
            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }

        /// <summary>
        /// 延迟执行，不受 Time.timeScale 影响
        /// </summary>
        public Coroutine DelayRealtime(float seconds, Action callback)
        {
            return StartRoutine(DelayRealtimeCoroutine(seconds, callback));
        }

        private IEnumerator DelayRealtimeCoroutine(float seconds, Action callback)
        {
            yield return new WaitForSecondsRealtime(seconds);
            callback?.Invoke();
        }

        /// <summary>
        /// 下一帧执行
        /// </summary>
        public Coroutine NextFrame(Action callback)
        {
            return StartRoutine(NextFrameCoroutine(callback));
        }

        private IEnumerator NextFrameCoroutine(Action callback)
        {
            yield return null;
            callback?.Invoke();
        }

        /// <summary>
        /// 组件销毁时清理 key 管理数据
        /// </summary>
        protected override void OnDestroy()
        {
            _coroutineDict.Clear();
        }
    }
}
