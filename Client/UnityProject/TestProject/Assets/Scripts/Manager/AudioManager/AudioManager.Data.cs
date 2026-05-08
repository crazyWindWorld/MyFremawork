using System.Collections.Generic;
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

            private static readonly Stack<SoundParams> s_pool = new Stack<SoundParams>();

            public static SoundParams Get()
            {
                return s_pool.Count > 0 ? s_pool.Pop() : new SoundParams();
            }

            public void Reset()
            {
                Type = AudioType.SE;
                IsLoop = false;
                SpatialBlend = 0f;
                MinDistance = 5f;
                MaxDistance = 30f;
            }

            public void Release()
            {
                Reset();
                s_pool.Push(this);
            }
        }

        public sealed class AudioClipData
        {
            /// <summary>
            /// 路径
            /// </summary>
            public string Path { get; set; }

            public SoundParams PlayParams { get; set; }

            private static readonly Stack<AudioClipData> s_pool = new Stack<AudioClipData>();

            public static AudioClipData Get()
            {
                var data = s_pool.Count > 0 ? s_pool.Pop() : new AudioClipData();
                data.PlayParams = SoundParams.Get();
                return data;
            }

            public void Release()
            {
                Path = null;
                PlayParams?.Release();
                PlayParams = null;
                s_pool.Push(this);
            }
        }
    }
}