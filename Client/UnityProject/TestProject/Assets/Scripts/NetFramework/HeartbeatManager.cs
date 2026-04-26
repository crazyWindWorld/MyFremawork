using System;
using System.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

namespace NetFramework
{
    public class HeartbeatManager
    {
        // 配置
        public float Interval { get; set; } = 5f;
        public float Timeout { get; set; } = 10f;
        public ushort HeartbeatReqMsgId { get; set; } = 9998;
        public ushort HeartbeatRspMsgId { get; set; } = 9999;

        // 状态
        private float _lastHeartbeatTime = 0f;
        private float _lastPongTime = 0f;
        private bool _isRunning = false;

        // 依赖
        private Func<bool> _isConnectedFunc;
        private Func<ushort, IMessage, Task> _sendFunc;

        // 事件
        public event Action OnHeartbeatSent;
        public event Action OnHeartbeatReceived;
        public event Action OnHeartbeatTimeout;

        public bool IsTimeout => _isRunning && Time.time - _lastPongTime > Timeout;

        public void Initialize(Func<bool> isConnectedFunc, Func<ushort, IMessage, Task> sendFunc)
        {
            _isConnectedFunc = isConnectedFunc;
            _sendFunc = sendFunc;
        }

        public void Start()
        {
            _isRunning = true;
            _lastHeartbeatTime = Time.time;
            _lastPongTime = Time.time;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Update()
        {
            if (!_isRunning) return;
            if (_isConnectedFunc == null || !_isConnectedFunc()) return;

            // 发送心跳
            if (Time.time - _lastHeartbeatTime >= Interval)
            {
                _ = SendHeartbeatAsync();
                _lastHeartbeatTime = Time.time;
            }

            // 检测超时
            if (IsTimeout)
            {
                OnHeartbeatTimeout?.Invoke();
            }
        }

        public void OnHeartbeatResponseReceived()
        {
            _lastPongTime = Time.time;
            OnHeartbeatReceived?.Invoke();
        }

        private async Task SendHeartbeatAsync()
        {
            if (_sendFunc == null) return;
            if (!_isConnectedFunc()) return;

            try
            {
                // 发送心跳请求（空消息或自定义心跳消息）
                await _sendFunc(HeartbeatReqMsgId, null);
                OnHeartbeatSent?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HeartbeatManager] Failed to send heartbeat: {ex.Message}");
            }
        }

        public void Reset()
        {
            _lastHeartbeatTime = Time.time;
            _lastPongTime = Time.time;
        }
    }
}
