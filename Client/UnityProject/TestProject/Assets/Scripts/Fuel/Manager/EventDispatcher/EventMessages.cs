namespace Fuel.GameEvent
{
    /// <summary>
    /// 窗口显示事件
    /// </summary>
    public struct UI_WindowShowEvent : IEventMessage
    {
        public string WindowId;
    }

    /// <summary>
    /// 窗口隐藏事件
    /// </summary>
    public struct UI_WindowHideEvent : IEventMessage
    {
        public string WindowId;
    }

    /// <summary>
    /// 玩家登录事件
    /// </summary>
    public struct Game_PlayerLoginEvent : IEventMessage
    {
        public int UserId;
        public string UserName;
    }

    /// <summary>
    /// 关卡切换事件
    /// </summary>
    public struct Game_LevelChangeEvent : IEventMessage
    {
        public int OldLevel;
        public int NewLevel;
    }

    /// <summary>
    /// 网络连接事件
    /// </summary>
    public struct Net_ConnectedEvent : IEventMessage
    {
    }

    /// <summary>
    /// 网络断开事件
    /// </summary>
    public struct Net_DisconnectedEvent : IEventMessage
    {
        public string Reason;
    }
}
