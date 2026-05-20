using Fuel.GameEvent;

namespace Fuel.Scene
{
    /// <summary>
    /// 场景开始加载事件
    /// </summary>
    public struct Scene_LoadStartEvent : IEventMessage
    {
        public string SceneId;
        public bool IsMainScene;
    }

    /// <summary>
    /// 场景加载进度事件
    /// </summary>
    public struct Scene_LoadProgressEvent : IEventMessage
    {
        public string SceneId;
        public float Progress;
    }

    /// <summary>
    /// 场景加载完成事件
    /// </summary>
    public struct Scene_LoadCompleteEvent : IEventMessage
    {
        public string SceneId;
        public bool IsMainScene;
    }

    /// <summary>
    /// 场景开始卸载事件
    /// </summary>
    public struct Scene_UnloadStartEvent : IEventMessage
    {
        public string SceneId;
    }

    /// <summary>
    /// 场景卸载完成事件
    /// </summary>
    public struct Scene_UnloadCompleteEvent : IEventMessage
    {
        public string SceneId;
    }

    /// <summary>
    /// 主场景切换事件
    /// </summary>
    public struct Scene_MainSceneChangedEvent : IEventMessage
    {
        public string OldSceneId;
        public string NewSceneId;
    }
}
