namespace NetFramework.Protocol
{
    /// <summary>
    /// 协议工厂，根据协议类型创建对应的 IProtocol 实例
    /// 扩展新协议时只需在 switch 中添加对应分支
    /// </summary>
    public static class ProtocolFactory
    {
        /// <summary>
        /// 创建指定类型的协议实例
        /// </summary>
        /// <param name="protocolType">协议类型</param>
        /// <returns>IProtocol 实例</returns>
        public static IProtocol Create(ProtocolType protocolType)
        {
            switch (protocolType)
            {
                case ProtocolType.TCP:
                    return new TcpProtocol();
                // case ProtocolType.WebSocket:
                //     return new WebSocketProtocol();
                // case ProtocolType.KCP:
                //     return new KcpProtocol();
                default:
                    throw new System.ArgumentException($"Unsupported protocol type: {protocolType}");
            }
        }
    }
}
