namespace GameServer.Core.Routing;

/// <summary>
/// 标记一个 Handler 类为某个 msgId 的处理器。
/// 用法：
///   [MsgHandler(ProtoCmds.LoginReq, RequireLogin = false)]
///   public class LoginHandler : HandlerBase { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MsgHandlerAttribute : Attribute
{
    public int MsgId { get; }

    /// <summary>
    /// 是否要求 Session 已登录才能处理。
    /// 默认 true；LoginReq、PingReq 等无需登录的消息设为 false。
    /// </summary>
    public bool RequireLogin { get; set; } = true;

    public MsgHandlerAttribute(int msgId) => MsgId = msgId;
}
