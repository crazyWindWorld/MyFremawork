using Fuel.Singleton;

namespace Fuel.Manager.AudioManager
{
    public sealed partial class AudioManager: Singleton<AudioManager>
    {
        /// <summary>
        /// 声音大类枚举
        /// </summary>
        public enum AudioType
        {
            /// <summary>
            /// 背景音乐
            /// </summary>
            BGM = 1,

            /// <summary>
            /// 环境音乐 例：鸟叫、雷声
            /// </summary>
            BGS = 2,

            /// <summary>
            /// 气氛音效 例：战斗胜利、转场
            /// </summary>
            ME = 3,

            /// <summary>
            /// 普通音效 例：点击、打击
            /// </summary>
            SE = 4,
        }

        public sealed class SoundParams
        {
            /// <summary>
            /// 声音大类
            /// </summary>
            public AudioType Type { get; set; }

            /// <summary>
            /// 是否循环
            /// </summary>
            public bool IsLoop = false;

            /// <summary>
            /// 声音混合量
            /// 0-> 2D
            /// 1-> 3D
            /// </summary>
            public float SpatialBlend = 0f;

            /// <summary>
            /// 声音最小距离
            /// </summary>
            public float MinDistance = 5f;

            /// <summary>
            /// 声音最大距离
            /// </summary>
            public float MaxDistance = 30f;
        }

        public sealed class AudioClipData
        {
            /// <summary>
            /// 路径
            /// </summary>
            public string Path { get; set; }

            public SoundParams PlayParams { get; set; } = new();
        }
    }
}