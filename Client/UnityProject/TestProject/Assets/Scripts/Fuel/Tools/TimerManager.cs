using System;
using System.Collections.Generic;
namespace Fuel.Tools
{
    public class TimerManager
    {
        private class TimerTask
        {
            public int Id;

            /// <summary>
            /// 间隔时间
            /// </summary>
            public float Interval;

            /// <summary>
            /// 当前累计时间
            /// </summary>
            public float Elapsed;

            /// <summary>
            /// 回调方法
            /// </summary>
            public Action Callback;

            /// <summary>
            /// 总执行次数，-1 表示无限次
            /// </summary>
            public int RepeatCount;

            /// <summary>
            /// 已执行次数
            /// </summary>
            public int ExecutedCount;

            /// <summary>
            /// 是否已取消
            /// </summary>
            public bool Cancelled;
        }

        private readonly Dictionary<int, TimerTask> _timers = new Dictionary<int, TimerTask>();

        private readonly List<int> _removeList = new List<int>();

        private int _timerId = 0;

        /// <summary>
        /// 创建一个定时器
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <param name="interval">间隔时间，单位秒</param>
        /// <param name="repeatCount">
        /// 执行次数：
        /// 1 表示执行一次；
        /// 大于 1 表示执行指定次数；
        /// -1 表示无限执行
        /// </param>
        /// <returns>定时器ID</returns>
        public int AddTimer(Action callback, float interval, int repeatCount = 1)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (interval <= 0)
                throw new ArgumentException("interval 必须大于 0");

            if (repeatCount == 0 || repeatCount < -1)
                throw new ArgumentException("repeatCount 必须为 -1 或大于 0");

            int id = ++_timerId;

            TimerTask task = new TimerTask
            {
                Id = id,
                Interval = interval,
                Elapsed = 0f,
                Callback = callback,
                RepeatCount = repeatCount,
                ExecutedCount = 0,
                Cancelled = false
            };

            _timers.Add(id, task);

            return id;
        }

        /// <summary>
        /// 延迟多少秒后执行一次
        /// </summary>
        public int Delay(Action callback, float delaySeconds)
        {
            return AddTimer(callback, delaySeconds, 1);
        }

        /// <summary>
        /// 每隔一段时间执行一次
        /// </summary>
        /// <param name="callback">回调</param>
        /// <param name="interval">间隔秒数</param>
        /// <param name="repeatCount">执行次数，-1 表示无限次</param>
        public int Repeat(Action callback, float interval, int repeatCount = -1)
        {
            return AddTimer(callback, interval, repeatCount);
        }

        /// <summary>
        /// 每秒执行一次，无限执行
        /// </summary>
        public int EverySecond(Action callback)
        {
            return Repeat(callback, 1f, -1);
        }

        /// <summary>
        /// 每秒执行一次，执行指定次数
        /// </summary>
        public int EverySecond(Action callback, int repeatCount)
        {
            return Repeat(callback, 1f, repeatCount);
        }

        /// <summary>
        /// 取消指定定时器
        /// </summary>
        public void RemoveTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerTask task))
            {
                task.Cancelled = true;
            }
        }

        /// <summary>
        /// 清理所有定时器
        /// </summary>
        public void ClearAll()
        {
            _timers.Clear();
            _removeList.Clear();
        }

        /// <summary>
        /// Update 驱动，所有时间累计都在这里
        /// </summary>
        /// <param name="deltaTime">每帧间隔时间，单位秒</param>
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0)
                return;

            _removeList.Clear();

            foreach (var pair in _timers)
            {
                TimerTask task = pair.Value;

                if (task.Cancelled)
                {
                    _removeList.Add(task.Id);
                    continue;
                }

                task.Elapsed += deltaTime;

                if (task.Elapsed >= task.Interval)
                {
                    // 防止 deltaTime 过大时丢失周期
                    while (task.Elapsed >= task.Interval)
                    {
                        task.Elapsed -= task.Interval;

                        if (task.Cancelled)
                            break;

                        task.Callback?.Invoke();

                        task.ExecutedCount++;

                        // 非无限次数，并且达到执行次数
                        if (task.RepeatCount != -1 && task.ExecutedCount >= task.RepeatCount)
                        {
                            task.Cancelled = true;
                            break;
                        }
                    }
                }

                if (task.Cancelled)
                {
                    _removeList.Add(task.Id);
                }
            }

            for (int i = 0; i < _removeList.Count; i++)
            {
                _timers.Remove(_removeList[i]);
            }
        }
    }
}
