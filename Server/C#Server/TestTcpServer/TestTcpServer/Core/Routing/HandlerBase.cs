using GameServer.Core.Net;
using GameServer.Core.Protocol;

namespace GameServer.Core.Routing;

/// <summary>
/// 所有业务 Handler 的基类。
/// 子类通过 DI 构造函数注入所需服务（Repository、Logger 等）。
/// </summary>
public abstract class HandlerBase
{
    /// <summary>处理一条消息。子类实现具体逻辑。</summary>
    public abstract ValueTask HandleAsync(
        GameSession session,
        Packet packet,
        CancellationToken ct);
}
