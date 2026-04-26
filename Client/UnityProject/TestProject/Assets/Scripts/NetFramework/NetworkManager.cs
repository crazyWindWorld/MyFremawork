using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Singleton;
using UnityEngine;

namespace NetFramework
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        private NetworkConfig _config;
        public BaseClient Client { get; private set; }
        public HeartbeatManager Heartbeat { get; private set; }
        public MessageCacheManager MessageCache { get; private set; }
        public MessageDispatcher Dispatcher { get; private set; }

        public bool IsConnected => Client?.State == BaseClient.NetworkState.Connected;

        // 重连相关
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 5;
        private float _reconnectDelay = 2f;
        private bool _isReconnecting = false;
        private NetworkConfig.ProtocolType _currentProtocol;
        private string _currentHost;
        private int _currentPort;

        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnReconnecting;
        public event Action<int> OnReconnectFailed;
        protected override void Init()
        {
            base.Init();
           /*  // 初始化心跳管理器
            Heartbeat = new HeartbeatManager();
            Heartbeat.Interval = _config.HeartbeatInterval;
            Heartbeat.Initialize(() => IsConnected, SendAsync);
            Heartbeat.OnHeartbeatTimeout += HandleHeartbeatTimeout; */

            // 初始化消息缓存管理器
            MessageCache = new MessageCacheManager();

            // 初始化消息分发器
            Dispatcher = new MessageDispatcher();
        }


        private void Update()
        {
            Heartbeat?.Update();
            MessageCache?.CleanupTimeout();
        }

        public async Task Connect(NetworkConfig.ProtocolType protocol, string host, int port)
        {
            _config = new NetworkConfig();
            _config.AutoReconnect = true;
            _config.HeartbeatInterval = 1000;
            _config.Host = host;
            _config.Port = port;
            _config.Protocol = protocol;
            // 保存连接信息用于重连
            _currentProtocol = protocol;
            _currentHost = host;
            _currentPort = port;
            _reconnectAttempts = 0;
            _isReconnecting = false;

            await DoConnectAsync(protocol, host, port);
        }

        private async Task DoConnectAsync(NetworkConfig.ProtocolType protocol, string host, int port)
        {
            Client?.Disconnect();
            Client = protocol switch
            {
                NetworkConfig.ProtocolType.Tcp => new TcpNetClient(),
                NetworkConfig.ProtocolType.Udp => new UdpNetClient(),
                NetworkConfig.ProtocolType.WebSocket => new WsNetClient(),
                _ => throw new ArgumentException("Unsupported protocol")
            };

            Client.OnConnected += HandleConnected;
            Client.OnDisconnected += HandleDisconnected;
            Client.OnError += HandleError;
            Client.OnMessage += HandleMessage;

            await Client.ConnectAsync(host, port);
        }

        private void HandleConnected()
        {
            Debug.Log($"[NetworkManager] [{_currentProtocol}] Connected");
            _reconnectAttempts = 0;
            _isReconnecting = false;
            Heartbeat?.Start();
            OnConnected?.Invoke();
        }

        private void HandleDisconnected()
        {
            Debug.Log($"[NetworkManager] [{_currentProtocol}] Disconnected");
            Heartbeat?.Stop();
            OnDisconnected?.Invoke();

            if (_config.AutoReconnect && !_isReconnecting && _reconnectAttempts < MaxReconnectAttempts)
            {
                _ = TryReconnectAsync();
            }
        }

        private void HandleError(Exception ex)
        {
            Debug.LogError($"[NetworkManager] [{_currentProtocol}] Error: {ex.Message}");

            if (_config.AutoReconnect && !_isReconnecting && _reconnectAttempts < MaxReconnectAttempts)
            {
                _ = TryReconnectAsync();
            }
        }

        private void HandleMessage(ushort msgId, IMessage msg)
        {
            // 处理心跳响应
            if (msgId == Heartbeat.HeartbeatRspMsgId)
            {
                Heartbeat?.OnHeartbeatResponseReceived();
                return; // 心跳消息不分发给外部
            }

            // 尝试匹配缓存的请求
            IMessage request = MessageCache?.TryGetRequest(msgId);

            // 通过分发器分发消息（支持两种事件）
            Dispatcher?.Dispatch(msgId, msg, request);
        }

        private void HandleHeartbeatTimeout()
        {
            Debug.LogWarning("[NetworkManager] Heartbeat timeout, disconnecting...");
            Disconnect();
            if (_config.AutoReconnect && !_isReconnecting)
            {
                _ = TryReconnectAsync();
            }
        }

        private async Task TryReconnectAsync()
        {
            if (_isReconnecting) return;
            _isReconnecting = true;

            while (_reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectAttempts++;
                Debug.Log($"[NetworkManager] Reconnecting... Attempt {_reconnectAttempts}/{MaxReconnectAttempts}");
                OnReconnecting?.Invoke();

                await Task.Delay(TimeSpan.FromSeconds(_reconnectDelay * _reconnectAttempts));

                try
                {
                    await DoConnectAsync(_currentProtocol, _currentHost, _currentPort);
                    if (IsConnected)
                    {
                        Debug.Log("[NetworkManager] Reconnect successful!");
                        _isReconnecting = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkManager] Reconnect attempt {_reconnectAttempts} failed: {ex.Message}");
                }
            }

            Debug.LogError("[NetworkManager] Max reconnect attempts reached, giving up.");
            OnReconnectFailed?.Invoke(_reconnectAttempts);
            _isReconnecting = false;
        }

        public void Disconnect()
        {
            _isReconnecting = false;
            Heartbeat?.Stop();
            MessageCache?.Clear();
            Dispatcher?.Clear();
            Client?.Disconnect();
        }

        public async Task Send<T>(T msg, bool cacheRequest = false) where T : IMessage<T>
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Cannot send message, not connected");
                return;
            }

            ushort msgId = MessageHandler.GetMsgId(msg);
            if (msgId == 0)
            {
                Debug.LogError($"[NetworkManager] Message type {typeof(T).Name} not registered");
                return;
            }

            // 缓存请求（如果需要）
            if (cacheRequest)
            {
                MessageCache?.CacheRequest(msgId, msg);
            }

            await Client.SendAsync(msgId, msg);
        }

        public async Task SendAsync(ushort msgId, IMessage msg)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Cannot send message, not connected");
                return;
            }

            await Client.SendAsync(msgId, msg);
        }

        private void OnDestroy()
        {
            Heartbeat?.Stop();
            MessageCache?.Clear();
            Dispatcher?.Clear();
            Client?.Dispose();
        }
    }

    [Serializable]
    public class NetworkConfig
    {
        public enum ProtocolType { Tcp, Udp, WebSocket }
        public ProtocolType Protocol;
        public string Host = "127.0.0.1";
        public int Port = 8080;
        public bool AutoReconnect = true;
        public float HeartbeatInterval = 5f;
    }
}
