using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;

namespace NetFramework
{
    public class UdpNetClient : BaseClient
    {
        private Socket _socket;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private const int ReceiveBufferSize = 8192;

        public override async Task ConnectAsync(string host, int port)
        {
            try
            {
                State = NetworkState.Connecting;
                _cts = new System.Threading.CancellationTokenSource();

                // 创建 UDP Socket
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.SendBufferSize = ReceiveBufferSize;
                _socket.ReceiveBufferSize = ReceiveBufferSize;

                // 绑定本地端口（让系统自动分配）
                _localEndPoint = new IPEndPoint(IPAddress.Any, 0);
                _socket.Bind(_localEndPoint);

                // 设置远程端点
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

                // UDP 是无连接的，直接标记为已连接
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
                    _socket.Close();
                }
                catch { }
                _socket = null;
            }
            State = NetworkState.Disconnected;
            RaiseDisconnected();
        }

        public override async Task SendAsync(ushort msgId, IMessage msg)
        {
            if (State != NetworkState.Connected || _socket == null)
                return;

            byte[] data = _messageHandler.Pack(msgId, msg);
            await _socket.SendToAsync(new ArraySegment<byte>(data), SocketFlags.None, _remoteEndPoint);
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                byte[] buffer = new byte[ReceiveBufferSize];
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                while (!_cts.Token.IsCancellationRequested && _socket != null)
                {
                    // 接收数据
                    var result = await _socket.ReceiveFromAsync(
                        new ArraySegment<byte>(buffer), 
                        SocketFlags.None, 
                        remoteEP);

                    int received = result.ReceivedBytes;
                    if (received < 8) continue;

                    // 解析消息头
                    int length = BitConverter.ToInt32(buffer, 0);
                    ushort msgId = (ushort)BitConverter.ToUInt32(buffer, 4);
                    int payloadLen = length - 8;

                    if (payloadLen < 0 || payloadLen > received - 8)
                        continue;

                    // 提取 payload
                    byte[] payload = new byte[payloadLen];
                    Buffer.BlockCopy(buffer, 8, payload, 0, payloadLen);

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
    }
}
