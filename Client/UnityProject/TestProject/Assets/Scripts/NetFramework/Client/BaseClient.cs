using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace NetFramework
{
    public abstract class BaseClient : IDisposable
    {
        public enum NetworkState { Disconnected, Connecting, Connected }
        public NetworkState State { get; protected set; } = NetworkState.Disconnected;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnError;
        public event Action<ushort, IMessage> OnMessage;

        protected MessageHandler _messageHandler = new MessageHandler();
        protected CancellationTokenSource _cts;

        public abstract Task ConnectAsync(string host, int port);
        public abstract void Disconnect();
        public abstract Task SendAsync(ushort msgId, IMessage msg);

        protected void RaiseConnected() => MainThreadDispatcher.Instance.Enqueue(() => OnConnected?.Invoke());
        protected void RaiseDisconnected() => MainThreadDispatcher.Instance.Enqueue(() => OnDisconnected?.Invoke());
        protected void RaiseError(Exception ex) => MainThreadDispatcher.Instance.Enqueue(() => OnError?.Invoke(ex));
        protected void RaiseMessage(ushort msgId, IMessage msg) => MainThreadDispatcher.Instance.Enqueue(() => OnMessage?.Invoke(msgId, msg));

        public virtual void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
