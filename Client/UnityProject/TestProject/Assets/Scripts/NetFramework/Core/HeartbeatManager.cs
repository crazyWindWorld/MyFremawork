using System;
using UnityEngine;

namespace NetFramework.Core
{
    /// <summary>
    /// 心跳管理器
    /// 负责定时发送PING、接收PONG、检测超时、超时重连
    /// 不直接依赖Proto类型，通过回调与外部交互
    /// </summary>
    public class HeartbeatManager
    {
        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public float Interval { get; set; } = 5f;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public float Timeout { get; set; } = 10f;

        /// <summary>
        /// 最大重连次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 当前重连次数
        /// </summary>
        public int CurrentRetryCount { get; private set; }

        /// <summary>
        /// 心跳超时事件（触发重连）
        /// </summary>
        public event Action OnHeartbeatTimeout;

        /// <summary>
        /// 超过最大重连次数事件（触发断开连接）
        /// </summary>
        public event Action OnMaxRetryExceeded;

        /// <summary>
        /// 收到PONG事件（参数: 延迟毫秒）
        /// </summary>
        public event Action<long> OnPongReceived;

        /// <summary>
        /// 需要发送PING时触发（参数: 客户端时间戳）
        /// </summary>
        public event Action<long> OnSendPing;

        private float _lastSendTime;
        private float _lastReceiveTime;
        private bool _waitingPong;
        private long _pingTimestamp;

        /// <summary>
        /// 启动心跳
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            _waitingPong = false;
            CurrentRetryCount = 0;
            _lastSendTime = 0f;
            _lastReceiveTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 停止心跳
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            _waitingPong = false;
        }

        /// <summary>
        /// 重置重连计数（重连成功后调用）
        /// </summary>
        public void ResetRetryCount()
        {
            CurrentRetryCount = 0;
        }

        /// <summary>
        /// 每帧调用，驱动心跳逻辑
        /// </summary>
        public void Tick()
        {
            if (!IsRunning)
                return;

            float currentTime = Time.realtimeSinceStartup;

            // 检查是否超时
            if (_waitingPong)
            {
                if (currentTime - _lastSendTime >= Timeout)
                {
                    HandleTimeout();
                    return;
                }
            }

            // 检查是否需要发送PING
            if (!_waitingPong && currentTime - _lastSendTime >= Interval)
            {
                SendPing();
            }
        }

        /// <summary>
        /// 处理超时
        /// </summary>
        private void HandleTimeout()
        {
            _waitingPong = false;
            CurrentRetryCount++;

            if (CurrentRetryCount >= MaxRetryCount)
            {
                Debug.LogError($"[HeartbeatManager] Max retry count ({MaxRetryCount}) exceeded, disconnecting...");
                OnMaxRetryExceeded?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[HeartbeatManager] Heartbeat timeout! Retry {CurrentRetryCount}/{MaxRetryCount}");
                OnHeartbeatTimeout?.Invoke();
            }
        }

        /// <summary>
        /// 发送PING
        /// </summary>
        private void SendPing()
        {
            _pingTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _waitingPong = true;
            _lastSendTime = Time.realtimeSinceStartup;

            OnSendPing?.Invoke(_pingTimestamp);
        }

        /// <summary>
        /// 处理PONG响应
        /// </summary>
        /// <param name="clientTime">客户端时间戳</param>
        /// <param name="serverTime">服务器时间戳</param>
        public void HandlePong(long clientTime, long serverTime)
        {
            if (!_waitingPong)
                return;

            // 验证是否是当前等待的PING响应
            if (clientTime != _pingTimestamp)
                return;

            _waitingPong = false;
            _lastReceiveTime = Time.realtimeSinceStartup;

            // 收到PONG，重置重连计数
            CurrentRetryCount = 0;

            long delay = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _pingTimestamp;
            Debug.Log($"[HeartbeatManager] Pong received, delay: {delay}ms");

            OnPongReceived?.Invoke(delay);
        }
    }
}