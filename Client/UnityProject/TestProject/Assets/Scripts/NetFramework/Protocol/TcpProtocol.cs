using System;
using System.Net;
using System.Net.Sockets;
using Fuel.NetFramework.Codec;
using Fuel.NetFramework.Core;
using UnityEngine;

namespace Fuel.NetFramework.Protocol
{
    /// <summary>
    /// TCP 协议实现
    /// 基于 System.Net.Sockets.Socket，异步收发，自动处理粘包/拆包
    /// </summary>
    public class TcpProtocol : IProtocol
    {
        private const int ReceiveBufferSize = 64 * 1024; // 64KB 接收缓冲区

        private Socket _socket;
        private readonly byte[] _receiveBuffer = new byte[ReceiveBufferSize];
        private int _bufferOffset; // 缓冲区中已有的数据长度

        private readonly object _sendLock = new object();

        public ProtocolType Type => ProtocolType.TCP;
        public string Host { get; private set; }
        public int Port { get; private set; }

        public bool IsConnected => _socket != null && _socket.Connected;

        public event Action OnConnected;
        public event Action<bool> OnDisconnected;
        public event Action<uint, ArraySegment<byte>> OnDataReceived;
        public event Action<string> OnError;

        public void Connect(string host, int port)
        {
            if (IsConnected)
            {
                Debug.LogWarning("[TcpProtocol] Already connected, close first.");
                return;
            }

            try
            {
                Host = host;
                Port = port;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, (System.Net.Sockets.ProtocolType)ProtocolType.TCP);
                _socket.NoDelay = true; // 禁用 Nagle 算法，减少延迟
                _bufferOffset = 0;

                var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
                _socket.BeginConnect(endpoint, ConnectCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TcpProtocol] Connect failed: {e.Message}");
                OnError?.Invoke($"Connect failed: {e.Message}");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                Debug.Log("[TcpProtocol] Connected.");
                OnConnected?.Invoke();

                // 开始接收数据
                BeginReceive();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TcpProtocol] Connect callback error: {e.Message}");
                OnError?.Invoke($"Connect error: {e.Message}");
                OnDisconnected?.Invoke(true);
            }
        }

        private void BeginReceive()
        {
            if (!IsConnected) return;

            try
            {
                _socket.BeginReceive(
                    _receiveBuffer, _bufferOffset,
                    ReceiveBufferSize - _bufferOffset,
                    SocketFlags.None,
                    ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TcpProtocol] BeginReceive error: {e.Message}");
                OnError?.Invoke($"Receive error: {e.Message}");
                HandleDisconnect(true);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!IsConnected) return;

            int bytesRead;
            try
            {
                bytesRead = _socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TcpProtocol] EndReceive error: {e.Message}");
                HandleDisconnect(true);
                return;
            }

            if (bytesRead <= 0)
            {
                Debug.Log("[TcpProtocol] Server closed connection.");
                HandleDisconnect(false);
                return;
            }

            _bufferOffset += bytesRead;
            ProcessReceivedData();

            // 继续接收下一段数据
            BeginReceive();
        }

        /// <summary>
        /// 处理缓冲区中的数据，循环解码完整包
        /// </summary>
        private void ProcessReceivedData()
        {
            int offset = 0;

            while (offset < _bufferOffset)
            {
                int consumed = PacketCodec.Decode(
                    _receiveBuffer, offset,
                    _bufferOffset - offset,
                    out Packet packet);

                if (consumed == 0)
                    break; // 数据不足，等待更多数据

                offset += consumed;

                try
                {
                    OnDataReceived?.Invoke(packet.CmdId, packet.Body);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TcpProtocol] OnDataReceived handler error: {e.Message}");
                }
            }

            // 将未处理的数据移到缓冲区头部
            if (offset > 0 && offset < _bufferOffset)
            {
                int remaining = _bufferOffset - offset;
                Buffer.BlockCopy(_receiveBuffer, offset, _receiveBuffer, 0, remaining);
                _bufferOffset = remaining;
            }
            else if (offset >= _bufferOffset)
            {
                _bufferOffset = 0;
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[TcpProtocol] Cannot send, not connected.");
                return;
            }

            lock (_sendLock)
            {
                try
                {
                    _socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TcpProtocol] Send error: {e.Message}");
                    OnError?.Invoke($"Send error: {e.Message}");
                    HandleDisconnect(true);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TcpProtocol] Send callback error: {e.Message}");
                HandleDisconnect(true);
            }
        }

        public void Close()
        {
            HandleDisconnect(false);
        }

        private void HandleDisconnect(bool isAbnormal)
        {
            if (_socket == null) return;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { /* ignore shutdown errors */ }

            try
            {
                _socket.Close();
            }
            catch { /* ignore close errors */ }

            _socket = null;
            _bufferOffset = 0;

            Debug.Log($"[TcpProtocol] Disconnected. Abnormal: {isAbnormal}");
            OnDisconnected?.Invoke(isAbnormal);
        }
    }
}
