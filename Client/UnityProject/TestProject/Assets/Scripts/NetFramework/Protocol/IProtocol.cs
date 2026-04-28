using System;

namespace NetFramework.Protocol
{
    /// <summary>
    /// 协议类型枚举，用于 ProtocolFactory 创建对应协议实例
    /// </summary>
    public enum ProtocolType
    {
        TCP,
        WebSocket,
        KCP
    }

    /// <summary>
    /// 网络协议抽象接口
    /// 定义统一的连接、收发、关闭接口，支持 TCP / WebSocket / KCP 等扩展
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// 当前协议类型
        /// </summary>
        ProtocolType Type { get; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        string Host { get; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// 连接断开回调 (参数: 是否异常断开)
        /// </summary>
        event Action<bool> OnDisconnected;

        /// <summary>
        /// 收到完整数据包回调 (参数: cmdId, bodyBytes)
        /// </summary>
        event Action<uint, ArraySegment<byte>> OnDataReceived;

        /// <summary>
        /// 网络错误回调 (参数: errorMessage)
        /// </summary>
        event Action<string> OnError;

        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        void Connect(string host, int port);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">编码后的完整数据包字节</param>
        void Send(byte[] data);

        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();
    }
}
