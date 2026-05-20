namespace Fuel.Manager.AudioManager
{
    /// <summary>
    /// 音频播放状态
    /// </summary>
    public enum AudioSourceState : byte
    {
        None,
        Playing, // 播放中
        Paused, // 暂停
        Stopped, // 停止
    }
}