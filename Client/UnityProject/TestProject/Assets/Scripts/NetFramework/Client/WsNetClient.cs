using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace NetFramework
{
    public class WsNetClient : BaseClient
    {
        private ClientWebSocket _ws;
        private const int ReceiveBufferSize = 8192;

        public override async Task ConnectAsync(string host, int port)
        {
            try
            {
                State = NetworkState.Connecting;
                _cts = new System.Threading.CancellationTokenSource();
                _ws = new ClientWebSocket();
                var uri = new Uri($"ws://{host}:{port}");
                await _ws.ConnectAsync(uri, _cts.Token);
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
            _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
            _ws?.Dispose();
            State = NetworkState.Disconnected;
            RaiseDisconnected();
        }

        public override async Task SendAsync(ushort msgId, IMessage msg)
        {
            if (State != NetworkState.Connected || _ws.State != WebSocketState.Open) return;
            byte[] data = _messageHandler.Pack(msgId, msg);
            await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, _cts.Token);
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                byte[] buffer = new byte[ReceiveBufferSize];
                while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await _ws.ReceiveAsync(segment, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Disconnect();
                        return;
                    }

                    if (result.Count >= 8)
                    {
                        int length = BitConverter.ToInt32(buffer, 0);
                        ushort msgId = (ushort)BitConverter.ToUInt32(buffer, 4);
                        int payloadLen = length - 8;

                        byte[] payload = new byte[payloadLen];
                        Buffer.BlockCopy(buffer, 8, payload, 0, payloadLen);

                        var message = _messageHandler.Unpack(msgId, payload);
                        if (message != null)
                            RaiseMessage(msgId, message);
                    }
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
