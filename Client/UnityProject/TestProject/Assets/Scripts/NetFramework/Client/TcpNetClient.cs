using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;

namespace NetFramework
{
    public class TcpNetClient : BaseClient
    {
        private Socket _socket;
        private byte[] _lengthBuffer = new byte[4];
        private byte[] _msgIdBuffer = new byte[4];
        private const int ReceiveBufferSize = 8192;

        public override async Task ConnectAsync(string host, int port)
        {
            try
            {
                State = NetworkState.Connecting;
                _cts = new System.Threading.CancellationTokenSource();

                // 创建 Socket
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true; // 禁用 Nagle 算法，降低延迟
                _socket.SendBufferSize = ReceiveBufferSize;
                _socket.ReceiveBufferSize = ReceiveBufferSize;

                // 异步连接
                await _socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));

                State = NetworkState.Connected;
                RaiseConnected();
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                State = NetworkState.Disconnected;
                RaiseError(ex);
            }
        }

        public override void Disconnect()
        {
            _cts?.Cancel();
            if (_socket != null)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                _socket.Close();
                _socket = null;
            }
            State = NetworkState.Disconnected;
            RaiseDisconnected();
        }

        public override async Task SendAsync(ushort msgId, IMessage msg)
        {
            if (State != NetworkState.Connected || _socket == null || !_socket.Connected)
                return;

            byte[] data = _messageHandler.Pack(msgId, msg);
            await SendInternalAsync(data);
        }

        private async Task SendInternalAsync(byte[] data)
        {
            int sent = 0;
            while (sent < data.Length)
            {
                int n = await _socket.SendAsync(new ArraySegment<byte>(data, sent, data.Length - sent), SocketFlags.None);
                if (n == 0) throw new IOException("Connection closed");
                sent += n;
            }
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested && _socket != null && _socket.Connected)
                {
                    // 读取 4-byte 长度
                    await ReadExactAsync(_lengthBuffer, 4);
                    int length = BitConverter.ToInt32(_lengthBuffer, 0);

                    if (length < 8 || length > 1024 * 1024) // 最大 1MB
                    {
                        throw new IOException($"Invalid message length: {length}");
                    }

                    // 读取 4-byte msgId
                    await ReadExactAsync(_msgIdBuffer, 4);
                    ushort msgId = (ushort)BitConverter.ToUInt32(_msgIdBuffer, 0);

                    // 读取 payload
                    int payloadLen = length - 8;
                    byte[] payload = new byte[payloadLen];
                    await ReadExactAsync(payload, payloadLen);

                    var message = _messageHandler.Unpack(msgId, payload);
                    if (message != null)
                        RaiseMessage(msgId, message);
                }
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                Disconnect();
            }
        }

        private async Task ReadExactAsync(byte[] buffer, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, read, count - read), SocketFlags.None);
                if (n == 0) throw new IOException("Connection closed");
                read += n;
            }
        }
    }
}
